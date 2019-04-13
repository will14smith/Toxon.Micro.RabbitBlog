using Toxon.Micro.RabbitBlog.Routing;
using Toxon.Swim.Models;

namespace Toxon.Micro.RabbitBlog.Mesh
{
    internal class RoutingData
    {
        public RoutingData(SwimHost host, int routeId, RouteExecution execution, RouteMode mode)
        {
            Host = host;
            RouteId = routeId;
            Execution = execution;
            Mode = mode;
        }

        public SwimHost Host { get; }
        public int RouteId { get; }
        public RouteExecution Execution { get; }
        public RouteMode Mode { get; }
    }
}