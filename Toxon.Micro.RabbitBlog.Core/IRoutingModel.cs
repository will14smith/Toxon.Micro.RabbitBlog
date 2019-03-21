using System;
using System.Threading.Tasks;
using Toxon.Micro.RabbitBlog.Core.Patterns;

namespace Toxon.Micro.RabbitBlog.Core
{
    public interface IRoutingModel
    {
        Task SendAsync(Message message);
        Task<Message> CallAsync(Message message);
        Task HandleAsync(string serviceKey, IRequestMatcher pattern, Func<Message, Task> handle);
        Task HandleAsync(string serviceKey, IRequestMatcher pattern, Func<Message, Task<Message>> handle);
    }
}