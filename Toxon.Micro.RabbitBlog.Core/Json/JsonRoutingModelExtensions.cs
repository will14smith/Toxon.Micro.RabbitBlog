using System;
using System.Threading.Tasks;
using Toxon.Micro.RabbitBlog.Core.Patterns;
using Toxon.Micro.RabbitBlog.Core.Routing;

namespace Toxon.Micro.RabbitBlog.Core.Json
{
    public static class JsonRoutingModelExtensions
    {
        public static async Task SendAsync(this IRoutingModel model, object request)
        {
            var message = JsonMessage.Write(request);

            await model.SendAsync(message);
        }
        public static async Task<TResponse> CallAsync<TResponse>(this IRoutingModel model, object request)
        {
            var requestMessage = JsonMessage.Write(request);

            var responseMessage = await model.CallAsync(requestMessage);

            return JsonMessage.Read<TResponse>(responseMessage);
        }

        public static Task SendAsync<T>(this IRoutingModel model, T request)
        {
            return SendAsync(model, (object)request);
        }
        public static Task<TResponse> CallAsync<TRequest, TResponse>(this IRoutingModel model, TRequest request)
        {
            return CallAsync<TResponse>(model, request);
        }

        public static Task RegisterHandlerAsync<T>(this IRoutingModel model, IRequestMatcher pattern, Func<T, Task> handler)
        {
            return model.RegisterHandlerAsync(pattern, requestMessage =>
            {
                var request = JsonMessage.Read<T>(requestMessage);
                return handler(request);
            });
        }
        public static Task RegisterHandlerAsync<TRequest, TResponse>(this IRoutingModel model, IRequestMatcher pattern, Func<TRequest, Task<TResponse>> handler)
        {
            return model.RegisterHandlerAsync(pattern, async requestMessage =>
            {
                var request = JsonMessage.Read<TRequest>(requestMessage);
                var response = await handler(request);
                return JsonMessage.Write(response);
            });
        }
    }
}
