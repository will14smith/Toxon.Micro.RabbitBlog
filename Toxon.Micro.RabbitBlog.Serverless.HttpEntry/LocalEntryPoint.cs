using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Toxon.Micro.RabbitBlog.Serverless.HttpEntry
{
    public class LocalEntryPoint
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .Build();
    }
}
