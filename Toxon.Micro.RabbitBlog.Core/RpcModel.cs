using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ;
using EasyNetQ.Topology;

namespace Toxon.Micro.RabbitBlog.Core
{
    public class RpcModel 
    {
        private const string ReplyExchangeName = "toxon.micro.rpc.reply";
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(1000);
        
        private readonly IAdvancedBus _bus;

        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<Message>> _responseHandlers
            = new ConcurrentDictionary<Guid, TaskCompletionSource<Message>>();

        private readonly AsyncLock _replyQueueLock = new AsyncLock();
        private string _replyQueue;

        public RpcModel(IAdvancedBus bus)
        {
            _bus = bus;
        }

        public async Task<Message> SendAsync(string route, Message message, CancellationToken cancellationToken = default)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(DefaultTimeout);

            var correlationId = Guid.NewGuid();
            var tcs = new TaskCompletionSource<Message>(TaskCreationOptions.RunContinuationsAsynchronously);
            RegisterReplyHandler(correlationId, tcs);

            using (cts.Token.Register(() => RemoveReplyHandler(correlationId)))
            using (cts.Token.Register(() => tcs.SetCanceled()))
            {
                var exchange = await DeclareRpcExchangeAsync(cts.Token);
                var replyAddress = await SubscribeToReplyAsync(cts.Token);

                var properties = new MessageProperties
                {
                    CorrelationId = correlationId.ToString(),
                    ReplyTo = replyAddress,

                    Headers = message.Headers.ToDictionary(x => x.Key, x => x.Value)
                };

                await _bus.PublishAsync(exchange, route, true, properties, message.Body);

                return await tcs.Task;
            }
        }

        private void RegisterReplyHandler(Guid correlationId, TaskCompletionSource<Message> tcs)
        {
            _responseHandlers.TryAdd(correlationId, tcs);
        }

        private void RemoveReplyHandler(Guid correlationId)
        {
            _responseHandlers.TryRemove(correlationId, out _);
        }

        private async Task<IExchange> DeclareRpcExchangeAsync(CancellationToken cancellationToken)
        {
            var exchangeName = "toxon.micro.rpc";

            return await _bus.ExchangeDeclareAsync(exchangeName, ExchangeType.Direct);
        }

        private async Task<string> SubscribeToReplyAsync(CancellationToken cancellationToken)
        {
            if (_replyQueue != null)
                return _replyQueue;

            using (await _replyQueueLock.AcquireAsync(cancellationToken))
            {
                if (_replyQueue != null)
                    return _replyQueue;

                var replyQueue = $"toxon.micro.rpc.reply.{Guid.NewGuid()}";

                var exchange = await _bus.ExchangeDeclareAsync(ReplyExchangeName, ExchangeType.Direct);
                var queue = await _bus.QueueDeclareAsync(replyQueue);
                await _bus.BindAsync(exchange, queue, replyQueue);

                _bus.Consume(queue, (body, props, info) =>
                {
                    var correlationIdString = props.CorrelationId;

                    if (Guid.TryParse(correlationIdString, out var correlationId) && _responseHandlers.TryRemove(correlationId, out var tcs))
                    {
                        tcs.SetResult(Message.FromArgs(body, props));
                    }
                });

                _replyQueue = replyQueue;
                return _replyQueue;
            }
        }

        public async Task RegisterHandlerAsync(string route, Func<Message, CancellationToken, Task<Message>> handler, CancellationToken cancellationToken = default)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var exchange = await DeclareRpcExchangeAsync(cts.Token);
            var queue = await _bus.QueueDeclareAsync();
            await _bus.BindAsync(exchange, queue, route);

            _bus.Consume(queue, (body, props, info) => HandleRequestAsync(body, props, handler));
        }

        private async Task HandleRequestAsync(byte[] body, MessageProperties props, Func<Message, CancellationToken, Task<Message>> handler)
        {
            var request = Message.FromArgs(body, props);
            // TODO cancellation token
            var response = await handler(request, CancellationToken.None);

            var exchange = await _bus.ExchangeDeclareAsync(ReplyExchangeName, ExchangeType.Direct, passive: true);

            var properties = new MessageProperties
            {
                CorrelationId = props.CorrelationId,
                Headers = response.Headers.ToDictionary(x => x.Key, x => x.Value)
            };

            await _bus.PublishAsync(exchange,
                props.ReplyTo,
                true,
                properties,
                response.Body);
        }
    }
}
