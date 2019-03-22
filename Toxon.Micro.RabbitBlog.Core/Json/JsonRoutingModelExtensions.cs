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

        public static Task HandleAsync<T>(this IRoutingModel model, string serviceKey, IRequestMatcher pattern, Func<T, Task> handle)
        {
            return model.HandleAsync(serviceKey, pattern, requestMessage =>
            {
                var request = JsonMessage.Read<T>(requestMessage);
                return handle(request);
            });
        }
        public static Task HandleAsync<TRequest, TResponse>(this IRoutingModel model, string serviceKey, IRequestMatcher pattern, Func<TRequest, Task<TResponse>> handle)
        {
            return model.HandleAsync(serviceKey, pattern, async requestMessage =>
            {
                var request = JsonMessage.Read<TRequest>(requestMessage);
                var response = await handle(request);
                return JsonMessage.Write(response);
            });
        }
    }
}
