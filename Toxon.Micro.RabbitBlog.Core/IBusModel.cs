using System;
using System.Threading;
using System.Threading.Tasks;

namespace Toxon.Micro.RabbitBlog.Core
{
    public interface IBusModel
    {
        Task SendAsync(string route, Message message, CancellationToken cancellationToken = default);
        Task RegisterHandlerAsync(string route, Func<Message, CancellationToken, Task> handler, CancellationToken cancellationToken = default);
    }
}