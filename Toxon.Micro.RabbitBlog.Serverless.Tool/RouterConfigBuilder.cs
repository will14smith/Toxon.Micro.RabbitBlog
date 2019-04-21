using System.Collections.Generic;
using System.Linq;
using Toxon.Micro.RabbitBlog.Plugins.Reflection;
using Toxon.Micro.RabbitBlog.Serverless.Router;

namespace Toxon.Micro.RabbitBlog.Serverless.Tool
{
    internal class RouterConfigBuilder
    {
        private readonly ServiceDiscoveryOptions _serviceDiscoveryOptions;
        private readonly NamingConventions _namingConventions;

        public RouterConfigBuilder(ServiceDiscoveryOptions serviceDiscoveryOptions, NamingConventions namingConventions)
        {
            _serviceDiscoveryOptions = serviceDiscoveryOptions;
            _namingConventions = namingConventions;
        }

        public RouterConfig Build()
        {
            var services = ServiceDiscoverer.Discover(_serviceDiscoveryOptions);
            var pluginLoaders = Bootstrapper.LoadPlugins(services.Select(x => x.AssemblyPath));

            var routes = new List<RouterEntry>();

            foreach (var assembly in pluginLoaders.Assemblies)
            {
                var assemblyRoutes = PluginDiscoverer.Discover(assembly)
                    .SelectMany(plugin => RouteDiscoverer.Discover(plugin).Select(route => ToRouterEntry(plugin, route)));

                routes.AddRange(assemblyRoutes);
            }

            return new RouterConfig(routes);
        }

        private RouterEntry ToRouterEntry(PluginMetadata plugin, RouteMetadata metadata)
        {
            if (RouteHandlerFactory.IsRpc(metadata))
            {
                return new RouterEntry(RouteType.Lambda, _namingConventions.GetLambdaArn(plugin), metadata.Route);
            }

            return new RouterEntry(RouteType.Sqs, _namingConventions.GetSqsArn(plugin), metadata.Route);
        }
    }
}
