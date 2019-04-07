using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Toxon.Micro.RabbitBlog.Routing.RouteSelection
{
    public class RandomRouteSelectionStrategy<TData> : IRouteSelectionStrategy<TData>
    {
        private static int _seed = new Random().Next();
        private static readonly ThreadLocal<Random> Random = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref _seed)));

        public IReadOnlyCollection<Router<TData>.Entry> Select(IReadOnlyCollection<Router<TData>.Entry> entries, IReadOnlyDictionary<string, object> message)
        {
            var index = Random.Value.Next(0, entries.Count);
            return entries.Skip(index).Take(1).ToList();
        }
    }
}
