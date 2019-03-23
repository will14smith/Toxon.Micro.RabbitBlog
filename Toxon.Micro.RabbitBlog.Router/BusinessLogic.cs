using System;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Events;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.Core.Routing;
using Toxon.Micro.RabbitBlog.Router.Messages;

namespace Toxon.Micro.RabbitBlog.Router
{
    internal class BusinessLogic
    {
        private readonly BusModel _bus;
        private readonly RpcModel _rpc;

        private readonly Router<RoutingData> _router = new Router<RoutingData>();

        private readonly Logger _logger;

        // TODO ideally this wouldn't be coupled so tightly with transport (although it is probably fine in *this* service...)
        public BusinessLogic(BusModel bus, RpcModel rpc, Logger logger)
        {
            _bus = bus;
            _rpc = rpc;

            _logger = logger;
        }

        public async Task<bool> RegisterRoute(RegisterRouteRequest request)
        {
            if (_router.IsRegistered(request.ServiceKey, request.RequestMatcher))
            {
                return false;
            }

            _logger.Write(LogEventLevel.Information, "Service {serviceKey} is registering a route for {routingKey} ({execution} {mode})", request.ServiceKey, request.RequestMatcher, request.Execution, request.Mode);

            _router.Register(request.ServiceKey, request.RequestMatcher, new RoutingData
            {
                RouteKey = request.RouteKey,

                Execution = request.Execution,
                Mode = request.Mode,
            });
            
            return true;
        }

        public async Task RouteBusMessage(Message message)
        {
            var route = _router.Match(message);

            _logger.Write(LogEventLevel.Information, "Routing message to {serviceKey} via {routeKey}", route.ServiceKey, route.Data.RouteKey);

            await _bus.SendAsync(route.Data.RouteKey, message);
        }

        public async Task<Message> RouteRpcMessage(Message message)
        {
            var route = _router.Match(message);

            _logger.Write(LogEventLevel.Information, "Routing message to {serviceKey} via {routeKey}", route.ServiceKey, route.Data.RouteKey);

            var response = await _rpc.SendAsync(route.Data.RouteKey, message);
            
            return response;
        }
    }

    internal class RoutingData
    {
        public string RouteKey { get; set; }

        public RouteExecution Execution { get; set; }
        public RouteMode Mode { get; set; }

    }
}
