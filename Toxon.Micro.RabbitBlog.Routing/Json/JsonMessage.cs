using System.Text;
using Newtonsoft.Json;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.Routing.Patterns;

namespace Toxon.Micro.RabbitBlog.Routing.Json
{
    public class JsonMessage
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Converters =
            {
                new PatternJsonConverter(),
                new ValuePatternJsonConverter()
            }
        };

        public static T Read<T>(Message message)
        {
            var messageString = Encoding.UTF8.GetString(message.Body);
            return JsonConvert.DeserializeObject<T>(messageString, Settings);
        }

        public static Message Write<T>(T message)
        {
            var content = JsonConvert.SerializeObject(message, Settings);
            var body = Encoding.UTF8.GetBytes(content);

            return new Message(body);
        }
    }
}
