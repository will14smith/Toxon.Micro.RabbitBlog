using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Toxon.Micro.RabbitBlog.All;
using Toxon.Micro.RabbitBlog.Front.Http;

namespace Toxon.Micro.RabbitBlog.Front
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var model = await MeshFactory.CreateAsync(Startup.ServiceName);
            
            new WebHostBuilder()
                .UseKestrel(k => k.ListenLocalhost(8500))
                .ConfigureServices(services => services.AddSingleton(model))
                .UseStartup<Startup>()
                .Start();

            Console.WriteLine("Running Front... press enter to exit!");
            Console.ReadLine();
        }
    }
}
