﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.Core.Json;
using Toxon.Micro.RabbitBlog.Front.Outbound;

namespace Toxon.Micro.RabbitBlog.Front.Http
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app, IRoutingModel model)
        {
            app.Get("/list", context => HandleList(model, context));
            app.Post("/new", context => HandleNewAsync(model, context));
        }

        private static async Task HandleList(IRoutingModel model, HttpContext context)
        {
            var entries = await model.CallAsync<List<EntryResponse>>(new ListEntriesFromStore());
            var response = entries.Select(entry => new Entry
            {
                Id = entry.Id,
                User = entry.User,
                Text = entry.Text,
            });

            await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
        }

        private static async Task HandleNewAsync(IRoutingModel model, HttpContext context)
        {
            var input = JsonSerializer.Create().Deserialize<EntryInput>(new JsonTextReader(new StreamReader(context.Request.Body)));

            var entry = await model.CallAsync<EntryCreateResponse>(new SaveEntryToStore
            {
                User = input.User,
                Text = input.Text,
            });

            await context.Response.WriteAsync(JsonConvert.SerializeObject(entry));
        }
    }
}
