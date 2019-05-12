using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.Routing;
using Toxon.Micro.RabbitBlog.Routing.Json;
using Toxon.Micro.RabbitBlog.Routing.Patterns;
using Toxon.Micro.RabbitBlog.Routing.RouteSelection;

namespace Toxon.Micro.RabbitBlog.Local.Host
{
    internal class LocalModel : IRoutingSender, IRoutingRegistration
    {
        private readonly ILogger _logger;

        private readonly Router<BusRoutingData> _busRouter = new Router<BusRoutingData>(new CompositeRouteSelectionStrategy<BusRoutingData>(
            new MatchingRoutesSelectionStrategy<BusRoutingData>(),
            new TopScoringRoutesSelectionStrategy<BusRoutingData>(new RouteScoreComparer())
        ));
        private readonly Router<RpcRoutingData> _rpcRouter = new Router<RpcRoutingData>(new CompositeRouteSelectionStrategy<RpcRoutingData>(
            new MatchingRoutesSelectionStrategy<RpcRoutingData>(),
            new TopScoringRoutesSelectionStrategy<RpcRoutingData>(new RouteScoreComparer()),
            new RandomRouteSelectionStrategy<RpcRoutingData>()
        ));

        public LocalModel(ILogger logger)
        {
            _logger = logger;
        }

        public async Task SendAsync(Message message, CancellationToken cancellationToken = default)
        {
            var routes = _busRouter.Match(message);

            if (!routes.Any())
            {
                var fields = JsonMessage.Read<Dictionary<string, object>>(message);

                _logger.Information("Failed to match any routes for bus message: {fields}", fields);
            }

            foreach (var route in routes)
            {
                await route.Data.Handler(message, cancellationToken);
            }
        }

        public Task<Message> CallAsync(Message message, CancellationToken cancellationToken = default)
        {
            var routes = _rpcRouter.Match(message);

            if (!routes.Any())
            {
                var fields = JsonMessage.Read<Dictionary<string, object>>(message);

                _logger.Error("Failed to match any routes for RPC message: {fields}", fields);

                var fieldsString = string.Join(", ", fields.Select(x => $"{x.Key}:{x.Value}"));
                throw new Exception($"Failed to match any routes for RPC message: {fieldsString}");
            }

            var route = routes.Single();

            return route.Data.Handler(message, cancellationToken);
        }

        public Task RegisterBusHandlerAsync(IRequestMatcher pattern, Func<Message, CancellationToken, Task> handler, RouteExecution execution = RouteExecution.Asynchronous, RouteMode mode = RouteMode.Observe, CancellationToken cancellationToken = default)
        {
            _busRouter.Register(string.Empty, pattern, new BusRoutingData(handler));
            return Task.CompletedTask;
        }

        public Task RegisterRpcHandlerAsync(IRequestMatcher pattern, Func<Message, CancellationToken, Task<Message>> handler, RouteExecution execution = RouteExecution.Synchronous, RouteMode mode = RouteMode.Capture, CancellationToken cancellationToken = default)
        {
            _rpcRouter.Register(string.Empty, pattern, new RpcRoutingData(handler));
            return Task.CompletedTask;
        }

        private class BusRoutingData
        {
            public Func<Message, CancellationToken, Task> Handler { get; }

            public BusRoutingData(Func<Message, CancellationToken, Task> handler)
            {
                Handler = handler;
            }
        }
        private class RpcRoutingData
        {
            public Func<Message, CancellationToken, Task<Message>> Handler { get; }

            public RpcRoutingData(Func<Message, CancellationToken, Task<Message>> handler)
            {
                Handler = handler;
            }
        }
    }
}