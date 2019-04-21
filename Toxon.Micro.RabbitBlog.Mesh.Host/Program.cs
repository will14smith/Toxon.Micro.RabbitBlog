using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Toxon.Micro.RabbitBlog.Plugins.Core;
using Toxon.Micro.RabbitBlog.Plugins.Reflection;
using Toxon.Micro.RabbitBlog.Routing;

namespace Toxon.Micro.RabbitBlog.Mesh.Host
{
    class Program
    {
        private static int Port = 8499;

        static async Task Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<Options>(args);
            if (result is Parsed<Options> parsed)
            {
                await Run(parsed.Value);
            }
        }

        private static async Task Run(Options opts)
        {
            var wellKnownBases = new WellKnownBases(opts.BaseAddresses);

            var pluginLoader = Bootstrapper.LoadPlugins(new[] { opts.AssemblyPath });
            var plugins = PluginDiscoverer.Discover(pluginLoader.Assemblies);

            var models = new List<RoutingModel>();
            foreach (var plugin in plugins)
            {
                var model = new RoutingModel(plugin.ServiceKey, wellKnownBases, new RoutingModelOptions());
                models.Add(model);

                switch (plugin.ServiceType)
                {
                    case ServiceType.MessageHandler:
                        await Bootstrapper.RegisterPluginAsync(model, model, plugin);
                        break;
                    case ServiceType.Http:
                        StartWebServer(plugin, model);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(1500));

            foreach (var model in models)
            {
                await model.StartAsync();
            }

            Console.WriteLine($"Running {string.Join(", ", plugins.Select(x => x.ServiceKey))}... press enter to exit!");
            Console.ReadLine();
        }

        private static IWebHost StartWebServer(PluginMetadata plugin, IRoutingSender sender)
        {
            var port = Interlocked.Increment(ref Port);

            Console.WriteLine($"Running {plugin.ServiceKey} HTTP server on {port}");

            return new WebHostBuilder()
                .UseKestrel(k => k.ListenLocalhost(port))
                .ConfigureServices(services => services.AddSingleton(sender))
                .UseStartup(plugin.Type)
                .Start();
        }
    }
}
