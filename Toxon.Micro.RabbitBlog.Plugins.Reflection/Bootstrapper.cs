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
        public static PluginLoader LoadPlugins(IEnumerable<string> pluginPaths)
        {
            var rootedPluginPaths = pluginPaths
                .Select(x => !Path.IsPathRooted(x) ? Path.Combine(Environment.CurrentDirectory, x) : x);

            return new PluginLoader(rootedPluginPaths);
        }

        public static async Task RegisterPluginAsync(IRoutingSender sender, IRoutingRegistration registration, PluginMetadata pluginMetadata)
        {
            var plugin = CreatePlugin(pluginMetadata, sender);

            var routes = RouteDiscoverer.Discover(pluginMetadata);
            foreach (var route in routes)
            {
                await RegisterRouteAsync(registration, plugin, route);
            }

            StartPlugin(pluginMetadata, plugin);
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

        private static void StartPlugin(PluginMetadata metadata, object plugin)
        {
            var methods = metadata.Type.GetMethods();
            foreach (var method in methods)
            {
                var startAttribute = method.GetCustomAttribute<PluginStartAttribute>();
                if (startAttribute == null)
                {
                    continue;
                }

                if (method.GetParameters().Length != 0)
                {
                    throw new InvalidOperationException("Cannot call start plugin method with parameters");
                }

                method.Invoke(plugin, null);
            }
        }
    }
}
