using System.Threading;
using EasyNetQ;
using Polly;
using Toxon.Micro.RabbitBlog.Rabbit;
using Toxon.Micro.RabbitBlog.Resilience;
using Toxon.Micro.RabbitBlog.Routing;
using Toxon.Micro.RabbitBlog.Zipkin;

namespace Toxon.Micro.RabbitBlog.All
{
    public static class ModelFactory
    {
        public static IRoutingModel Create(string serviceKey, ConnectionConfiguration rabbitConfig)
        {
            var bus = RabbitHutch.CreateBus(rabbitConfig, _ => { });

            Thread.Sleep(1500);

            return new RoutingModel(serviceKey, bus.Advanced)
                .ConfigureTracing(serviceKey)
                .ConfigurePolicy(Policy.NoOpAsync())
                ;
        }
    }
}
