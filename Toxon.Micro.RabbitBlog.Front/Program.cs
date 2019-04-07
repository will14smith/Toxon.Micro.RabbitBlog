using System;
using System.Threading.Tasks;
using EasyNetQ;
using EasyNetQ.ConnectionString;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Toxon.Micro.RabbitBlog.All;
using Toxon.Micro.RabbitBlog.Front.Http;

namespace Toxon.Micro.RabbitBlog.Front
{
    class Program
    {
        private static readonly ConnectionConfiguration RabbitConfig = new ConnectionStringParser().Parse("amqp://guest:guest@localhost:5672");

        static async Task Main(string[] args)
        {
            var model = ModelFactory.Create(Startup.ServiceName, RabbitConfig);
            
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
