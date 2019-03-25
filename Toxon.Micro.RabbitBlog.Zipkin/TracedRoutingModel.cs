using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.Core.Patterns;
using Toxon.Micro.RabbitBlog.Core.Routing;
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

        public async Task SendAsync(Message message)
        {
            var trace = Trace.Current.Child();

            var newMessage = InjectTracing(message, trace);

            trace.Record(Annotations.ClientSend());
            trace.Record(Annotations.ServiceName(_serviceName));
            trace.Record(Annotations.Event("TODO"));
            try
            {
                await _model.SendAsync(newMessage);
                trace.Record(Annotations.ClientRecv());
            }
            catch (Exception ex)
            {
                trace.Record(Annotations.Tag("error", ex.Message));
                trace.Record(Annotations.LocalOperationStop());

                throw;
            }
        }

        public async Task<Message> CallAsync(Message message)
        {
            var trace = Trace.Current.Child();

            var newMessage = InjectTracing(message, trace);

            trace.Record(Annotations.ClientSend());
            trace.Record(Annotations.ServiceName(_serviceName));
            trace.Record(Annotations.Rpc("TODO"));
            try
            {
                var reply = await _model.CallAsync(newMessage);
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

        public Task RegisterHandlerAsync(IRequestMatcher pattern, Func<Message, Task> handler, RouteExecution execution = RouteExecution.Asynchronous, RouteMode mode = RouteMode.Observe)
        {
            return _model.RegisterHandlerAsync(
                pattern,
                async message =>
                {
                    Trace.Current = ExtractTracing(message);

                    var trace = Trace.Current;
                    trace.Record(Annotations.ServerRecv());
                    trace.Record(Annotations.ServiceName(_serviceName));
                    trace.Record(Annotations.Event("TODO"));
                    try
                    {
                        await handler(message);
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
                mode
            );
        }

        public Task RegisterHandlerAsync(IRequestMatcher pattern, Func<Message, Task<Message>> handler, RouteExecution execution = RouteExecution.Synchronous, RouteMode mode = RouteMode.Capture)
        {
            return _model.RegisterHandlerAsync(
                pattern,
                async message =>
                {
                    Trace.Current = ExtractTracing(message);

                    var trace = Trace.Current;
                    trace.Record(Annotations.ServerRecv());
                    trace.Record(Annotations.ServiceName(_serviceName));
                    trace.Record(Annotations.Rpc("TODO"));
                    try
                    {
                        var response = await handler(message);
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
                mode
            );
        }

        private static Message InjectTracing(Message message, Trace trace)
        {
            var headers = message.Headers.ToDictionary(entry => entry.Key, entry => entry.Value);

            var injector = Propagations.B3String.Injector<Dictionary<string, object>>((carrier, key, value) => carrier.Add(key, value));
            injector.Inject(trace.CurrentSpan, headers);

            return new Message(message.Body, headers);
        }

        private static Trace ExtractTracing(Message message)
        {
            var extractor = Propagations.B3String.Extractor<IReadOnlyDictionary<string, object>>((carrier, key) =>
            {
                if (!carrier.TryGetValue(key, out var value))
                {
                    return null;
                }

                if (value is byte[] bytes)
                {
                    return Encoding.UTF8.GetString(bytes);
                }

                return (string) value;
            });

            var traceContext = extractor.Extract(message.Headers);
            return traceContext == null ? Trace.Create() : Trace.CreateFromId(traceContext);
        }
    }
}
