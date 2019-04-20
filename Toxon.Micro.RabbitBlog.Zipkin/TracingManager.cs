using System;
using zipkin4net;
using zipkin4net.Tracers.Zipkin;
using zipkin4net.Transport.Http;

namespace Toxon.Micro.RabbitBlog.Zipkin
{
    public static class TracingManager
    {
        public static void StartTracing(string collectionUrl)
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

    }
}