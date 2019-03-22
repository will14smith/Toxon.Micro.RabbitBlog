using System;
using System.Threading.Tasks;
using Toxon.Micro.RabbitBlog.Core.Patterns;

namespace Toxon.Micro.RabbitBlog.Core.Routing
{
    public interface IRoutingModel
    {
        Task SendAsync(Message message);
        Task<Message> CallAsync(Message message);
        Task HandleAsync(string serviceKey, IRequestMatcher pattern, Func<Message, Task> handle, RouteExecution execution = RouteExecution.Asynchronous, RouteMode mode = RouteMode.Observe);
        Task HandleAsync(string serviceKey, IRequestMatcher pattern, Func<Message, Task<Message>> handle, RouteExecution execution = RouteExecution.Synchronous, RouteMode mode = RouteMode.Capture);
    }
}