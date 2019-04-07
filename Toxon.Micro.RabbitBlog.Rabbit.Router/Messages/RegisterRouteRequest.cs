using Toxon.Micro.RabbitBlog.Routing;
using Toxon.Micro.RabbitBlog.Routing.Patterns;

namespace Toxon.Micro.RabbitBlog.Rabbit.Router.Messages
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