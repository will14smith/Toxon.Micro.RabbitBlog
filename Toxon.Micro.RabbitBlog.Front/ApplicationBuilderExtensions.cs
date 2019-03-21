using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Toxon.Micro.RabbitBlog.Front
{
    public static class ApplicationBuilderExtensions
    {
        public static void Get(this IApplicationBuilder app, string path, RequestDelegate handler)
        {
            app.MapWhen(
                ctx => ctx.Request.Path == path && ctx.Request.Method == "GET",
                innerApp => innerApp.Run(handler)
            );
        }
        public static void Post(this IApplicationBuilder app, string path, RequestDelegate handler)
        {
            app.MapWhen(
                ctx => ctx.Request.Path == path && ctx.Request.Method == "POST",
                innerApp => innerApp.Run(handler)
            );
        }

    }
}
