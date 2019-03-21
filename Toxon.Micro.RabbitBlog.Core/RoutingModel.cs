using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using Toxon.Micro.RabbitBlog.Core.Json;
using Toxon.Micro.RabbitBlog.Core.Patterns;

namespace Toxon.Micro.RabbitBlog.Core
{
    public class RoutingModel : IRoutingModel
    {
        private readonly BusModel _bus;
        private readonly RpcModel _rpc;

        public RoutingModel(IModel model) : this(new BusModel(model), new RpcModel(model)) { }
        public RoutingModel(BusModel bus, RpcModel rpc)
        {
            _bus = bus;
            _rpc = rpc;
        }

        public Task SendAsync(Message message)
        {
            throw new NotImplementedException();
        }
        public Task<Message> CallAsync(Message message)
        {
            throw new NotImplementedException();
        }

        public async Task HandleAsync(string serviceKey, IRequestMatcher pattern, Func<Message, Task> handle)
        {
            // TODO better route key? needs to be consistent across a cluster of services per route
            var route = $"{serviceKey}-{pattern}";

            await _rpc.SendAsync("toxon.micro.router.register", JsonMessage.Write(new RegisterRoute
            {
                ServiceKey = serviceKey,
                RequestMatcher = pattern,
                RouteKey = route
            }));

            await _bus.RegisterHandlerAsync(serviceKey, route, handle);
        }
        public async Task HandleAsync(string serviceKey, IRequestMatcher pattern, Func<Message, Task<Message>> handle)
        {
            // TODO better route key? needs to be consistent across a cluster of services per route
            var route = $"{serviceKey}-{pattern}";

            await _rpc.SendAsync("toxon.micro.router.register", JsonMessage.Write(new RegisterRoute
            {
                ServiceKey = serviceKey,
                RequestMatcher = pattern,
                RouteKey = route
            }));

            await _rpc.RegisterHandlerAsync(route, handle);
        }

        private class RegisterRoute
        {
            public string ServiceKey { get; set; }
            public IRequestMatcher RequestMatcher { get; set; }
            public string RouteKey { get; set; }
        }
    }
}
