using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ;
using EasyNetQ.ConnectionString;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Toxon.Micro.RabbitBlog.Core.Routing;
using Toxon.Micro.RabbitBlog.Front.Http;

namespace Toxon.Micro.RabbitBlog.Front
{
    class Program
    {
        private static readonly ConnectionConfiguration RabbitConfig = new ConnectionStringParser().Parse("amqp://guest:guest@localhost:5672");

        static async Task Main(string[] args)
        {
            var bus = RabbitHutch.CreateBus(RabbitConfig, _ => { });

            Thread.Sleep(1000);

            var model = new RoutingModel(Startup.ServiceName, bus.Advanced);

            new WebHostBuilder()
                .UseKestrel(k => k.ListenLocalhost(8500))
                .ConfigureServices(services => services.AddSingleton<IRoutingModel>(model))
                .UseStartup<Startup>()
                .Start();

            Console.WriteLine("Running Front... press enter to exit!");
            Console.ReadLine();
        }
    }
}
