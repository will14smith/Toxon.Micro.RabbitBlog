﻿using System;
using System.Threading.Tasks;
using EasyNetQ;
using EasyNetQ.ConnectionString;
using Serilog;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.Core.Json;
using Toxon.Micro.RabbitBlog.Router.Messages;

namespace Toxon.Micro.RabbitBlog.Router
{
    class Program
    {
        private static readonly ConnectionConfiguration RabbitConfig = new ConnectionStringParser().Parse("amqp://guest:guest@localhost:5672");

        static async Task Main(string[] args)
        {
            var rabbitBus = RabbitHutch.CreateBus(RabbitConfig, _ => { });

            var logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            var bus = new BusModel(rabbitBus.Advanced);
            var rpc = new RpcModel(rabbitBus.Advanced);

            var logic = new BusinessLogic(bus, rpc, logger);

            await rpc.RegisterHandlerAsync("toxon.micro.router.register", async (requestMessage, _) =>
            {
                var request = JsonMessage.Read<RegisterRouteRequest>(requestMessage);

                var response = await logic.RegisterRoute(request);

                return JsonMessage.Write(new RegisterRouteResponse { Done = response });
            });

            await bus.RegisterHandlerAsync("toxon.micro.router.route", (message, _) => logic.RouteBusMessage(message));
            await rpc.RegisterHandlerAsync("toxon.micro.router.route", (message, _) => logic.RouteRpcMessage(message));

            logic.Start();
            
            Console.WriteLine("Running Router... press enter to exit!");
            Console.ReadLine();
        }
    }
}
