using System.Linq;
using Amazon.Lambda.AspNetCoreServer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Toxon.Micro.RabbitBlog.Plugins.Core;
using Toxon.Micro.RabbitBlog.Plugins.Reflection;
using Toxon.Micro.RabbitBlog.Routing;

namespace Toxon.Micro.RabbitBlog.Serverless.Host
{
    public class HttpFunction : APIGatewayProxyFunction
    {
        private readonly IRoutingSender _sender;
        private readonly PluginMetadata _plugin;

        public HttpFunction()
        {
            var plugins = LambdaConfig.DiscoverPlugins();
            var httpServiceKey = LambdaConfig.GetHttpServiceKey();

            _sender = LambdaConfig.CreateSender();
            _plugin = plugins.Single(x => x.ServiceKey == httpServiceKey && x.ServiceType == ServiceType.Http);
        }

        protected override void Init(IWebHostBuilder builder)
        {
            builder
                .ConfigureServices(services => services.AddSingleton(_sender))
                .UseStartup(_plugin.Type);
        }
    }
}
