using System.Collections.Generic;

namespace Toxon.Micro.RabbitBlog.Core
{
    public class Message
    {
        public Message(byte[] body) : this(body, new Dictionary<string, byte[]>()) { }
        public Message(byte[] body, IReadOnlyDictionary<string, byte[]> headers)
        {
            Body = body;
            Headers = headers;
        }

        public IReadOnlyDictionary<string, byte[]> Headers { get; }
        public byte[] Body { get; }
    }
}