using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.Routing.Json;
using Toxon.Micro.RabbitBlog.Routing.Patterns;
using Toxon.Micro.RabbitBlog.Routing.RouteSelection;

namespace Toxon.Micro.RabbitBlog.Routing
{
    public class Router<TData>
    {
        private int _entryCounter;
        private readonly ConcurrentDictionary<int, Entry> _routes = new ConcurrentDictionary<int, Entry>();
        private readonly IRouteSelectionStrategy<TData> _routeSelectionStrategy;

        public Router(IRouteSelectionStrategy<TData> routeSelectionStrategy)
        {
            _routeSelectionStrategy = routeSelectionStrategy;
        }

        public bool IsRegistered(string serviceKey, IRequestMatcher route)
        {
            return _routes.Values.Any(x => x.ServiceKey == serviceKey && RequestMatcherEqualityComparer.Instance.Equals(x.Route, route));
        }

        public int Register(string serviceKey, IRequestMatcher route, TData data)
        {
            var entryId = Interlocked.Increment(ref _entryCounter);
            if (!_routes.TryAdd(entryId, new Entry(serviceKey, route, data)))
            {
                throw new InvalidOperationException();
            }
            return entryId;
        }

        public void Unregister(int entryId)
        {
            if (!_routes.TryRemove(entryId, out _))
            {
                throw new InvalidOperationException();
            }
        }

        public class Entry
        {
            public Entry(string serviceKey, IRequestMatcher route, TData data)
            {
                ServiceKey = serviceKey;
                Route = route;
                Data = data;
            }

            public string ServiceKey { get; }
            public IRequestMatcher Route { get; }
            public TData Data { get; }
        }

        public IReadOnlyCollection<Entry> Match(Message message)
        {
            var fields = JsonMessage.Read<Dictionary<string, object>>(message);

            fields = new Dictionary<string, object>(fields, StringComparer.InvariantCultureIgnoreCase);

            return _routeSelectionStrategy.Select(_routes.Values.ToList(), fields);
        }
    }

    internal class RequestMatcherEqualityComparer : IEqualityComparer<IRequestMatcher>
    {
        public static readonly RequestMatcherEqualityComparer Instance = new RequestMatcherEqualityComparer();
        
        public bool Equals(IRequestMatcher x, IRequestMatcher y)
        {
            // TODO yeah...
            return x?.ToString() == y?.ToString();
        }

        public int GetHashCode(IRequestMatcher obj)
        {
            return obj?.ToString()?.GetHashCode() ?? 0;
        }
    }
}
