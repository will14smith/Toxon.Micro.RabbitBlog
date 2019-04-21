using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.Plugins.Core;
using Toxon.Micro.RabbitBlog.Routing;

namespace Toxon.Micro.RabbitBlog.Plugins.Reflection
{
    public static class Bootstrapper
    {
        public static PluginsAssemblyLoader LoadPlugins(IEnumerable<string> pluginPaths)
        {
            var rootedPluginPaths = pluginPaths
                .Select(x => !Path.IsPathRooted(x) ? Path.Combine(Environment.CurrentDirectory, x) : x);

            return new PluginsAssemblyLoader(rootedPluginPaths);
        }

        public static async Task RegisterPluginAsync(IRoutingSender sender, IRoutingRegistration registration, PluginMetadata pluginMetadata)
        {
            if (pluginMetadata.ServiceType != ServiceType.MessageHandler)
            {
                return;
            }

            var plugin = CreatePlugin(pluginMetadata, sender);

            
            var routes = RouteDiscoverer.Discover(pluginMetadata);
            foreach (var route in routes)
            {
                await RegisterRouteAsync(registration, plugin, route);
            }
        }

        private static object CreatePlugin(PluginMetadata metadata, IRoutingSender sender)
        {
            var type = metadata.Type;

            var constructor = type.GetConstructor(new[] { typeof(IRoutingSender) });
            if (constructor != null)
            {
                return constructor.Invoke(new object[] { sender });
            }

            constructor = type.GetConstructor(new Type[0]);
            if (constructor != null)
            {
                return constructor.Invoke(new object[0]);
            }

            throw new InvalidOperationException("Cannot activate the plugin, missing compatible constructor");
        }

        private static async Task RegisterRouteAsync(IRoutingRegistration model, object plugin, RouteMetadata route)
        {
            var pattern = route.Route;

            if (RouteHandlerFactory.IsRpc(route))
            {
                var handler = RouteHandlerFactory.BuildRpcHandler(plugin, route);
                await model.RegisterHandlerAsync(pattern, new Func<Message, CancellationToken, Task<Message>>(handler));
            }
            else
            {
                var handler = RouteHandlerFactory.BuildBusHandler(plugin, route);
                await model.RegisterHandlerAsync(pattern, new Func<Message, CancellationToken, Task>(handler));
            }
        }
    }
}
