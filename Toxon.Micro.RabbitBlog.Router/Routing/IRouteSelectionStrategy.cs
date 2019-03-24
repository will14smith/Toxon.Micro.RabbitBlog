using System.Collections.Generic;

namespace Toxon.Micro.RabbitBlog.Router.Routing
{
    internal interface IRouteSelectionStrategy<TData>
    {
        IReadOnlyCollection<Router<TData>.Entry> Select(IReadOnlyCollection<Router<TData>.Entry> entries, IReadOnlyDictionary<string, object> message);
    }
}
