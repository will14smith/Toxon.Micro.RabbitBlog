using System;
using Amazon.Lambda.AspNetCoreServer;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Toxon.Micro.RabbitBlog.Routing;
using Toxon.Micro.RabbitBlog.Serverless.Core;

namespace Toxon.Micro.RabbitBlog.Serverless.HttpEntry
{
    public abstract class BaseFunction : APIGatewayProxyFunction
    {
        protected override void Init(IWebHostBuilder builder)
        {
            AWSSDKHandler.RegisterXRayForAllServices();

            var serviceKey = GetServiceKey();
            var type = GetServiceType();

            builder
                .ConfigureServices(services => services.AddSingleton(CreateSender()))
                .Configure(app =>
                {
                    app.UseExceptionHandler("/Error");
                    app.UseXRay(serviceKey);
                })
                .UseStartup(type);
        }
        
        protected abstract string GetServiceKey();
        protected abstract Type GetServiceType();

        private static IRoutingSender CreateSender()
        {
            return new LambdaSender(GetRouterQueueName(), GetRouterFunctionName());
        }

        public static string GetRouterQueueName()
        {
            return Environment.GetEnvironmentVariable("ROUTER_QUEUE_NAME");
        }
        public static string GetRouterFunctionName()
        {
            return Environment.GetEnvironmentVariable("ROUTER_FUNCTION_NAME");
        }
    }
}
