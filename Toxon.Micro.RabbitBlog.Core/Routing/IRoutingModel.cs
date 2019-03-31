﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Toxon.Micro.RabbitBlog.Core.Patterns;

namespace Toxon.Micro.RabbitBlog.Core.Routing
{
    public interface IRoutingModel
    {
        Task SendAsync(Message message, CancellationToken cancellationToken = default);
        Task<Message> CallAsync(Message message, CancellationToken cancellationToken = default);
        Task RegisterHandlerAsync(IRequestMatcher pattern, Func<Message, CancellationToken, Task> handler, RouteExecution execution = RouteExecution.Asynchronous, RouteMode mode = RouteMode.Observe, CancellationToken cancellationToken = default);
        Task RegisterHandlerAsync(IRequestMatcher pattern, Func<Message, CancellationToken, Task<Message>> handler, RouteExecution execution = RouteExecution.Synchronous, RouteMode mode = RouteMode.Capture, CancellationToken cancellationToken = default);
    }
}