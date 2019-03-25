using Toxon.Micro.RabbitBlog.Core.Patterns;
using Toxon.Micro.RabbitBlog.Core.Routing;

namespace Toxon.Micro.RabbitBlog.Router.Messages
{
    internal class RegisterRouteRequest
    {
        public string ServiceKey { get; set; }
        public string RouteKey { get; set; }

        public string ServiceHealthEndpoint { get; set; }
        
        public IRequestMatcher RequestMatcher { get; set; }
        public RouteExecution Execution { get; set; }
        public RouteMode Mode { get; set; }
    }
}