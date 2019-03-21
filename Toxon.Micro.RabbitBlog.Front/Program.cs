using System;
using Microsoft.AspNetCore.Hosting;
using Toxon.Micro.RabbitBlog.Front.Http;

namespace Toxon.Micro.RabbitBlog.Front
{
    class Program
    {
        static void Main(string[] args)
        {
            // TODO define routing model

            new WebHostBuilder()
                .UseKestrel(k => k.ListenLocalhost(8500))
                // TODO .ConfigureServices(services => services.AddSingleton(model))
                .UseStartup<Startup>()
                .Start();

            Console.WriteLine("Running Front... press enter to exit!");
            Console.ReadLine();
        }
    }
}
