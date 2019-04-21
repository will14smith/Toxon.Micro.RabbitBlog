using System.Collections.Generic;

namespace Toxon.Micro.RabbitBlog.Serverless.Router
{
    public class RouterConfig
    {
        public RouterConfig(IReadOnlyCollection<RouterEntry> routes)
        {
            Routes = routes;
        }

        public IReadOnlyCollection<RouterEntry> Routes { get; }
    }
}