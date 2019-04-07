using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ;
using EasyNetQ.ConnectionString;
using Toxon.Micro.RabbitBlog.EntryStore.Inbound;
using Toxon.Micro.RabbitBlog.RouterService;
using Toxon.Micro.RabbitBlog.Routing.Json;
using Toxon.Micro.RabbitBlog.Routing.Patterns;
using Toxon.Micro.RabbitBlog.Zipkin;

namespace Toxon.Micro.RabbitBlog.EntryStore
{
    class Program
    {
        private const string ServiceName = "entry-store.v2";

        private static readonly ConnectionConfiguration RabbitConfig = new ConnectionStringParser().Parse("amqp://guest:guest@localhost:5672");

        static async Task Main(string[] args)
        {
            var bus = RabbitHutch.CreateBus(RabbitConfig, _ => { });

            Thread.Sleep(1500);

            var logic = new BusinessLogic();
            
            var model = new RoutingModel(ServiceName, bus.Advanced)
                .ConfigureTracing(ServiceName);

            await model.RegisterHandlerAsync(RouterPatternParser.Parse("store:*,kind:entry,cache:true"), (StoreRequest request, CancellationToken _) => logic.HandleStoreAsync(request));
            await model.RegisterHandlerAsync(RouterPatternParser.Parse("store:*,kind:entry"), (StoreRequest request, CancellationToken _) => logic.HandleStoreAsync(request));

            Console.WriteLine("Running EntryStore... press enter to exit!");
            Console.ReadLine();
        }
    }
}
