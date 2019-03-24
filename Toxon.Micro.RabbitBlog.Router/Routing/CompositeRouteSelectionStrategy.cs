using System.Collections.Generic;
using System.Linq;

namespace Toxon.Micro.RabbitBlog.Router.Routing
{
    internal class CompositeRouteSelectionStrategy<TData> : IRouteSelectionStrategy<TData>
    {
        private readonly IReadOnlyCollection<IRouteSelectionStrategy<TData>> _strategies;

        public CompositeRouteSelectionStrategy(params IRouteSelectionStrategy<TData>[] strategies)
            : this((IReadOnlyCollection<IRouteSelectionStrategy<TData>>)strategies) { }
        public CompositeRouteSelectionStrategy(IReadOnlyCollection<IRouteSelectionStrategy<TData>> strategies)
        {
            _strategies = strategies;
        }

        public IReadOnlyCollection<Router<TData>.Entry> Select(IReadOnlyCollection<Router<TData>.Entry> entries, IReadOnlyDictionary<string, object> message)
        {
            return _strategies.Aggregate(entries, (current, strategy) => strategy.Select(current, message));
        }
    }
}
