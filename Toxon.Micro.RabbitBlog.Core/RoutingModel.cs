using System;
using Toxon.Micro.RabbitBlog.Core.Patterns;

namespace Toxon.Micro.RabbitBlog.Core
{
    public class RoutingModel
    {
        private readonly BusModel _bus;
        private readonly RpcModel _rpc;

        public RoutingModel(BusModel bus, RpcModel rpc)
        {
            _bus = bus;
            _rpc = rpc;
        }

        public void Send(Message message)
        {
            throw new NotImplementedException();
        }
        public Message Call(Message message)
        {
            throw new NotImplementedException();
        }

        public void Handle(string serviceKey, IRequestMatcher pattern, Action<Message> handle)
        {
            throw new NotImplementedException();
        }
        public void Handle(string serviceKey, IRequestMatcher pattern, Func<Message, Message> handle)
        {
            throw new NotImplementedException();
        }
    }
}
