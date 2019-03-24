using System.Linq;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Events;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.Core.Routing;
using Toxon.Micro.RabbitBlog.Router.Messages;
using Toxon.Micro.RabbitBlog.Router.Routing;

namespace Toxon.Micro.RabbitBlog.Router
{
    internal class BusinessLogic
    {
        private readonly BusModel _bus;
        private readonly RpcModel _rpc;

        private readonly Router<RoutingData> _router = new Router<RoutingData>(new CompositeRouteSelectionStrategy<RoutingData>(
            new MatchingRoutesSelectionStrategy<RoutingData>(),
            new TopScoringRoutesSelectionStrategy<RoutingData>(new RouteScoreComparer()),
            new RandomRouteSelectionStrategy<RoutingData>()
        ));

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
            var routes = _router.Match(message);

            await Task.WhenAll(routes.Select(route =>
            {
                _logger.Write(LogEventLevel.Information, "Routing event to {serviceKey} via {routeKey}", route.ServiceKey, route.Data.RouteKey);

                return _bus.SendAsync(route.Data.RouteKey, message);
            }));
        }

        public async Task<Message> RouteRpcMessage(Message message)
        {
            var route = _router.Match(message).Single();

            _logger.Write(LogEventLevel.Information, "Routing rpc call to {serviceKey} via {routeKey}", route.ServiceKey, route.Data.RouteKey);

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
