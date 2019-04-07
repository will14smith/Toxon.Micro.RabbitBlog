using System;
using System.Collections.Generic;
using System.Linq;

namespace Toxon.Micro.RabbitBlog.Routing.RouteSelection
{
    public class WeightedRandomRouteSelectionStrategy<TData> : IRouteSelectionStrategy<TData>
    {
        private readonly Func<Router<TData>.Entry, int> _weight;

        private readonly RandomRouteSelectionStrategy<TData> _inner = new RandomRouteSelectionStrategy<TData>();
        
        public WeightedRandomRouteSelectionStrategy(Func<Router<TData>.Entry, int> weight)
        {
            _weight = weight;
        }

        public IReadOnlyCollection<Router<TData>.Entry> Select(IReadOnlyCollection<Router<TData>.Entry> entries, IReadOnlyDictionary<string, object> message)
        {
            var weightedEntries = new List<Router<TData>.Entry>();

            foreach (var entry in entries)
            {
                weightedEntries.AddRange(Enumerable.Repeat(entry, _weight(entry)));
            }

            return _inner.Select(weightedEntries, message);
        }
    }
}