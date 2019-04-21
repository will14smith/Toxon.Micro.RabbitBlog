using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.Routing;
using Toxon.Micro.RabbitBlog.Routing.Patterns;
using Toxon.Micro.RabbitBlog.Routing.RouteSelection;

namespace Toxon.Micro.RabbitBlog.Serverless.Host
{
    internal class LocalModel : IRoutingSender, IRoutingRegistration
    {
        private readonly Router<BusRoutingData> _busRouter = CreateRouter<BusRoutingData>();
        private readonly Router<RpcRoutingData> _rpcRouter = CreateRouter<RpcRoutingData>();

        private static Router<T> CreateRouter<T>() =>
            new Router<T>(new CompositeRouteSelectionStrategy<T>(
                new MatchingRoutesSelectionStrategy<T>(),
                new TopScoringRoutesSelectionStrategy<T>(new RouteScoreComparer()),
                new RandomRouteSelectionStrategy<T>()
            ));

        public Task SendAsync(Message message, CancellationToken cancellationToken = default)
        {
            var route = _busRouter.Match(message).Single();
            return route.Data.Handler(message, cancellationToken);
        }

        public Task<Message> CallAsync(Message message, CancellationToken cancellationToken = default)
        {
            var route = _rpcRouter.Match(message).Single();
            return route.Data.Handler(message, cancellationToken);
        }

        public Task RegisterHandlerAsync(IRequestMatcher pattern, Func<Message, CancellationToken, Task> handler, RouteExecution execution = RouteExecution.Asynchronous, RouteMode mode = RouteMode.Observe, CancellationToken cancellationToken = default)
        {
            _busRouter.Register(string.Empty, pattern, new BusRoutingData(handler));
            return Task.CompletedTask;
        }

        public Task RegisterHandlerAsync(IRequestMatcher pattern, Func<Message, CancellationToken, Task<Message>> handler, RouteExecution execution = RouteExecution.Synchronous, RouteMode mode = RouteMode.Capture, CancellationToken cancellationToken = default)
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