using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Toxon.Micro.RabbitBlog.Front.Http;
using Toxon.Micro.RabbitBlog.Plugins.Core;
using Toxon.Micro.RabbitBlog.Routing;

namespace Toxon.Micro.RabbitBlog.Front
{
    [MessagePlugin("front.v2")]
    internal class Plugin
    {
        private readonly IRoutingSender _sender;

        private IWebHost _webHost;

        public Plugin(IRoutingSender sender)
        {
            _sender = sender;
        }

        [PluginStart]
        public void Start()
        {
            _webHost = new WebHostBuilder()
                .UseKestrel(k => k.ListenLocalhost(8500))
                .ConfigureServices(services => services.AddSingleton(_sender))
                .UseStartup<Startup>()
                .Start();
        }
    }
}
