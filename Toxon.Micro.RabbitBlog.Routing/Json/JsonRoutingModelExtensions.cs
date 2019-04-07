using System;
using System.Threading;
using System.Threading.Tasks;
using Toxon.Micro.RabbitBlog.Routing.Patterns;

namespace Toxon.Micro.RabbitBlog.Routing.Json
{
    public static class JsonRoutingModelExtensions
    {
        public static async Task SendAsync(this IRoutingModel model, object request, CancellationToken cancellationToken = default)
        {
            var message = JsonMessage.Write(request);

            await model.SendAsync(message, cancellationToken);
        }
        public static async Task<TResponse> CallAsync<TResponse>(this IRoutingModel model, object request, CancellationToken cancellationToken = default)
        {
            var requestMessage = JsonMessage.Write(request);

            var responseMessage = await model.CallAsync(requestMessage, cancellationToken);

            return JsonMessage.Read<TResponse>(responseMessage);
        }

        public static Task SendAsync<T>(this IRoutingModel model, T request, CancellationToken cancellationToken = default)
        {
            return SendAsync(model, (object)request, cancellationToken);
        }
        public static Task<TResponse> CallAsync<TRequest, TResponse>(this IRoutingModel model, TRequest request, CancellationToken cancellationToken = default)
        {
            return CallAsync<TResponse>(model, request, cancellationToken);
        }

        public static Task RegisterHandlerAsync<T>(this IRoutingModel model, IRequestMatcher pattern, Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken = default)
        {
            return model.RegisterHandlerAsync(pattern, (requestMessage, handlerToken) =>
            {
                var request = JsonMessage.Read<T>(requestMessage);
                return handler(request, handlerToken);
            }, cancellationToken: cancellationToken);
        }
        public static Task RegisterHandlerAsync<TRequest, TResponse>(this IRoutingModel model, IRequestMatcher pattern, Func<TRequest, CancellationToken, Task<TResponse>> handler, CancellationToken cancellationToken = default)
        {
            return model.RegisterHandlerAsync(pattern, async (requestMessage, handlerToken) =>
            {
                var request = JsonMessage.Read<TRequest>(requestMessage);
                var response = await handler(request, handlerToken);
                return JsonMessage.Write(response);
            }, cancellationToken: cancellationToken);
        }
    }
}
