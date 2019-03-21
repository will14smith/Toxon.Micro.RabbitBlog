using System.Collections.Generic;
using System.Linq;
using RabbitMQ.Client.Events;

namespace Toxon.Micro.RabbitBlog.Core
{
    public class Message
    {
        public Message(byte[] body) : this(body, new Dictionary<string, object>()) { }
        public Message(byte[] body, IReadOnlyDictionary<string, object> headers)
        {
            Body = body;
            Headers = headers;
        }

        public IReadOnlyDictionary<string, object> Headers { get; }
        public byte[] Body { get; }

        internal static Message FromArgs(BasicDeliverEventArgs ea)
        {
            var body = ea.Body;
            var headers = ea.BasicProperties.Headers.ToDictionary(x => x.Key, x => x.Value);

            return new Message(body, headers);
        }
    }
}