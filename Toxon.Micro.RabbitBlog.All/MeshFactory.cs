using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Toxon.Micro.RabbitBlog.Mesh;
using Toxon.Micro.RabbitBlog.Resilience;
using Toxon.Micro.RabbitBlog.Routing;
using Toxon.Micro.RabbitBlog.Zipkin;
using Toxon.Swim.Models;

namespace Toxon.Micro.RabbitBlog.All
{
    public static class MeshFactory
    {
        private class WellKnownHost : ISwimBootstrapper
        {
            public IReadOnlyCollection<SwimHost> GetWellKnownHosts()
            {
                return new[]
                {
                    // TODO ...
                    new SwimHost(new IPEndPoint(IPAddress.Loopback, 17999)),
                };
            }
        }

        public static async Task<IRoutingModel> CreateAsync(string serviceKey)
        {
            var model = new RoutingModel(serviceKey, new WellKnownHost(), new RoutingModelOptions());

            Thread.Sleep(1500);
            await model.StartAsync(); 

            return model
                .ConfigureTracing(serviceKey)
                .ConfigurePolicy(Policy.NoOpAsync())
                ;
        }
    }
}
