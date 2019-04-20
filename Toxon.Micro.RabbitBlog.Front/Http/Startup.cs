using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Toxon.Micro.RabbitBlog.Front.Outbound;
using Toxon.Micro.RabbitBlog.Routing;
using Toxon.Micro.RabbitBlog.Routing.Json;
using zipkin4net.Middleware;

namespace Toxon.Micro.RabbitBlog.Front.Http
{
    public class Startup
    {
        internal const string ServiceName = "front.v1";

        public void Configure(IApplicationBuilder app, IRoutingSender sender)
        {   
            app.UseTracing(ServiceName);

            app.Get("/list", context => HandleList(sender, context));
            app.Get("/search", context => HandleSearch(sender, context));
            app.Post("/new", context => HandleNewAsync(sender, context));
        }

        private static async Task HandleList(IRoutingSender sender, HttpContext context)
        {
            var entries = await sender.CallAsync<List<EntryResponse>>(new ListEntriesFromStore());
            var response = entries.Select(entry => new Entry
            {
                Id = entry.Id,
                User = entry.User,
                Text = entry.Text,
            });

            await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
        }

        private static async Task HandleSearch(IRoutingSender sender, HttpContext context)
        {
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

        private static async Task HandleNewAsync(IRoutingSender sender, HttpContext context)
        {
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
