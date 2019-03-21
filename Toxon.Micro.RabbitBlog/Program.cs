using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.Messages;

namespace Toxon.Micro.RabbitBlog
{
    class Program
    {
        private static readonly ConnectionFactory Factory = new ConnectionFactory { Uri = new Uri("amqp://guest:guest@localhost:5672"), DispatchConsumersAsync = true };

        private static readonly ConcurrentDictionary<string, BasicDeliverEventArgs> Responses = new ConcurrentDictionary<string, BasicDeliverEventArgs>();

        static async Task Main(string[] args)
        {
            var connection = Factory.CreateConnection();

            var channel = connection.CreateModel();
            var bus = new BusModel(channel);
            var rpc = new RpcModel(channel);

            Console.WriteLine("Waiting 1 second...");
            Thread.Sleep(1000);

            Console.WriteLine("Sending event");
            await bus.SendAsync("toxon.micro.greetingevent", CreateGreetingEvent("Will 1"));
            Console.WriteLine("Sent event, requesting greeting");
            var response = await HandleGreetingResponse(rpc.SendAsync("toxon.micro.greetingrequest", CreateGreetingRequest("Will 2")));
            Console.WriteLine($"Greeting was: {response.Greeting}");

            Console.WriteLine("Press ENTER to exit.");
            Console.ReadLine();

            channel.Close();
            connection.Close();
        }

        private static Message CreateGreetingEvent(string name)
        {
            var message = new GreetingEvent { Name = name };

            return CreateRequest(message);
        }
        private static Message CreateGreetingRequest(string name)
        {
            var message = new GreetingRequest { Name = name };

            return CreateRequest(message);
        }
        private static Message CreateRequest(object message)
        {
            var content = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(content);

            return new Message(body);
        }

        private static async Task<GreetingResponse> HandleGreetingResponse(Task<Message> sendTask)
        {
            var response = await sendTask;

            return JsonMessage.Read<GreetingResponse>(response);
        }
    }
}
