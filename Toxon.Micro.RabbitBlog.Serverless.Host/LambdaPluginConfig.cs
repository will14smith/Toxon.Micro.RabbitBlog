using System;
using System.Collections.Generic;
using System.Linq;
using Toxon.Micro.RabbitBlog.Plugins.Core;
using Toxon.Micro.RabbitBlog.Plugins.Reflection;
using Toxon.Micro.RabbitBlog.Routing;
using Toxon.Micro.RabbitBlog.Serverless.Core;

namespace Toxon.Micro.RabbitBlog.Serverless.Host
{
    internal static class LambdaPluginConfig
    {
        public static LocalModel CreateHandler(IRoutingSender sender)
        {
            var plugins = DiscoverPlugins();
            var handler = new LocalModel();

            foreach (var plugin in plugins.Where(x => x.ServiceType == ServiceType.MessageHandler))
            {
                Bootstrapper.RegisterPluginAsync(sender, handler, plugin).Wait();
            }

            return handler;
        }

        public static IReadOnlyCollection<PluginMetadata> DiscoverPlugins()
        {
            var pluginPaths = GetPluginPaths();
            var pluginLoaders = Bootstrapper.LoadPlugins(pluginPaths);

            return PluginDiscoverer.Discover(pluginLoaders.Assemblies);
        }

        public static string GetHttpServiceKey()
        {
            return Environment.GetEnvironmentVariable("HTTP_SERVICE_KEY");
        }
        public static IReadOnlyCollection<string> GetPluginPaths()
        {
            return Environment.GetEnvironmentVariable("PLUGIN_PATHS")
                .Split(",")
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();
        }

    }
}
