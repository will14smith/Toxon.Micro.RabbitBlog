using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.Front.Http;

namespace Toxon.Micro.RabbitBlog.Front
{
    class Program
    {
        private static readonly ConnectionFactory Factory = new ConnectionFactory { Uri = new Uri("amqp://guest:guest@localhost:5672"), DispatchConsumersAsync = true };

        static void Main(string[] args)
        {
            var connection = Factory.CreateConnection();
            var channel = connection.CreateModel();

            var model = new RoutingModel(channel);

            new WebHostBuilder()
                .UseKestrel(k => k.ListenLocalhost(8500))
                .ConfigureServices(services => services.AddSingleton(model))
                .UseStartup<Startup>()
                .Start();

            Console.WriteLine("Running Front... press enter to exit!");
            Console.ReadLine();

            channel.Close();
            connection.Close();
        }
    }
}
