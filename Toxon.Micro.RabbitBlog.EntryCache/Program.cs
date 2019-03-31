using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ;
using EasyNetQ.ConnectionString;
using Toxon.Micro.RabbitBlog.Core.Json;
using Toxon.Micro.RabbitBlog.Core.Patterns;
using Toxon.Micro.RabbitBlog.Core.Routing;
using Toxon.Micro.RabbitBlog.EntryCache.Messages;
using Toxon.Micro.RabbitBlog.Zipkin;
using zipkin4net;

namespace Toxon.Micro.RabbitBlog.EntryCache
{
    class Program
    {
        private const string ServiceName = "entry-cache.v1";

        private static readonly ConnectionConfiguration RabbitConfig = new ConnectionStringParser().Parse("amqp://guest:guest@localhost:5672");

        static async Task Main(string[] args)
        {
            var bus = RabbitHutch.CreateBus(RabbitConfig, _ => { });

            Thread.Sleep(1500);

            var model = new RoutingModel(ServiceName, bus.Advanced)
                .ConfigureTracing(ServiceName);

            var logic = new BusinessLogic(model);
            
            await model.RegisterHandlerAsync(RouterPatternParser.Parse("store:*,kind:entry"), (StoreRequest request, CancellationToken _) => logic.HandleStoreAsync(request));

            Console.WriteLine("Running EntryCache... press enter to exit!");
            Console.ReadLine();
        }
    }

    internal class ConsoleLogger : ILogger
    {
        public void LogInformation(string message)
        {
            Console.WriteLine(message);
        }

        public void LogWarning(string message)
        {
            Console.WriteLine(message);
        }

        public void LogError(string message)
        {
            Console.WriteLine(message);
        }
    }
}
