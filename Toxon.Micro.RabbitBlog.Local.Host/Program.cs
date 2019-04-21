using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Toxon.Micro.RabbitBlog.Plugins.Core;
using Toxon.Micro.RabbitBlog.Plugins.Reflection;
using Toxon.Micro.RabbitBlog.Routing;

namespace Toxon.Micro.RabbitBlog.Local.Host
{
    class Program
    {
        private static int Port = 8499;

        static async Task Main(string[] args)
        {
            var pluginPaths = DiscoverPluginAssemblies();

            var pluginLoaders = Bootstrapper.LoadPlugins(pluginPaths);
            var plugins = PluginDiscoverer.Discover(pluginLoaders.Assemblies);

            var model = new LocalModel();
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
            var port = Interlocked.Increment(ref Port);

            Console.WriteLine($"Running {plugin.ServiceKey} HTTP server on {port}");

            return new WebHostBuilder()
                .UseKestrel(k => k.ListenLocalhost(port))
                .ConfigureServices(services => services.AddSingleton(sender))
                .UseStartup(plugin.Type)
                .Start();
        }

        private static IReadOnlyCollection<string> DiscoverPluginAssemblies()
        {
            // navigate to solution root
            var baseDir = Path.GetFullPath("../../../../", Environment.CurrentDirectory);
            var pattern = "Toxon.Micro.RabbitBlog.*.dll";

            var files = Directory.GetFiles(baseDir, pattern, SearchOption.AllDirectories);

            return files
                .Where(x => x.Contains($"bin{Path.DirectorySeparatorChar}Debug"))
                .Where(x => !x.EndsWith(".Tests.dll") && !x.EndsWith(".Test.dll"))
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .Distinct(new FileNameEqualityComparer())
                .ToList();
        }
    }

    internal class FileNameEqualityComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            var xFileName = Path.GetFileName(x)?.ToLower();
            var yFileName = Path.GetFileName(y)?.ToLower();

            return string.Equals(xFileName, yFileName, StringComparison.InvariantCultureIgnoreCase);
        }

        public int GetHashCode(string obj)
        {
            return Path.GetFileName(obj)?.ToLower().GetHashCode() ?? 0;
        }
    }
}
