using Polly;
using Toxon.Micro.RabbitBlog.Routing;

namespace Toxon.Micro.RabbitBlog.Resilience
{
    public static class RoutingModelExtensions
    {
        public static IRoutingModel ConfigurePolicy(this IRoutingModel inputModel, AsyncPolicy policy)
        {
            return new PollyRoutingModel(inputModel, policy);
        }
    }
}
