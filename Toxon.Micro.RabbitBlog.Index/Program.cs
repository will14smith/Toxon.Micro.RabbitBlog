using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ;
using EasyNetQ.ConnectionString;
using Toxon.Micro.RabbitBlog.All;
using Toxon.Micro.RabbitBlog.Index.Inbound;
using Toxon.Micro.RabbitBlog.Index.Translation;
using Toxon.Micro.RabbitBlog.Routing.Json;
using Toxon.Micro.RabbitBlog.Routing.Patterns;

namespace Toxon.Micro.RabbitBlog.Index
{
    class Program
    {
        private const string ServiceName = "index.v1";

        private static readonly ConnectionConfiguration RabbitConfig = new ConnectionStringParser().Parse("amqp://guest:guest@localhost:5672");

        static async Task Main(string[] args)
        {
            var model = ModelFactory.Create(ServiceName, RabbitConfig);

            var logic = new BusinessLogic();

            await model.RegisterHandlerAsync(RouterPatternParser.Parse("search:insert"), (SearchInsertRequest request, CancellationToken _) => logic.HandleInsert(request));
            await model.RegisterHandlerAsync(RouterPatternParser.Parse("search:query"), (SearchQueryRequest request, CancellationToken _) => logic.HandleQuery(request));

            // translation
            await model.RegisterHandlerAsync(RouterPatternParser.Parse("info:entry"), (InfoEntryRequest request, CancellationToken _) =>
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
