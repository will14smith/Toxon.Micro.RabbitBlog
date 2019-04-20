using System;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.Routing;
using Toxon.Micro.RabbitBlog.Routing.Patterns;

namespace Toxon.Micro.RabbitBlog.Resilience
{
    public class PollyRoutingRegistration : IRoutingRegistration
    {
        private readonly IRoutingRegistration _registration;
        private readonly AsyncPolicy _policy;

        public PollyRoutingRegistration(IRoutingRegistration registration, AsyncPolicy policy)
        {
            _registration = registration;
            _policy = policy;
        }

        public Task RegisterHandlerAsync(IRequestMatcher pattern, Func<Message, CancellationToken, Task> handler, RouteExecution execution = RouteExecution.Asynchronous, RouteMode mode = RouteMode.Observe, CancellationToken cancellationToken = default)
        {
            return _policy.ExecuteAsync((_, registerPolicyToken) => _registration.RegisterHandlerAsync(
                pattern,
                (message, handlerToken) => _policy.ExecuteAsync((__, handlerPolicyToken) => handler(message, handlerPolicyToken), new Context(), handlerToken),
                execution,
                mode,
                registerPolicyToken
            ), new Context(), cancellationToken);
        }

        public Task RegisterHandlerAsync(IRequestMatcher pattern, Func<Message, CancellationToken, Task<Message>> handler, RouteExecution execution = RouteExecution.Synchronous, RouteMode mode = RouteMode.Capture, CancellationToken cancellationToken = default)
        {
            return _policy.ExecuteAsync((_, registerPolicyToken) => _registration.RegisterHandlerAsync(
                pattern,
                (message, handlerToken) => _policy.AsAsyncPolicy<Message>().ExecuteAsync((__, handlerPolicyToken) => handler(message, handlerPolicyToken), new Context(), handlerToken),
                execution,
                mode,
                registerPolicyToken
            ), new Context(), cancellationToken);
        }
    }

}
