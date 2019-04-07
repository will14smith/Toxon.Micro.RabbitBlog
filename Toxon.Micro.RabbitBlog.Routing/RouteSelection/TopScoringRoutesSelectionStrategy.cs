using System.Collections.Generic;
using System.Linq;
using Toxon.Micro.RabbitBlog.Routing.Patterns;

namespace Toxon.Micro.RabbitBlog.Routing.RouteSelection
{
    public class TopScoringRoutesSelectionStrategy<TData> : IRouteSelectionStrategy<TData>
    {
        private readonly IComparer<IRequestMatcher> _scorer;

        public TopScoringRoutesSelectionStrategy(IComparer<IRequestMatcher> scorer)
        {
            _scorer = scorer;
        }

        public IReadOnlyCollection<Router<TData>.Entry> Select(IReadOnlyCollection<Router<TData>.Entry> entries, IReadOnlyDictionary<string, object> message)
        {
            var orderedEntries = entries.OrderByDescending(x => x.Route, _scorer).ToList();

            var selectedEntries = new List<Router<TData>.Entry>();
            if (entries.Count == 0) return selectedEntries;

            var headEntry = orderedEntries[0];
            selectedEntries.Add(headEntry);

            for (var i = 1; i < entries.Count; i++)
            {
                var entry = orderedEntries[i];

                if (_scorer.Compare(headEntry.Route, entry.Route) != 0)
                {
                    break;
                }

                selectedEntries.Add(entry);
            }

            return selectedEntries;
        }
    }
}