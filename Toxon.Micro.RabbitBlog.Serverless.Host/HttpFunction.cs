using System.Linq;
using Amazon.Lambda.AspNetCoreServer;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Toxon.Micro.RabbitBlog.Plugins.Core;

namespace Toxon.Micro.RabbitBlog.Serverless.Host
{
    public class HttpFunction : APIGatewayProxyFunction
    {
        protected override void Init(IWebHostBuilder builder)
        {
            AWSSDKHandler.RegisterXRayForAllServices();

            var plugins = LambdaConfig.DiscoverPlugins();
            var httpServiceKey = LambdaConfig.GetHttpServiceKey();

            var sender = LambdaConfig.CreateSender();
            var plugin = plugins.Single(x => x.ServiceKey == httpServiceKey && x.ServiceType == ServiceType.Http);
            
            builder
                .ConfigureServices(services => services.AddSingleton(sender))
                .Configure(app =>
                {
                    app.UseExceptionHandler("/Error");
                    app.UseXRay(plugin.ServiceKey);
                })
                .UseStartup(plugin.Type);
        }
    }
}
