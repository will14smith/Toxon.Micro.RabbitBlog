using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.Routing;

namespace Toxon.Micro.RabbitBlog.Plugins.Reflection
{
    public static class Bootstrapper
    {
        public static IRoutingModel ConfigureModel(IRoutingModel model)
        {
            // TODO add tracing & retry
            return model;
        }

        public static IReadOnlyCollection<Assembly> LoadAssembly(string assemblyPath)
        {
            if (!Path.IsPathRooted(assemblyPath))
            {
                assemblyPath = Path.Combine(Environment.CurrentDirectory, assemblyPath);
            }

            return new[]
            {
                Assembly.LoadFile(assemblyPath),
            };
        }

        public static async Task RegisterPluginAsync(IRoutingModel model, PluginMetadata pluginMetadata)
        {
            var plugin = CreatePlugin(pluginMetadata, model);

            var routes = RouteDiscoverer.Discover(pluginMetadata);
            foreach (var route in routes)
            {
                await RegisterRouteAsync(model, plugin, route);
            }
        }

        private static object CreatePlugin(PluginMetadata metadata, IRoutingModel model)
        {
            var type = metadata.Type;

            var constructor = type.GetConstructor(new[] { typeof(IRoutingModel) });
            if (constructor != null)
            {
                return constructor.Invoke(new object[] { model });
            }

            constructor = type.GetConstructor(new Type[0]);
            if (constructor != null)
            {
                return constructor.Invoke(new object[0]);
            }

            throw new InvalidOperationException("Cannot activate the plugin, missing compatible constructor");
        }

        private static async Task RegisterRouteAsync(IRoutingModel model, object plugin, RouteMetadata route)
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
