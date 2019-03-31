using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ;
using EasyNetQ.Topology;

namespace Toxon.Micro.RabbitBlog.Core
{
    public class BusModel
    {
        private readonly IAdvancedBus _bus;


        public BusModel(IAdvancedBus bus)
        {
            _bus = bus;
        }

        public async Task SendAsync(string route, Message message, CancellationToken cancellationToken = default)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var exchange = await DeclareBusExchangeAsync(cts.Token);

            var properties = new MessageProperties
            {
                Headers = message.Headers.ToDictionary(x => x.Key, x => x.Value)
            };

            await _bus.PublishAsync(exchange, route, true, properties, message.Body);
        }

        private async Task<IExchange> DeclareBusExchangeAsync(CancellationToken cancellationToken)
        {
            var exchangeName = "toxon.micro.bus";

            return await _bus.ExchangeDeclareAsync(exchangeName, ExchangeType.Direct);
        }

        public async Task RegisterHandlerAsync(string route, Func<Message, CancellationToken, Task> handler, CancellationToken cancellationToken = default)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var exchange = await DeclareBusExchangeAsync(cts.Token);
            var queue = _bus.QueueDeclare(route);
            _bus.Bind(exchange, queue, route);

            // TODO token passed into handler?
            _bus.Consume(queue, (body, props, info) => handler(Message.FromArgs(body, props), CancellationToken.None));
        }
    }
}
