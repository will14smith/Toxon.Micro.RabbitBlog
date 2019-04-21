using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Toxon.Micro.RabbitBlog.Front.Http;
using Toxon.Micro.RabbitBlog.Front.Outbound;
using Toxon.Micro.RabbitBlog.Plugins.Core;
using Toxon.Micro.RabbitBlog.Routing;
using Toxon.Micro.RabbitBlog.Routing.Json;
using zipkin4net.Middleware;
using SearchResult = Toxon.Micro.RabbitBlog.Front.Http.SearchResult;

namespace Toxon.Micro.RabbitBlog.Front
{
    [ServicePlugin(ServiceName, ServiceType.Http)]
    public class Startup
    {
        internal const string ServiceName = "front.v1";

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
        }

        public void Configure(IApplicationBuilder app, IRoutingSender sender)
        {
            app.UseTracing(ServiceName);

            var router = new RouteBuilder(app);

            router.MapGet("list", HandleList);
            router.MapGet("search", HandleSearch);
            router.MapPost("new", HandleNew);

            app.UseRouter(router.Build());
        }

        private static async Task HandleList(HttpContext context)
        {
            var sender = context.RequestServices.GetRequiredService<IRoutingSender>();

            var entries = await sender.CallAsync<List<EntryResponse>>(new ListEntriesFromStore());
            var response = entries.Select(entry => new Entry
            {
                Id = entry.Id,
                User = entry.User,
                Text = entry.Text,
            });

            await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
        }

        private static async Task HandleSearch(HttpContext context)
        {
            var sender = context.RequestServices.GetRequiredService<IRoutingSender>();

            var query = context.Request.Query["q"];
            var searchResponse = await sender.CallAsync<SearchResponse>(new SearchRequest { Kind = "entry", Query = query });
            var response = searchResponse.Results.Select(result => new SearchResult
            {
                Score = result.Score,
                Document = new Entry
                {
                    Id = result.Document.Id,
                    User = result.Document.Fields["user"],
                    Text = result.Document.Fields["text"],
                }
            });

            await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
        }

        private static async Task HandleNew(HttpContext context)
        {
            var sender = context.RequestServices.GetRequiredService<IRoutingSender>();

            var input = JsonSerializer.Create().Deserialize<EntryInput>(new JsonTextReader(new StreamReader(context.Request.Body)));

            var entry = await sender.CallAsync<EntryCreateResponse>(new SaveEntryToStore
            {
                User = input.User,
                Text = input.Text,
            });

            await context.Response.WriteAsync(JsonConvert.SerializeObject(entry));
        }
    }
}
