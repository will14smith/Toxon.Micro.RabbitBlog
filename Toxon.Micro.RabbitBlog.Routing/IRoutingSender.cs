using System.Threading;
using System.Threading.Tasks;
using Toxon.Micro.RabbitBlog.Core;

namespace Toxon.Micro.RabbitBlog.Routing
{
    public interface IRoutingSender
    {
        Task SendAsync(Message message, CancellationToken cancellationToken = default);
        Task<Message> CallAsync(Message message, CancellationToken cancellationToken = default);
    }
}