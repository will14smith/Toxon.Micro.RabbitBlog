using System;
using System.Threading;
using System.Threading.Tasks;
using Toxon.Micro.RabbitBlog.All;
using Toxon.Micro.RabbitBlog.EntryCache.Messages;
using Toxon.Micro.RabbitBlog.Routing.Json;
using Toxon.Micro.RabbitBlog.Routing.Patterns;
using zipkin4net;

namespace Toxon.Micro.RabbitBlog.EntryCache
{
    class Program
    {
        private const string ServiceName = "entry-cache.v1";

        static async Task Main(string[] args)
        {
            var model = await MeshFactory.CreateAsync(ServiceName);

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
