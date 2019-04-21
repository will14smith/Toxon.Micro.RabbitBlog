using Toxon.Micro.RabbitBlog.Routing.Patterns;

namespace Toxon.Micro.RabbitBlog.Serverless.Router
{
    public class RouterEntry
    {
        public RouterEntry(RouteType type, string arn, IRequestMatcher route)
        {
            Arn = arn;
            Route = route;
            Type = type;
        }

        public RouteType Type { get; }
        public string Arn { get; }
        public IRequestMatcher Route { get; }
    }
}