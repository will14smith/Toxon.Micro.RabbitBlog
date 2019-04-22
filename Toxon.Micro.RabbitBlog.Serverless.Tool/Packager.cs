using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Toxon.Micro.RabbitBlog.Plugins.Reflection;
using Toxon.Micro.RabbitBlog.Serverless.Tool.Verbs;

namespace Toxon.Micro.RabbitBlog.Serverless.Tool
{
    internal class Packager
    {
        private readonly ServiceDiscoveryOptions _serviceDiscoveryOptions;
        private readonly PackageOptions _options;

        public Packager(ServiceDiscoveryOptions serviceDiscoveryOptions, PackageOptions options)
        {
            _serviceDiscoveryOptions = serviceDiscoveryOptions;
            _options = options;
        }

        public void Package(string outputRoot)
        {
            var artifactsFolder = Path.Combine(outputRoot, "artifacts");
            Directory.CreateDirectory(artifactsFolder);

            Package(outputRoot, artifactsFolder, Path.Combine(_options.Root, "Toxon.Micro.RabbitBlog.Serverless.Host", "Toxon.Micro.RabbitBlog.Serverless.Host.csproj"), "host", true);
            var routerPackage = Package(outputRoot, artifactsFolder, Path.Combine(_options.Root, "Toxon.Micro.RabbitBlog.Serverless.Router", "Toxon.Micro.RabbitBlog.Serverless.Router.csproj"), "router", true);
            AddRoutesToPackage(outputRoot, routerPackage);

            var services = ServiceDiscoverer.Discover(_serviceDiscoveryOptions);
            var pluginLoaders = Bootstrapper.LoadPlugins(services.Select(x => x.AssemblyPath));

            foreach (var service in services)
            {
                var assembly = pluginLoaders.Assemblies.Single(x => x.GetName().Name == service.Name);
                var plugins = PluginDiscoverer.Discover(assembly);
                if (!plugins.Any())
                {
                    continue;
                }

                Package(outputRoot, artifactsFolder, service.ProjectPath, service.Name, false);
            }
        }

        private string Package(string outputRoot, string artifactsFolder, string projectPath, string projectName, bool isExecutable)
        {
            var tempFolder = RunPublish(outputRoot, projectPath, projectName, isExecutable);
            var packagePath = Path.Combine(artifactsFolder, $"{projectName}.zip");
            File.Delete(packagePath);

            ZipFile.CreateFromDirectory(tempFolder, packagePath);
            using (var zip = ZipFile.Open(packagePath, ZipArchiveMode.Update))
            {
                foreach (var entry in zip.Entries)
                {
                    // 100777 << 16L

                    // set high order bits to octal 100777
                    entry.ExternalAttributes &= 0xffff;
                    entry.ExternalAttributes |= (0b001_000_000_111_111_111) << 16;
                }
            }

            Directory.Delete(tempFolder, true);

            return packagePath;
        }

        private string RunPublish(string outputRoot, string projectPath, string projectName, bool isExecutable)
        {
            var tempFolder = Path.Combine(outputRoot, "temp", projectName + "-" + DateTime.UtcNow.ToString("yyyyMMMMddHHmmss"));
            Directory.CreateDirectory(tempFolder);

            var argsBuilder = new StringBuilder();
            argsBuilder.Append("publish");
            argsBuilder.Append($" {projectPath}");
            argsBuilder.Append($" -c {_options.Configuration}");
            argsBuilder.Append($" -o {tempFolder}");
            if (isExecutable)
            {
                argsBuilder.Append(" -f netcoreapp2.1");
            }
            argsBuilder.Append(" --self-contained false");
            argsBuilder.Append(" /p:GenerateRuntimeConfigurationFiles=true");
            argsBuilder.Append(" /p:PreserveCompilationContext=false");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = argsBuilder.ToString()
                }
            };
            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new NotImplementedException("TODO handle errors");
            }

            return tempFolder;
        }

        private void AddRoutesToPackage(string outputRoot, string routerPackagePath)
        {
            var routesFilePath = Path.Combine(outputRoot, "routes.json");

            using (var zip = ZipFile.Open(routerPackagePath, ZipArchiveMode.Update))
            using (var routesFile = File.OpenRead(routesFilePath))
            {
                var entry = zip.CreateEntry("routes.json");
                using (var entryStream = entry.Open())
                {
                    routesFile.CopyTo(entryStream);
                    entryStream.Flush();
                }
            }
        }
    }
}