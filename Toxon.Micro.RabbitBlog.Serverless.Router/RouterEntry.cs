using Toxon.Micro.RabbitBlog.Routing.Patterns;

namespace Toxon.Micro.RabbitBlog.Serverless.Router
{
    public class RouterEntry
    {
        public RouterEntry(string serviceKey, IRequestMatcher route, RouteTargetType targetType, string target)
        {
            ServiceKey = serviceKey;
            Route = route;
            TargetType = targetType;
            Target = target;
        }

        public string ServiceKey { get; }
        public IRequestMatcher Route { get; }
        public RouteTargetType TargetType { get; }
        public string Target { get; }
    }
}