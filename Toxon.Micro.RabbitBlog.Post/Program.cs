using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ;
using EasyNetQ.ConnectionString;
using Toxon.Micro.RabbitBlog.All;
using Toxon.Micro.RabbitBlog.Post.Inbound;
using Toxon.Micro.RabbitBlog.Routing.Json;
using Toxon.Micro.RabbitBlog.Routing.Patterns;

namespace Toxon.Micro.RabbitBlog.Post
{
    class Program
    {
        private const string ServiceName = "post.v1";

        private static readonly ConnectionConfiguration RabbitConfig = new ConnectionStringParser().Parse("amqp://guest:guest@localhost:5672");

        static async Task Main(string[] args)
        {
            var model = ModelFactory.Create(ServiceName, RabbitConfig);

            var logic = new BusinessLogic();

            await model.RegisterHandlerAsync(RouterPatternParser.Parse("post:entry"), (PostEntryRequest request, CancellationToken _) => logic.HandlePostEntryAsync(model, request));

            Console.WriteLine("Running Post... press enter to exit!");
            Console.ReadLine();
        }
    }
}
