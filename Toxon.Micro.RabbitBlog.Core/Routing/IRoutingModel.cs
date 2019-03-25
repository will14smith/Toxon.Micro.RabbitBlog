using System;
using System.Threading.Tasks;
using Toxon.Micro.RabbitBlog.Core.Patterns;

namespace Toxon.Micro.RabbitBlog.Core.Routing
{
    public interface IRoutingModel
    {
        Task SendAsync(Message message);
        Task<Message> CallAsync(Message message);
        Task RegisterHandlerAsync(IRequestMatcher pattern, Func<Message, Task> handler, RouteExecution execution = RouteExecution.Asynchronous, RouteMode mode = RouteMode.Observe);
        Task RegisterHandlerAsync(IRequestMatcher pattern, Func<Message, Task<Message>> handler, RouteExecution execution = RouteExecution.Synchronous, RouteMode mode = RouteMode.Capture);
    }
}