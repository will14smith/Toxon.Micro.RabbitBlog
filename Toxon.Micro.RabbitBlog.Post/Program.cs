using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.Core.Json;
using Toxon.Micro.RabbitBlog.Core.Patterns;
using Toxon.Micro.RabbitBlog.Post.Inbound;

namespace Toxon.Micro.RabbitBlog.Post
{
    class Program
    {
        private static readonly ConnectionFactory Factory = new ConnectionFactory { Uri = new Uri("amqp://guest:guest@localhost:5672"), DispatchConsumersAsync = true };

        static async Task Main(string[] args)
        {
            var logic = new BusinessLogic();

            var connection = Factory.CreateConnection();
            var channel = connection.CreateModel();

            var model = new RoutingModel(channel);
            await model.HandleAsync("post.v1", RouterPatternParser.Parse("post:entry"), (PostEntryRequest request) => logic.HandlePostEntryAsync(model, request));

            Console.WriteLine("Running Post... press enter to exit!");
            Console.ReadLine();

            channel.Close();
            connection.Close();
        }
    }
}
