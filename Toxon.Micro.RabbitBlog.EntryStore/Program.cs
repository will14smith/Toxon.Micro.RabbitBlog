using System;
using RabbitMQ.Client;
using Toxon.Micro.RabbitBlog.Core;

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
            // TODO store:*,kind:entry => StoreRequest => logic.HandleStoreAsync
            
            Console.WriteLine("Running EntryStore... press enter to exit!");
            Console.ReadLine();

            channel.Close();
            connection.Close();
        }
    }
}
