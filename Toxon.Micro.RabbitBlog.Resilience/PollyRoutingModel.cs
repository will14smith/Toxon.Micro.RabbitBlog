using System;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.Core.Patterns;
using Toxon.Micro.RabbitBlog.Core.Routing;

namespace Toxon.Micro.RabbitBlog.Resilience
{
    public class PollyRoutingModel : IRoutingModel
    {
        private readonly IRoutingModel _model;
        private readonly AsyncPolicy _policy;

        public PollyRoutingModel(IRoutingModel model, AsyncPolicy policy)
        {
            _model = model;
            _policy = policy;
        }

        public Task SendAsync(Message message, CancellationToken cancellationToken = default)
        {
            return _policy.ExecuteAsync((_, policyToken) => _model.SendAsync(message, policyToken), new Context(), cancellationToken);
        }

        public Task<Message> CallAsync(Message message, CancellationToken cancellationToken = default)
        {
            return _policy
                .AsAsyncPolicy<Message>()
                .ExecuteAsync((_, policyToken) => _model.CallAsync(message, policyToken), new Context(), cancellationToken);
        }

        public Task RegisterHandlerAsync(IRequestMatcher pattern, Func<Message, CancellationToken, Task> handler, RouteExecution execution = RouteExecution.Asynchronous, RouteMode mode = RouteMode.Observe, CancellationToken cancellationToken = default)
        {
            return _policy.ExecuteAsync((_, registerPolicyToken) => _model.RegisterHandlerAsync(
                pattern,
                (message, handlerToken) => _policy.ExecuteAsync((__, handlerPolicyToken) => handler(message, handlerPolicyToken), new Context(), handlerToken),
                execution, 
                mode,
                registerPolicyToken
            ), new Context(), cancellationToken);
        }

        public Task RegisterHandlerAsync(IRequestMatcher pattern, Func<Message, CancellationToken, Task<Message>> handler, RouteExecution execution = RouteExecution.Synchronous, RouteMode mode = RouteMode.Capture, CancellationToken cancellationToken = default)
        {
            return _policy.ExecuteAsync((_, registerPolicyToken) => _model.RegisterHandlerAsync(
                pattern,
                (message, handlerToken) => _policy.AsAsyncPolicy<Message>().ExecuteAsync((__, handlerPolicyToken) => handler(message, handlerPolicyToken), new Context(), handlerToken),
                execution,
                mode,
                registerPolicyToken
            ), new Context(), cancellationToken);
        }
    }
}
