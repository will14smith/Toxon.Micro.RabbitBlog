using System;
using System.Collections.Generic;
using System.Linq;
using Toxon.Micro.RabbitBlog.Plugins.Core;
using Toxon.Micro.RabbitBlog.Plugins.Reflection;
using Toxon.Micro.RabbitBlog.Routing;

namespace Toxon.Micro.RabbitBlog.Serverless.Host
{
    internal static class LambdaConfig
    {
        public static IRoutingSender CreateSender()
        {
            return new LambdaSender(GetRouterQueueName(), GetRouterFunctionName());
        }
        
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

        public static string GetRouterQueueName()
        {
            return Environment.GetEnvironmentVariable("ROUTER_QUEUE_NAME");
        }
        public static string GetRouterFunctionName()
        {
            return Environment.GetEnvironmentVariable("ROUTER_FUNCTION_NAME");
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
