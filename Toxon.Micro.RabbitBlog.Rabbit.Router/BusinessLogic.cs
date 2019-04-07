using System.Linq;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Events;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.Rabbit.Router.Messages;
using Toxon.Micro.RabbitBlog.Routing;
using Toxon.Micro.RabbitBlog.Routing.RouteSelection;

namespace Toxon.Micro.RabbitBlog.Rabbit.Router
{
    internal class BusinessLogic
    {
        private readonly IBusModel _bus;
        private readonly IRpcModel _rpc;

        private readonly ServiceHealthTracker _tracker;
        private readonly Router<RoutingData> _router;

        private readonly Logger _logger;

        public BusinessLogic(IBusModel bus, IRpcModel rpc, Logger logger)
        {
            _bus = bus;
            _rpc = rpc;

            _tracker = new ServiceHealthTracker(rpc, logger);
            _router = new Router<RoutingData>(new CompositeRouteSelectionStrategy<RoutingData>(
                new MatchingRoutesSelectionStrategy<RoutingData>(),
                new TopScoringRoutesSelectionStrategy<RoutingData>(new RouteScoreComparer()),
                new WeightedRandomRouteSelectionStrategy<RoutingData>(x => _tracker.GetServiceCount(x.ServiceKey))
            ));

            _logger = logger;
        }

        public void Start()
        {
            _tracker.Start();
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
            await _tracker.RegisterAsync(request.ServiceKey, request.ServiceHealthEndpoint);

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
