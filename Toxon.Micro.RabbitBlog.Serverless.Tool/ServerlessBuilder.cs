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
            var functions = new YamlMappingNode();
            AddRouterFunction(functions);

            var resources = new YamlMappingNode
            {
                { "RouterQueue", BuildQueue($"{_namingConventions.GetServiceName()}-router") }
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
                        { "runtime", "dotnetcore2.1" },
                        {
                            "iamRoleStatements", new YamlSequenceNode
                            {
                                new YamlMappingNode
                                {
                                    { "Effect", "Allow" },
                                    { "Action", new YamlSequenceNode("lambda:InvokeFunction", "lambda:InvokeAsync") },
                                    // TODO ideally tighten this up...
                                    { "Resource", "*" }
                                },
                                new YamlMappingNode
                                {
                                    { "Effect", "Allow" },
                                    { "Action", new YamlSequenceNode("sqs:GetQueueUrl", "sqs:SendMessage") },
                                    // TODO ideally tighten this up...
                                    { "Resource", "*" }
                                },
                                // TODO allow dynamic injection / file inclusion
                                new YamlMappingNode
                                {
                                    { "Effect", "Allow" },
                                    { "Action", new YamlSequenceNode("dynamodb:*") },
                                    { "Resource", "*" }
                                }
                            }
                        },
                        {
                            "tracing", new YamlMappingNode
                            {
                                { "apiGateway", "true" },
                                { "lambda", "true" },
                            }
                        },
                    }
                },
                {
                    "package", new YamlMappingNode
                    {
                        { "individually", "true" },
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

            switch (plugin.ServiceType)
            {
                case ServiceType.MessageHandler:
                    var routes = RouteDiscoverer.Discover(plugin);
                    if (routes.Any(x => !RouteHandlerFactory.IsRpc(x)))
                    {
                        var queueName = _namingConventions.GetSqsName(plugin);
                        var queueRefName = ToTitleCase(plugin.ServiceKey.Replace('.', '-'));

                        functions.Add(functionName + "-queue", new YamlMappingNode
                        {
                            { "name", functionName + "-queue" },
                            { "handler", "Toxon.Micro.RabbitBlog.Serverless.Host::Toxon.Micro.RabbitBlog.Serverless.Host.LambdaFunction::HandleQueueAsync" },
                            { "environment", env },
                            { "memorySize", "512" },
                            { "timeout", "60" },
                            { "events", new YamlSequenceNode(BuildSqsEvent(queueRefName)) },
                            {
                                "package", new YamlMappingNode
                                {
                                    { "artifact", $"artifacts/{service.Name}.zip" },
                                }
                            },
                        });
                        resources.Add(queueRefName, BuildQueue(queueName));
                    }

                    if (routes.Any(RouteHandlerFactory.IsRpc))
                    {
                        functions.Add(functionName, new YamlMappingNode
                        {
                            { "name", functionName },
                            { "handler", "Toxon.Micro.RabbitBlog.Serverless.Host::Toxon.Micro.RabbitBlog.Serverless.Host.LambdaFunction::HandleDirectAsync" },
                            { "environment", env },
                            { "memorySize", "512" },
                            { "timeout", "60" },
                            {
                                "package", new YamlMappingNode
                                {
                                    { "artifact", $"artifacts/{service.Name}.zip" },
                                }
                            },
                        });
                    }
                    break;
                case ServiceType.Http:
                    env.Add("HTTP_SERVICE_KEY", plugin.ServiceKey);

                    functions.Add(functionName, new YamlMappingNode
                    {
                        { "name", functionName },
                        { "handler", "Toxon.Micro.RabbitBlog.Serverless.Host::Toxon.Micro.RabbitBlog.Serverless.Host.HttpFunction::FunctionHandlerAsync" },
                        { "environment", env },
                        { "memorySize", "512" },
                        { "timeout", "60" },
                        { "events", new YamlSequenceNode(BuildHttpEvent()) },
                        {
                            "package", new YamlMappingNode
                            {
                                { "artifact", $"artifacts/{service.Name}.zip" },
                            }
                        },
                    });
                    break;

                default: throw new ArgumentOutOfRangeException();
            }
        }
        private void AddRouterFunction(YamlMappingNode functions)
        {
            functions.Add("router", new YamlMappingNode
            {
                {"name", $"{_namingConventions.GetServiceName()}-router"},
                {"handler", "Toxon.Micro.RabbitBlog.Serverless.Router::Toxon.Micro.RabbitBlog.Serverless.Router.RouterFunction::HandleDirectAsync"},
                {"memorySize", "512"},
                {"timeout", "60"},
                {
                    "package", new YamlMappingNode
                    {
                        {"artifact", "artifacts/router.zip"},
                    }
                },
            });
            functions.Add("router-queue", new YamlMappingNode
            {
                {"name", $"{_namingConventions.GetServiceName()}-router-queue"},
                {"handler", "Toxon.Micro.RabbitBlog.Serverless.Router::Toxon.Micro.RabbitBlog.Serverless.Router.RouterFunction::HandleQueueAsync"},
                {"memorySize", "512"},
                {"timeout", "60"},
                {
                    "events", new YamlSequenceNode
                    {
                        BuildSqsEvent("RouterQueue")
                    }
                },
                {
                    "package", new YamlMappingNode
                    {
                        {"artifact", "artifacts/router.zip"},
                    }
                },
            });
        }

        private YamlNode BuildQueue(string name)
        {
            return new YamlMappingNode
            {
                { "Type", "AWS::SQS::Queue" },
                { "Properties", new YamlMappingNode
                    {
                        { "QueueName", name },
                        { "VisibilityTimeout", "60" },
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