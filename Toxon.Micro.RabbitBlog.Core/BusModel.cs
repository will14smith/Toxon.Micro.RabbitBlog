using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Toxon.Micro.RabbitBlog.Core
{
    public class BusModel
    {
        private readonly IModel _model;

        public BusModel(IModel model)
        {
            _model = model;
        }

        public async Task SendAsync(string route, Message message, CancellationToken cancellationToken = default)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var exchange = await DeclareBusExchangeAsync(cts.Token);

            var properties = _model.CreateBasicProperties();
            properties.Headers = message.Headers.ToDictionary(x => x.Key, x => x.Value);

            _model.BasicPublish(exchange, route, properties, message.Body);
        }

        private async Task<string> DeclareBusExchangeAsync(CancellationToken cancellationToken)
        {
            var exchangeName = "toxon.micro.bus";
            _model.ExchangeDeclare(exchangeName, ExchangeType.Direct);
            return exchangeName;
        }

        public async Task RegisterHandlerAsync(string queueName, string route, Action<Message> handler, CancellationToken cancellationToken = default)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var exchange = await DeclareBusExchangeAsync(cts.Token);
            var queue = _model.QueueDeclare(queueName, durable: true, exclusive: false);
            _model.QueueBind(queue.QueueName, exchange, route);

            var consumer = new AsyncEventingBasicConsumer(_model);
            consumer.Received += async (sender, ea) =>
            {
                handler(Message.FromArgs(ea));

                _model.BasicAck(ea.DeliveryTag, false);
            };

            _model.BasicConsume(queue.QueueName, false, consumer);
        }
    }
}
