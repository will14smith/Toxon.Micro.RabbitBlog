using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Toxon.Micro.RabbitBlog.Core
{
    public class RpcModel
    {
        private readonly IModel _model;

        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<Message>> _responseHandlers
            = new ConcurrentDictionary<Guid, TaskCompletionSource<Message>>();

        private readonly AsyncLock _replyQueueLock = new AsyncLock();
        private PublicationAddress _replyQueue;

        public RpcModel(IModel model)
        {
            _model = model;
        }

        public async Task<Message> SendAsync(string route, Message message, CancellationToken cancellationToken = default)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            // TODO cts.CancelAfter(200);

            var correlationId = Guid.NewGuid();
            var tcs = new TaskCompletionSource<Message>();
            RegisterReplyHandler(correlationId, tcs);

            using (cts.Token.Register(() => RemoveReplyHandler(correlationId)))
            using (cts.Token.Register(() => tcs.SetCanceled()))
            {
                var exchange = await DeclareRpcExchangeAsync(cts.Token);
                var replyAddress = await SubscribeToReplyAsync(cts.Token);

                var properties = _model.CreateBasicProperties();
                properties.CorrelationId = correlationId.ToString();
                properties.ReplyToAddress = replyAddress;
                properties.Headers = message.Headers.ToDictionary(x => x.Key, x => x.Value);

                _model.BasicPublish(exchange, route, properties, message.Body);

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

        private async Task<string> DeclareRpcExchangeAsync(CancellationToken cancellationToken)
        {
            var exchangeName = "toxon.micro.rpc";
            _model.ExchangeDeclare(exchangeName, ExchangeType.Direct);
            return exchangeName;
        }

        private async Task<PublicationAddress> SubscribeToReplyAsync(CancellationToken cancellationToken)
        {
            if (_replyQueue != null)
                return _replyQueue;

            using (await _replyQueueLock.AcquireAsync(cancellationToken))
            {
                if (_replyQueue != null)
                    return _replyQueue;

                var exchange = "toxon.micro.rpc.reply";
                var replyQueue = $"toxon.micro.rpc.reply.{Guid.NewGuid()}";

                _model.ExchangeDeclare(exchange, ExchangeType.Direct);
                _model.QueueDeclare(replyQueue);
                _model.QueueBind(replyQueue, exchange, replyQueue);

                var consumer = new AsyncEventingBasicConsumer(_model);
                consumer.Received += async (sender, ea) =>
                {
                    var correlationIdString = ea.BasicProperties.CorrelationId;

                    if (Guid.TryParse(correlationIdString, out var correlationId) && _responseHandlers.TryRemove(correlationId, out var tcs))
                    {
                        tcs.SetResult(Message.FromArgs(ea));
                        _model.BasicAck(ea.DeliveryTag, false);
                    }
                    else
                    {
                        // TODO requeue?
                        _model.BasicNack(ea.DeliveryTag, false, false);
                    }

                };
                _model.BasicConsume(replyQueue, false, consumer);

                _replyQueue = new PublicationAddress(ExchangeType.Direct, exchange, replyQueue);
                return _replyQueue;
            }
        }

        public async Task RegisterHandlerAsync(string route, Func<Message, Task<Message>> handler, CancellationToken cancellationToken = default)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var exchange = await DeclareRpcExchangeAsync(cts.Token);
            var queue = _model.QueueDeclare();
            _model.QueueBind(queue.QueueName, exchange, route);

            var consumer = new AsyncEventingBasicConsumer(_model);
            consumer.Received += async (sender, ea) =>
            {
                await HandleRequestAsync(ea, handler);
                _model.BasicAck(ea.DeliveryTag, false);
            };

            _model.BasicConsume(queue.QueueName, false, consumer);
        }

        private async Task HandleRequestAsync(BasicDeliverEventArgs ea, Func<Message, Task<Message>> handler)
        {
            var response = await handler(Message.FromArgs(ea));

            var properties = _model.CreateBasicProperties();
            properties.CorrelationId = ea.BasicProperties.CorrelationId;
            properties.Headers = response.Headers.ToDictionary(x => x.Key, x => x.Value);

            _model.BasicPublish(
                ea.BasicProperties.ReplyToAddress.ExchangeName,
                ea.BasicProperties.ReplyToAddress.RoutingKey,
                properties,
                response.Body);
        }
    }
}
