using System.Collections.Generic;
using System.Linq;
using Toxon.Micro.RabbitBlog.Core;

namespace Toxon.Micro.RabbitBlog.Serverless.Core
{
    public class MessageModel
    {
        public MessageModel() { }
        public MessageModel(Message message)
        {
            Body = message.Body;
            Headers = message.Headers.ToDictionary(x => x.Key, x => x.Value);
        }

        public byte[] Body { get; set; }
        public Dictionary<string, byte[]> Headers { get; set; }

        public Message ToMessage()
        {
            return new Message(Body, Headers);
        }
    }
}