using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.Routing;
using zipkin4net;
using zipkin4net.Propagation;

namespace Toxon.Micro.RabbitBlog.Zipkin
{
    public class TracedRoutingSender : IRoutingSender
    {
        private readonly IRoutingSender _sender;
        private readonly string _serviceName;

        public TracedRoutingSender(IRoutingSender sender, string serviceName)
        {
            _sender = sender;
            _serviceName = serviceName;
        }

        public async Task SendAsync(Message message, CancellationToken cancellationToken = default)
        {
            var trace = Trace.Current.Child();

            var newMessage = InjectTracing(message, trace);

            trace.Record(Annotations.ClientSend());
            trace.Record(Annotations.ServiceName(_serviceName));
            trace.Record(Annotations.Event("TODO"));
            try
            {
                await _sender.SendAsync(newMessage, cancellationToken);
                trace.Record(Annotations.ClientRecv());
            }
            catch (Exception ex)
            {
                trace.Record(Annotations.Tag("error", ex.Message));
                trace.Record(Annotations.LocalOperationStop());

                throw;
            }
        }

        public async Task<Message> CallAsync(Message message, CancellationToken cancellationToken = default)
        {
            var trace = Trace.Current.Child();

            var newMessage = InjectTracing(message, trace);

            trace.Record(Annotations.ClientSend());
            trace.Record(Annotations.ServiceName(_serviceName));
            trace.Record(Annotations.Rpc("TODO"));
            try
            {
                var reply = await _sender.CallAsync(newMessage, cancellationToken);
                trace.Record(Annotations.ClientRecv());
                return reply;
            }
            catch (Exception ex)
            {
                trace.Record(Annotations.Tag("error", ex.Message));
                trace.Record(Annotations.LocalOperationStop());

                throw;
            }
        }

        private static Message InjectTracing(Message message, Trace trace)
        {
            var headers = message.Headers.ToDictionary(entry => entry.Key, entry => entry.Value);

            var injector = Propagations.B3String.Injector<Dictionary<string, byte[]>>((carrier, key, value) => carrier.Add(key, Encoding.UTF8.GetBytes(value)));
            injector.Inject(trace.CurrentSpan, headers);

            return new Message(message.Body, headers);
        }
    }
}
