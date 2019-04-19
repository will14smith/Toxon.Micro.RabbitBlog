using System.Reflection;
using Toxon.Micro.RabbitBlog.Routing.Patterns;

namespace Toxon.Micro.RabbitBlog.Plugins.Reflection
{
    public class RouteMetadata
    {
        public RouteMetadata(IRequestMatcher route, MethodInfo method)
        {
            Route = route;
            Method = method;
        }

        public IRequestMatcher Route { get; }
        public MethodInfo Method { get; }
    }
}