using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Toxon.Micro.RabbitBlog.Serverless.Tool
{
    public class ServiceDiscoverer
    {
        public static IReadOnlyCollection<ServiceProject> Discover(ServiceDiscoveryOptions options)
        {
            var files = Directory.GetFiles(options.Root, "*.csproj", SearchOption.AllDirectories);

            return files
                .Where(x => !x.EndsWith(".Tests.csproj") && !x.EndsWith(".Test.csproj") && !x.Contains("node_modules"))
                .Select(projectPath => new ServiceProject(projectPath, FindAssembly(options, projectPath)))
                .ToList();
        }

        private static string FindAssembly(ServiceDiscoveryOptions options, string projectPath)
        {
            var assemblyName = Path.GetFileNameWithoutExtension(projectPath) + ".dll";

            var files = Directory.GetFiles(options.Root, assemblyName, SearchOption.AllDirectories);

            return files
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .First();
        }
    }

    public class ServiceProject
    {
        public ServiceProject(string projectPath, string assemblyPath)
        {
            ProjectPath = projectPath;
            AssemblyPath = assemblyPath;
        }

        public string Name => Path.GetFileNameWithoutExtension(ProjectPath);
        public string ProjectPath { get; }
        public string AssemblyPath { get; }
    }
}
