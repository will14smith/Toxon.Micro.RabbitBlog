using System.Threading;
using System.Threading.Tasks;

namespace Toxon.Micro.RabbitBlog.Routing.Json
{
    public static class JsonRoutingSenderExtensions
    {
        public static async Task SendAsync(this IRoutingSender sender, object request, CancellationToken cancellationToken = default)
        {
            var message = JsonMessage.Write(request);

            await sender.SendAsync(message, cancellationToken);
        }
        public static async Task<TResponse> CallAsync<TResponse>(this IRoutingSender sender, object request, CancellationToken cancellationToken = default)
        {
            var requestMessage = JsonMessage.Write(request);

            var responseMessage = await sender.CallAsync(requestMessage, cancellationToken);

            return JsonMessage.Read<TResponse>(responseMessage);
        }

        public static Task SendAsync<T>(this IRoutingSender sender, T request, CancellationToken cancellationToken = default)
        {
            return SendAsync(sender, (object)request, cancellationToken);
        }
        public static Task<TResponse> CallAsync<TRequest, TResponse>(this IRoutingSender sender, TRequest request, CancellationToken cancellationToken = default)
        {
            return CallAsync<TResponse>(sender, request, cancellationToken);
        }
    }
}
