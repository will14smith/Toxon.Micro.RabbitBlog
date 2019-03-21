using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.Greetings.Messages;

namespace Toxon.Micro.RabbitBlog.Greetings
{
    class Program
    {
        private static readonly ConnectionFactory Factory = new ConnectionFactory { Uri = new Uri("amqp://guest:guest@localhost:5672"), DispatchConsumersAsync = true };

        static async Task Main(string[] args)
        {
            var handler = new GreetingHandler();

            var connection = Factory.CreateConnection();

            var channel = connection.CreateModel();
            var bus = new BusModel(channel);
            var rpc = new RpcModel(channel);

            await bus.RegisterHandlerAsync("toxon.micro.greetings.greetingevent", "toxon.micro.greetingevent", request => HandleEvent(handler, request));
            await rpc.RegisterHandlerAsync("toxon.micro.greetingrequest", request => HandleRequest(handler, request));

            Console.WriteLine("Press ENTER to exit.");
            Console.ReadLine();

            channel.Close();
            connection.Close();
        }

        private static void HandleEvent(GreetingHandler handler, Message message)
        {
            var request = JsonMessage.Read<GreetingEvent>(message);

            handler.Handle(request);
        }

        private static Message HandleRequest(GreetingHandler handler, Message message)
        {
            var request = JsonMessage.Read<GreetingRequest>(message);

            var response = handler.Handle(request);

            return JsonMessage.Write(response);
        }
    }
}
