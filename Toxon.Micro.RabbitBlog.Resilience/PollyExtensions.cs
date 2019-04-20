using Polly;
using Toxon.Micro.RabbitBlog.Routing;

namespace Toxon.Micro.RabbitBlog.Resilience
{
    public static class PollyExtensions
    {
        public static IRoutingSender ConfigurePolicy(this IRoutingSender sender, AsyncPolicy policy)
        {
            return new PollyRoutingSender(sender, policy);
        }
        public static IRoutingRegistration ConfigurePolicy(this IRoutingRegistration registration, AsyncPolicy policy)
        {
            return new PollyRoutingRegistration(registration, policy);
        }
    }
}
