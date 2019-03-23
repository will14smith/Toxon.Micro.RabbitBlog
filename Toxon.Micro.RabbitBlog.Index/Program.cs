using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyNetQ;
using EasyNetQ.ConnectionString;
using Toxon.Micro.RabbitBlog.Core.Json;
using Toxon.Micro.RabbitBlog.Core.Patterns;
using Toxon.Micro.RabbitBlog.Core.Routing;
using Toxon.Micro.RabbitBlog.Index.Inbound;
using Toxon.Micro.RabbitBlog.Index.Translation;

namespace Toxon.Micro.RabbitBlog.Index
{
    class Program
    {
        private static readonly ConnectionConfiguration RabbitConfig = new ConnectionStringParser().Parse("amqp://guest:guest@localhost:5672");

        static async Task Main(string[] args)
        {
            var bus = RabbitHutch.CreateBus(RabbitConfig, _ => { });

            var logic = new BusinessLogic();

            var model = new RoutingModel(bus.Advanced);

            await model.RegisterHandlerAsync("index.v1", RouterPatternParser.Parse("search:insert"), (SearchInsertRequest request) => logic.HandleInsert(request));
            await model.RegisterHandlerAsync("index.v1", RouterPatternParser.Parse("search:query"), (SearchQueryRequest request) => logic.HandleQuery(request));
            
            // translation
            await model.RegisterHandlerAsync("index.v1", RouterPatternParser.Parse("info:entry"), (InfoEntryRequest request) =>
            {
                var translatedRequest = new SearchInsertRequest
                {
                    Kind = "entry",
                    Id = request.Id,

                    Fields = new Dictionary<string, string>
                    {
                        {"User", request.User},
                        {"Text", request.Text},
                    }
                };

                return logic.HandleInsert(translatedRequest);
            });

            Console.WriteLine("Running Index... press enter to exit!");
            Console.ReadLine();
        }
    }
}
