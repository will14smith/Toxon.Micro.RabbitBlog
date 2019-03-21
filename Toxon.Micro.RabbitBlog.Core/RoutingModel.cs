using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using Toxon.Micro.RabbitBlog.Core.Patterns;

namespace Toxon.Micro.RabbitBlog.Core
{
    public class RoutingModel : IRoutingModel
    {
        private readonly BusModel _bus;
        private readonly RpcModel _rpc;

        public RoutingModel(IModel model) : this(new BusModel(model), new RpcModel(model)) { }
        public RoutingModel(BusModel bus, RpcModel rpc)
        {
            _bus = bus;
            _rpc = rpc;
        }

        public Task SendAsync(Message message)
        {
            throw new NotImplementedException();
        }
        public Task<Message> CallAsync(Message message)
        {
            throw new NotImplementedException();
        }

        public Task HandleAsync(string serviceKey, IRequestMatcher pattern, Func<Message, Task> handle)
        {
            throw new NotImplementedException();
        }
        public Task HandleAsync(string serviceKey, IRequestMatcher pattern, Func<Message, Task<Message>> handle)
        {
            throw new NotImplementedException();
        }
    }
}
