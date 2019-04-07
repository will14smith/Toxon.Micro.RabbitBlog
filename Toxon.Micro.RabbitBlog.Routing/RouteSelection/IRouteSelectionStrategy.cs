using System.Collections.Generic;

namespace Toxon.Micro.RabbitBlog.Routing.RouteSelection
{
    public interface IRouteSelectionStrategy<TData>
    {
        IReadOnlyCollection<Router<TData>.Entry> Select(IReadOnlyCollection<Router<TData>.Entry> entries, IReadOnlyDictionary<string, object> message);
    }
}
