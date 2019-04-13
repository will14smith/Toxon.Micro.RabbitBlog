using System.Linq;
using EasyNetQ;
using Toxon.Micro.RabbitBlog.Core;

namespace Toxon.Micro.RabbitBlog.Rabbit
{
    internal static class MessageHelpers
    {
        internal static Message FromArgs(byte[] body, MessageProperties properties)
        {
            var headers = properties.Headers.ToDictionary(x => x.Key, x => (byte[])x.Value);

            return new Message(body, headers);
        }
    }
}
