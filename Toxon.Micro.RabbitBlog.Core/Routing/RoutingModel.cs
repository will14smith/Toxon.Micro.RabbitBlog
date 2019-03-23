using System;
using System.Threading.Tasks;
using EasyNetQ;
using RabbitMQ.Client;
using Toxon.Micro.RabbitBlog.Core.Json;
using Toxon.Micro.RabbitBlog.Core.Patterns;

namespace Toxon.Micro.RabbitBlog.Core.Routing
{
    public class RoutingModel : IRoutingModel
    {
        private readonly BusModel _bus;
        private readonly RpcModel _rpc;

        public RoutingModel(IAdvancedBus bus) : this(new BusModel(bus), new RpcModel(bus)) { }
        public RoutingModel(BusModel bus, RpcModel rpc)
        {
            _bus = bus;
            _rpc = rpc;
        }

        public async Task SendAsync(Message message)
        {
            await _bus.SendAsync("toxon.micro.router.route", message);
        }
        public async Task<Message> CallAsync(Message message)
        {
            return await _rpc.SendAsync("toxon.micro.router.route", message);
        }

        public async Task RegisterHandlerAsync(string serviceKey, IRequestMatcher pattern, Func<Message, Task> handler, RouteExecution execution = RouteExecution.Asynchronous, RouteMode mode = RouteMode.Observe)
        {
            // TODO better route key? needs to be consistent across a cluster of services per route
            var route = $"{serviceKey}-{pattern}";

            await _rpc.SendAsync("toxon.micro.router.register", JsonMessage.Write(new RegisterRoute
            {
                ServiceKey = serviceKey,
                RouteKey = route,

                RequestMatcher = pattern,
                Execution = execution,
                Mode = mode
            }));

            await _bus.RegisterHandlerAsync(route, handler);
        }
        public async Task RegisterHandlerAsync(string serviceKey, IRequestMatcher pattern, Func<Message, Task<Message>> handler, RouteExecution execution = RouteExecution.Synchronous, RouteMode mode = RouteMode.Capture)
        {
            // TODO better route key? needs to be consistent across a cluster of services per route
            var route = $"{serviceKey}-{pattern}";

            await _rpc.SendAsync("toxon.micro.router.register", JsonMessage.Write(new RegisterRoute
            {
                ServiceKey = serviceKey,
                RouteKey = route,

                RequestMatcher = pattern,
                Execution = execution,
                Mode = mode
            })).ConfigureAwait(false);

            await _rpc.RegisterHandlerAsync(route, handler);
        }

        private class RegisterRoute
        {
            public string ServiceKey { get; set; }
            public string RouteKey { get; set; }

            public IRequestMatcher RequestMatcher { get; set; }
            public RouteExecution Execution { get; set; }
            public RouteMode Mode { get; set; }
        }
    }
}
