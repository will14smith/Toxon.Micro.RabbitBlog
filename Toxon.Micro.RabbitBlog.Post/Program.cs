using System;
using System.Threading.Tasks;
using EasyNetQ;
using EasyNetQ.ConnectionString;
using RabbitMQ.Client;
using Toxon.Micro.RabbitBlog.Core.Json;
using Toxon.Micro.RabbitBlog.Core.Patterns;
using Toxon.Micro.RabbitBlog.Core.Routing;
using Toxon.Micro.RabbitBlog.Post.Inbound;

namespace Toxon.Micro.RabbitBlog.Post
{
    class Program
    {
        private static readonly ConnectionConfiguration RabbitConfig = new ConnectionStringParser().Parse("amqp://guest:guest@localhost:5672");

        static async Task Main(string[] args)
        {
            var bus = RabbitHutch.CreateBus(RabbitConfig, _ => { });

            var logic = new BusinessLogic();

            var model = new RoutingModel(bus.Advanced);
            await model.HandleAsync("post.v1", RouterPatternParser.Parse("post:entry"), (PostEntryRequest request) => logic.HandlePostEntryAsync(model, request));

            Console.WriteLine("Running Post... press enter to exit!");
            Console.ReadLine();
        }
    }
}
