using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Toxon.Micro.RabbitBlog.Plugins.Core;
using Toxon.Micro.RabbitBlog.Plugins.Reflection;
using Toxon.Micro.RabbitBlog.Routing;

namespace Toxon.Micro.RabbitBlog.Local.Host
{
    class Program
    {
        private static int _port = 8499;

        static async Task Main(string[] args)
        {
            var log = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                log.Error((Exception)eventArgs.ExceptionObject, "Unhandled error!");
            };

            var pluginPaths = Discover(Environment.CurrentDirectory);

            var pluginLoaders = Bootstrapper.LoadPlugins(pluginPaths);
            var plugins = PluginDiscoverer.Discover(pluginLoaders.Assemblies);

            var model = new LocalModel(log);
            foreach (var plugin in plugins)
            {
                switch (plugin.ServiceType)
                {
                    case ServiceType.MessageHandler:
                        await Bootstrapper.RegisterPluginAsync(model, model, plugin);
                        break;
                    case ServiceType.Http:
                        var host = StartWebServer(plugin, model);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            Console.WriteLine($"Running {string.Join(", ", plugins.Select(x => x.ServiceKey))}... press enter to exit!");
            Console.ReadLine();
        }

        private static IWebHost StartWebServer(PluginMetadata plugin, IRoutingSender sender)
        {
            var port = Interlocked.Increment(ref _port);

            Console.WriteLine($"Running {plugin.ServiceKey} HTTP server on {port}");

            return new WebHostBuilder()
                .UseKestrel(k => k.ListenLocalhost(port))
                .ConfigureServices(services => services.AddSingleton(sender))
                .UseStartup(plugin.Type)
                .Start();
        }

        public static IReadOnlyCollection<string> Discover(string rootDir)
        {
            var files = Directory.GetFiles(rootDir, "*.csproj", SearchOption.AllDirectories);

            return files
                .Where(x => !x.EndsWith(".Tests.csproj") && !x.EndsWith(".Test.csproj") && !x.Contains("node_modules"))
                .Select(projectPath => FindAssembly(rootDir, projectPath))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
        }

        private static string FindAssembly(string rootDir, string projectPath)
        {
            var assemblyName = Path.GetFileNameWithoutExtension(projectPath) + ".dll";

            var files = Directory.GetFiles(rootDir, assemblyName, SearchOption.AllDirectories);

            return files
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
        }
    }
}
