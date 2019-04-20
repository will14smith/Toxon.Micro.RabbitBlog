using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Toxon.Micro.RabbitBlog.Plugins.Reflection;

namespace Toxon.Micro.RabbitBlog.Local.Host
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var pluginPaths = DiscoverPluginAssemblies();

            var pluginLoaders = Bootstrapper.LoadPlugins(pluginPaths);
            var plugins = PluginDiscoverer.Discover(pluginLoaders.Assemblies);

            var model = new LocalModel();
            foreach (var plugin in plugins)
            {
                await Bootstrapper.RegisterPluginAsync(model, model, plugin);
            }

            Console.WriteLine($"Running {string.Join(", ", plugins.Select(x => x.ServiceKey))}... press enter to exit!");
            Console.ReadLine();
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
