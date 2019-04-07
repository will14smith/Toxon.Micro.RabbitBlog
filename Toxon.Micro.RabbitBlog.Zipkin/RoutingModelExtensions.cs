using Toxon.Micro.RabbitBlog.Routing;

namespace Toxon.Micro.RabbitBlog.Zipkin
{
    public static class RoutingModelExtensions
    {
        public static IRoutingModel ConfigureTracing(this IRoutingModel inputModel, string serviceName, string collectionUrl = "http://127.0.0.1:9411")
        {
            var model = new TracedRoutingModel(inputModel, serviceName);
            model.StartTracing(collectionUrl);
            return model;
        }
    }
}
