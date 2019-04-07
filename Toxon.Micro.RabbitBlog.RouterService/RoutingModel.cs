using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.Routing;
using Toxon.Micro.RabbitBlog.Routing.Json;
using Toxon.Micro.RabbitBlog.Routing.Patterns;

namespace Toxon.Micro.RabbitBlog.RouterService
{
    public class RoutingModel : IRoutingModel
    {
        private readonly string _serviceKey;
        private readonly BusModel _bus;
        private readonly RpcModel _rpc;

        private readonly Lazy<Task<string>> _serviceHealthEndpoint;

        public RoutingModel(string serviceKey, IAdvancedBus bus) : this(serviceKey, new BusModel(bus), new RpcModel(bus)) { }
        public RoutingModel(string serviceKey, BusModel bus, RpcModel rpc)
        {
            _serviceKey = serviceKey;
            _bus = bus;
            _rpc = rpc;

            _serviceHealthEndpoint = new Lazy<Task<string>>(InitializeHealthEndpointAsync);
        }

        public async Task SendAsync(Message message, CancellationToken cancellationToken = default)
        {
            await _bus.SendAsync("toxon.micro.router.route", message, cancellationToken);
        }
        public async Task<Message> CallAsync(Message message, CancellationToken cancellationToken = default)
        {
            return await _rpc.SendAsync("toxon.micro.router.route", message, cancellationToken);
        }

        public async Task RegisterHandlerAsync(IRequestMatcher pattern, Func<Message, CancellationToken, Task> handler, RouteExecution execution = RouteExecution.Asynchronous, RouteMode mode = RouteMode.Observe, CancellationToken cancellationToken = default)
        {
            // TODO better route key? needs to be consistent across a cluster of services per route
            var route = $"{_serviceKey}-{pattern}";

            await _rpc.SendAsync("toxon.micro.router.register", JsonMessage.Write(new RegisterRoute
            {
                ServiceKey = _serviceKey,
                RouteKey = route,

                ServiceHealthEndpoint = await _serviceHealthEndpoint.Value,

                RequestMatcher = pattern,
                Execution = execution,
                Mode = mode
            }), cancellationToken);

            await _bus.RegisterHandlerAsync(route, handler, cancellationToken);
        }
        public async Task RegisterHandlerAsync(IRequestMatcher pattern, Func<Message, CancellationToken, Task<Message>> handler, RouteExecution execution = RouteExecution.Synchronous, RouteMode mode = RouteMode.Capture, CancellationToken cancellationToken = default)
        {
            // TODO better route key? needs to be consistent across a cluster of services per route
            var route = $"{_serviceKey}-{pattern}";

            await _rpc.SendAsync("toxon.micro.router.register", JsonMessage.Write(new RegisterRoute
            {
                ServiceKey = _serviceKey,
                RouteKey = route,

                ServiceHealthEndpoint = await _serviceHealthEndpoint.Value,

                RequestMatcher = pattern,
                Execution = execution,
                Mode = mode
            }), cancellationToken).ConfigureAwait(false);

            await _rpc.RegisterHandlerAsync(route, handler, cancellationToken);
        }

        private class RegisterRoute
        {
            public string ServiceKey { get; set; }
            public string RouteKey { get; set; }

            public string ServiceHealthEndpoint { get; set; }

            public IRequestMatcher RequestMatcher { get; set; }
            public RouteExecution Execution { get; set; }
            public RouteMode Mode { get; set; }
        }
        
        #region Health 

        // TODO should this be in here?

        private async Task<string> InitializeHealthEndpointAsync()
        {
            var healthEndpoint = $"{_serviceKey}-{Guid.NewGuid()}";

            await _rpc.RegisterHandlerAsync(healthEndpoint, HandleHealthCheckAsync);

            return healthEndpoint;
        }

        private static Task<Message> HandleHealthCheckAsync(Message requestMessage, CancellationToken cancellationToken)
        {
            var request = JsonMessage.Read<HealthCheck>(requestMessage);

            var response = "unknown";
            if (request.Health == "ping")
            {
                response = "pong";
            }

            return Task.FromResult(JsonMessage.Write(new HealthCheck
            {
                Health = response,
                Nonce = request.Nonce,
            }));
        }

        private class HealthCheck
        {
            public string Health { get; set; }
            public long Nonce { get; set; }
        }

        #endregion
    }
}
