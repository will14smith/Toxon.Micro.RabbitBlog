using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.Routing;
using Toxon.Micro.RabbitBlog.Routing.Patterns;
using zipkin4net;
using zipkin4net.Propagation;

namespace Toxon.Micro.RabbitBlog.Zipkin
{
    public class TracedRoutingRegistration : IRoutingRegistration
    {
        private readonly IRoutingRegistration _registration;
        private readonly string _serviceName;

        public TracedRoutingRegistration(IRoutingRegistration registration, string serviceName)
        {
            _registration = registration;
            _serviceName = serviceName;
        }
        
        public Task RegisterBusHandlerAsync(IRequestMatcher pattern, Func<Message, CancellationToken, Task> handler, RouteExecution execution = RouteExecution.Asynchronous, RouteMode mode = RouteMode.Observe, CancellationToken cancellationToken = default)
        {
            return _registration.RegisterBusHandlerAsync(
                pattern,
                async (message, handlerToken) =>
                {
                    Trace.Current = ExtractTracing(message);

                    var trace = Trace.Current;
                    trace.Record(Annotations.ServerRecv());
                    trace.Record(Annotations.ServiceName(_serviceName));
                    trace.Record(Annotations.Event("TODO"));
                    try
                    {
                        await handler(message, handlerToken);
                        trace.Record(Annotations.ServerSend());
                    }
                    catch (Exception ex)
                    {
                        trace.Record(Annotations.Tag("error", ex.Message));
                        trace.Record(Annotations.LocalOperationStop());

                        throw;
                    }
                },
                execution,
                mode,
                cancellationToken);
        }

        public Task RegisterRpcHandlerAsync(IRequestMatcher pattern, Func<Message, CancellationToken, Task<Message>> handler, RouteExecution execution = RouteExecution.Synchronous, RouteMode mode = RouteMode.Capture, CancellationToken cancellationToken = default)
        {
            return _registration.RegisterRpcHandlerAsync(
                pattern,
                async (message, handlerToken) =>
                {
                    Trace.Current = ExtractTracing(message);

                    var trace = Trace.Current;
                    trace.Record(Annotations.ServerRecv());
                    trace.Record(Annotations.ServiceName(_serviceName));
                    trace.Record(Annotations.Rpc("TODO"));
                    try
                    {
                        var response = await handler(message, handlerToken);
                        trace.Record(Annotations.ServerSend());
                        return response;
                    }
                    catch (Exception ex)
                    {
                        trace.Record(Annotations.Tag("error", ex.Message));
                        trace.Record(Annotations.LocalOperationStop());

                        throw;
                    }
                },
                execution,
                mode,
                cancellationToken);
        }

        private static Trace ExtractTracing(Message message)
        {
            var extractor = Propagations.B3String.Extractor<IReadOnlyDictionary<string, byte[]>>((carrier, key) => carrier.TryGetValue(key, out var value) ? Encoding.UTF8.GetString(value) : null);

            var traceContext = extractor.Extract(message.Headers);
            return traceContext == null ? Trace.Create() : Trace.CreateFromId(traceContext);
        }
    }
}