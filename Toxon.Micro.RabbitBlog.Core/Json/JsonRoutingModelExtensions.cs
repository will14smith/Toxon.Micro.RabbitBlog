using System;
using System.Threading.Tasks;
using Toxon.Micro.RabbitBlog.Core.Patterns;

namespace Toxon.Micro.RabbitBlog.Core.Json
{
    public static class JsonRoutingModelExtensions
    {
        public static Task SendAsync(this IRoutingModel model, object message)
        {
            throw new NotImplementedException();
        }
        public static Task<TResponse> CallAsync<TResponse>(this IRoutingModel model, object message)
        {
            throw new NotImplementedException();
        }

        public static Task SendAsync<T>(this IRoutingModel model, T message)
        {
            throw new NotImplementedException();
        }
        public static Task<TResponse> CallAsync<TRequest, TResponse>(this IRoutingModel model, TRequest message)
        {
            throw new NotImplementedException();
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
            return model.HandleAsync(serviceKey, pattern, requestMessage =>
            {
                var request = JsonMessage.Read<TRequest>(requestMessage);
                var response = handle(request);
                var responseMessage = JsonMessage.Write(response);
                return Task.FromResult(responseMessage);
            });
        }
    }
}
