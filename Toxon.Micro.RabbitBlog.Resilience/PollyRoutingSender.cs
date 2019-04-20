using System.Threading;
using System.Threading.Tasks;
using Polly;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.Routing;

namespace Toxon.Micro.RabbitBlog.Resilience
{
    public class PollyRoutingSender : IRoutingSender
    {
        private readonly IRoutingSender _sender;
        private readonly AsyncPolicy _policy;

        public PollyRoutingSender(IRoutingSender sender, AsyncPolicy policy)
        {
            _sender = sender;
            _policy = policy;
        }

        public Task SendAsync(Message message, CancellationToken cancellationToken = default)
        {
            return _policy.ExecuteAsync((_, policyToken) => _sender.SendAsync(message, policyToken), new Context(), cancellationToken);
        }

        public Task<Message> CallAsync(Message message, CancellationToken cancellationToken = default)
        {
            return _policy
                .AsAsyncPolicy<Message>()
                .ExecuteAsync((_, policyToken) => _sender.CallAsync(message, policyToken), new Context(), cancellationToken);
        }
    }
}