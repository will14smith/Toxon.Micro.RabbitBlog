using System.Collections.Generic;

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
    }
}