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
                .ConfigureServices(services => services.AddSingleton(LambdaConfig.CreateSender()))
                .Configure(app =>
                {
                    app.UseExceptionHandler("/Error");
                    app.UseXRay(serviceKey);
                })
                .UseStartup(type);
        }
        
        protected abstract string GetServiceKey();
        protected abstract Type GetServiceType();
    }
}
