using System;
using System.IO;
using System.Linq;
using Toxon.Micro.RabbitBlog.Plugins.Core;
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

            var resources = new YamlMappingNode
            {
                { "RouterQueue", BuildQueue("router") }
            };

            AddServices(functions, resources);

            var root = new YamlMappingNode
            {
                { "service", _namingConventions.GetServiceName() },
                { "provider", new YamlMappingNode
                    {
                        { "name", "aws" },
                        { "stage", "${opt:stage, 'dev'}" },
                        { "region", "${opt:region, 'eu-west-1'}" },
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
                { "functions", functions },
                { "resources", new YamlMappingNode
                    {
                        { "Resources", resources }
                    }
                }
            };

            var doc = new YamlDocument(root);
            var stream = new YamlStream(doc);
            var output = new StringWriter();
            stream.Save(output, false);

            return output.ToString();
        }

        private void AddServices(YamlMappingNode functions, YamlMappingNode resources)
        {
            var services = ServiceDiscoverer.Discover(_serviceDiscoveryOptions);
            var pluginLoaders = Bootstrapper.LoadPlugins(services.Select(x => x.AssemblyPath));

            foreach (var service in services)
            {
                var assembly = pluginLoaders.Assemblies.Single(x => x.GetName().Name == service.Name);
                var plugins = PluginDiscoverer.Discover(assembly);

                foreach (var plugin in plugins)
                {
                    AddService(functions, resources, service, plugin);
                }
            }
        }

        private void AddService(YamlMappingNode functions, YamlMappingNode resources, ServiceProject service, PluginMetadata plugin)
        {
            var functionName = _namingConventions.GetLambdaName(plugin);

            var env = new YamlMappingNode
            {
                { "ROUTER_QUEUE_NAME", new YamlMappingNode { { "Fn::GetAtt", new YamlSequenceNode("RouterQueue", "QueueName") } } },
                { "ROUTER_FUNCTION_NAME", new YamlMappingNode { { "Ref", "RouterLambdaFunction" } } },
                { "PLUGIN_PATHS", Path.GetFileName(service.AssemblyPath) },
            };

            var events = new YamlSequenceNode();

            switch (plugin.ServiceType)
            {
                case ServiceType.MessageHandler:
                    var routes = RouteDiscoverer.Discover(plugin);
                    if (routes.Any(x => !RouteHandlerFactory.IsRpc(x)))
                    {
                        var queueRefName = ToTitleCase(plugin.ServiceKey.Replace('.', '-'));

                        resources.Add(queueRefName, BuildQueue(queueRefName));

                        events.Add(BuildQueue(queueRefName));
                    }
                    break;
                case ServiceType.Http:
                    env.Add("HTTP_SERVICE_KEY", plugin.ServiceKey);
                    events.Add(BuildHttpEvent());
                    break;

                default: throw new ArgumentOutOfRangeException();
            }

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
                { "environment", env },
                { "memorySize", "128" },
                { "timeout", "6" },
                { "events", events },
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
                { "events", new YamlSequenceNode
                    {
                        BuildSqsEvent("RouterQueue")
                    }
                },
                {
                    "package", new YamlMappingNode
                    {
                        { "artifact", "artifacts/router.zip" },
                    }
                },
            };
        }

        private YamlNode BuildQueue(string name)
        {
            return new YamlMappingNode
            {
                { "Type", "AWS::SQS::Queue" },
                { "Properties", new YamlMappingNode
                    {
                        { "QueueName", $"{_namingConventions.GetServiceName()}-{name}" }
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

        private YamlNode BuildSqsEvent(string queueRefName)
        {
            return new YamlMappingNode
            {
                { "sqs", new YamlMappingNode
                    {
                        { "arn", new YamlMappingNode { { "Fn::GetAtt", new YamlSequenceNode(queueRefName, "Arn") } } },
                    }
                },
            };
        }
        private YamlNode BuildHttpEvent()
        {
            return new YamlMappingNode
            {
                { "http", new YamlMappingNode
                    {
                        { "path", "/{proxy+}" },
                        { "method", "ANY" },
                    }
                },
            };
        }

        private static string ToTitleCase(string str)
        {
            return string.Join("", str
                .Split('-')
                .Select(x => char.ToUpper(x[0]) + x.Substring(1).ToLower())
            );
        }

    }
}