using System.Collections.Generic;
using System.Linq;
using Toxon.Micro.RabbitBlog.Core;

namespace Toxon.Micro.RabbitBlog.Serverless.Host
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
        
        internal Message ToMessage()
        {
            return new Message(Body, Headers);
        }
    }
}