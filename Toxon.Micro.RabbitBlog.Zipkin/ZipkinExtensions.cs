using Toxon.Micro.RabbitBlog.Routing;

namespace Toxon.Micro.RabbitBlog.Zipkin
{
    public static class ZipkinExtensions
    {
        public static IRoutingSender ConfigureTracing(this IRoutingSender sender, string serviceName)
        {
            return new TracedRoutingSender(sender, serviceName);
        }
        public static IRoutingRegistration ConfigureTracing(this IRoutingRegistration registration, string serviceName)
        {
            return new TracedRoutingRegistration(registration, serviceName);
        }
    }
}
