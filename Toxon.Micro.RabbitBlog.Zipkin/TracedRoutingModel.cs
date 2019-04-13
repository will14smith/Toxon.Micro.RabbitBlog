using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.Routing;
using Toxon.Micro.RabbitBlog.Routing.Patterns;
using zipkin4net;
using zipkin4net.Propagation;
using zipkin4net.Tracers.Zipkin;
using zipkin4net.Transport.Http;

namespace Toxon.Micro.RabbitBlog.Zipkin
{
    public class TracedRoutingModel : IRoutingModel
    {
        private readonly IRoutingModel _model;
        private readonly string _serviceName;

        public TracedRoutingModel(IRoutingModel model, string serviceName)
        {
            _model = model;
            _serviceName = serviceName;
        }

        public void StartTracing(string collectionUrl)
        {
            var tracer = new ZipkinTracer(new HttpZipkinSender(collectionUrl, "application/json"), new JSONSpanSerializer());

            TraceManager.SamplingRate = 1;
            TraceManager.RegisterTracer(tracer);
            TraceManager.Start(new ConsoleLogger());
        }

        private class ConsoleLogger : ILogger
        {
            public void LogInformation(string message)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(message);
                Console.ResetColor();
            }

            public void LogWarning(string message)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(message);
                Console.ResetColor();
            }

            public void LogError(string message)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(message);
                Console.ResetColor();
            }
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
                await _model.SendAsync(newMessage, cancellationToken);
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
                var reply = await _model.CallAsync(newMessage, cancellationToken);
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

        public Task RegisterHandlerAsync(IRequestMatcher pattern, Func<Message, CancellationToken, Task> handler, RouteExecution execution = RouteExecution.Asynchronous, RouteMode mode = RouteMode.Observe, CancellationToken cancellationToken = default)
        {
            return _model.RegisterHandlerAsync(
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

        public Task RegisterHandlerAsync(IRequestMatcher pattern, Func<Message, CancellationToken, Task<Message>> handler, RouteExecution execution = RouteExecution.Synchronous, RouteMode mode = RouteMode.Capture, CancellationToken cancellationToken = default)
        {
            return _model.RegisterHandlerAsync(
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

        private static Message InjectTracing(Message message, Trace trace)
        {
            var headers = message.Headers.ToDictionary(entry => entry.Key, entry => entry.Value);

            var injector = Propagations.B3String.Injector<Dictionary<string, byte[]>>((carrier, key, value) => carrier.Add(key, Encoding.UTF8.GetBytes(value)));
            injector.Inject(trace.CurrentSpan, headers);

            return new Message(message.Body, headers);
        }

        private static Trace ExtractTracing(Message message)
        {
            var extractor = Propagations.B3String.Extractor<IReadOnlyDictionary<string, byte[]>>((carrier, key) => carrier.TryGetValue(key, out var value) ? Encoding.UTF8.GetString(value) : null);

            var traceContext = extractor.Extract(message.Headers);
            return traceContext == null ? Trace.Create() : Trace.CreateFromId(traceContext);
        }
    }
}
