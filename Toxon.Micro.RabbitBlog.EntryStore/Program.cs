using System;
using RabbitMQ.Client;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.Core.Json;
using Toxon.Micro.RabbitBlog.Core.Patterns;
using Toxon.Micro.RabbitBlog.EntryStore.Inbound;

namespace Toxon.Micro.RabbitBlog.EntryStore
{
    class Program
    {
        private static readonly ConnectionFactory Factory = new ConnectionFactory { Uri = new Uri("amqp://guest:guest@localhost:5672"), DispatchConsumersAsync = true };

        static void Main(string[] args)
        {
            var logic = new BusinessLogic();

            var connection = Factory.CreateConnection();
            var channel = connection.CreateModel();

            var model = new RoutingModel(channel);
            model.HandleAsync("entry-store.v1", RouterPatternParser.Parse("store:*,kind:entry"), (StoreRequest request) => logic.HandleStoreAsync(request));
            
            Console.WriteLine("Running EntryStore... press enter to exit!");
            Console.ReadLine();

            channel.Close();
            connection.Close();
        }
    }
}
