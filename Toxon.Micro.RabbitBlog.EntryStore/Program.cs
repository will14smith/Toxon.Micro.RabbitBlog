using System;
using System.Threading;
using System.Threading.Tasks;
using Toxon.Micro.RabbitBlog.All;
using Toxon.Micro.RabbitBlog.EntryStore.Inbound;
using Toxon.Micro.RabbitBlog.Routing.Json;
using Toxon.Micro.RabbitBlog.Routing.Patterns;

namespace Toxon.Micro.RabbitBlog.EntryStore
{
    class Program
    {
        private const string ServiceName = "entry-store.v2";

        static async Task Main(string[] args)
        {
            var model = await ModelFactory.CreateAsync(ServiceName);

            var logic = new BusinessLogic();
            
            await model.RegisterHandlerAsync(RouterPatternParser.Parse("store:*,kind:entry,cache:true"), (StoreRequest request, CancellationToken _) => logic.HandleStoreAsync(request));
            await model.RegisterHandlerAsync(RouterPatternParser.Parse("store:*,kind:entry"), (StoreRequest request, CancellationToken _) => logic.HandleStoreAsync(request));

            Console.WriteLine("Running EntryStore... press enter to exit!");
            Console.ReadLine();
        }
    }
}
