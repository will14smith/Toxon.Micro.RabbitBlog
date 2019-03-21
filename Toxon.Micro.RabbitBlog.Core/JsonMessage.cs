using System.Text;
using Newtonsoft.Json;

namespace Toxon.Micro.RabbitBlog.Core
{
    public class JsonMessage
    {
        public static T Read<T>(Message message)
        {
            var messageString = Encoding.UTF8.GetString(message.Body);
            return JsonConvert.DeserializeObject<T>(messageString);
        }

        public static Message Write<T>(T message)
        {
            var content = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(content);

            return new Message(body);
        }
    }
}
