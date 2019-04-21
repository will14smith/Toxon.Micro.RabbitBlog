using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Toxon.Micro.RabbitBlog.Plugins.Reflection;
using YamlDotNet.RepresentationModel;

namespace Toxon.Micro.RabbitBlog.Serverless.Tool
{
    internal class ServerlessBuilder
    {
        private readonly ServiceDiscoveryOptions _serviceDiscoveryOptions;
        private readonly NamingConventions _namingConventions;

        public ServerlessBuilder(ServiceDiscoveryOptions serviceDiscoveryOptions, NamingConventions namingConventions)
        {
            _serviceDiscoveryOptions = serviceDiscoveryOptions;
            _namingConventions = namingConventions;
        }

        public string Build()
        {
            var functions = new YamlMappingNode
            {
                { "router", BuildRouterFunction() }
            };

            AddServices(functions);

            var root = new YamlMappingNode
            {
                { "service", _namingConventions.GetServiceName() },
                { "provider", new YamlMappingNode
                    {
                        { "name", "aws" },
                        { "stage", "${opt:stage, 'dev'}" },
                        { "region", "TODO" },
                        { "runtime", "dotnetcore2.1" }
                    }
                },
                {
                    "package", new YamlMappingNode
                    {
                        { "individually", "true" },
                    }

                },
                {
                    "layers", new YamlMappingNode
                    {
                        { "host", BuildHostLayer() }
                    }
                },
                { "functions", functions }
            };

            var doc = new YamlDocument(root);
            var stream = new YamlStream(doc);
            var output = new StringWriter();
            stream.Save(output, false);

            return output.ToString();
        }

        private void AddServices(YamlMappingNode functions)
        {
            var services = ServiceDiscoverer.Discover(_serviceDiscoveryOptions);
            var pluginLoaders = Bootstrapper.LoadPlugins(services.Select(x => x.AssemblyPath));

            foreach (var service in services)
            {
                var assembly = pluginLoaders.Assemblies.Single(x => x.GetName().Name == service.Name);
                var plugins = PluginDiscoverer.Discover(assembly);

                foreach (var plugin in plugins)
                {
                    AddService(functions, service, plugin);
                }
            }
        }

        private void AddService(YamlMappingNode functions, ServiceProject service, PluginMetadata plugin)
        {
            // TODO handle triggers, environment variables
            var functionName = _namingConventions.GetLambdaName(plugin);

            functions.Add(functionName, new YamlMappingNode
            {
                { "name", functionName },
                { "handler", "Toxon.Micro.RabbitBlog.Serverless.Host::Toxon.Micro.RabbitBlog.Serverless.Host.HostingFunction::Handle" },
                {
                    "layers", new YamlSequenceNode
                    {
                        new YamlMappingNode { { "Ref", "HostLambdaLayer" } },
                    }
                },
                { "memorySize", "128" },
                { "timeout", "6" },
                {
                    "package", new YamlMappingNode
                    {
                        { "artifact", $"artifacts/{service.Name}.zip" },
                    }
                },
            });
        }

        private YamlNode BuildRouterFunction()
        {
            // TODO handle triggers, routing info (or in packager?)

            return new YamlMappingNode
            {
                { "name", $"{_namingConventions.GetServiceName()}-router" },
                { "handler", "Toxon.Micro.RabbitBlog.Serverless.Router::Toxon.Micro.RabbitBlog.Serverless.Router.RouterFunction::Handle" },
                { "memorySize", "128" },
                { "timeout", "6" },
                {
                    "package", new YamlMappingNode
                    {
                        { "artifact", "artifacts/router.zip" },
                    }
                },
            };
        }

        private YamlNode BuildHostLayer()
        {
            return new YamlMappingNode
            {
                { "name", $"{_namingConventions.GetServiceName()}-host" },
                { "compatibleRuntimes", new YamlSequenceNode { "dotnetcore2.1" } },
                {
                    "package", new YamlMappingNode
                    {
                        { "artifact", "artifacts/host.zip" },
                    }
                },
            };
        }
    }
}