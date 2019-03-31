using Polly;
using Toxon.Micro.RabbitBlog.Core.Routing;

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
