using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using Toxon.Micro.RabbitBlog.Plugins.Core;
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
            var tempRoot = Path.Combine(outputRoot, "temp");

            // Router
            PackageRouter(outputRoot, artifactsFolder);

            // Function entrypoints
            using (var serviceEntryFolder = RunPublish(tempRoot, Path.Combine(_options.Root, "Toxon.Micro.RabbitBlog.Serverless.ServiceEntry", "Toxon.Micro.RabbitBlog.Serverless.ServiceEntry.csproj"), "serviceEntry", true))
            using (var httpEntryFolder = RunPublish(tempRoot, Path.Combine(_options.Root, "Toxon.Micro.RabbitBlog.Serverless.HttpEntry", "Toxon.Micro.RabbitBlog.Serverless.HttpEntry.csproj"), "httpEntry", true))
            {
                var serviceEntryAssembly = Path.Combine(serviceEntryFolder.Path, "Toxon.Micro.RabbitBlog.Serverless.ServiceEntry.dll");
                var httpEntryAssembly = Path.Combine(httpEntryFolder.Path, "Toxon.Micro.RabbitBlog.Serverless.HttpEntry.dll");

                // Services
                var services = ServiceDiscoverer.Discover(_serviceDiscoveryOptions);
                var pluginLoaders = Bootstrapper.LoadPlugins(services.Select(x => x.AssemblyPath));

                foreach (var service in services)
                {
                    var assembly = pluginLoaders.Assemblies.Single(x => x.GetName().Name == service.Name);
                    PackageService(service, assembly, tempRoot, serviceEntryAssembly, httpEntryAssembly, artifactsFolder);
                }
            }
        }

        private void PackageService(ServiceProject service, Assembly assembly, string tempRoot, string serviceEntryAssembly, string httpEntryAssembly, string artifactsFolder)
        {
            var plugins = PluginDiscoverer.Discover(assembly);
            if (!plugins.Any())
            {
                return;
            }

            var serviceFolder = RunPublish(tempRoot, service.ProjectPath, service.Name, false);
            var serviceAssembly = Path.Combine(serviceFolder.Path, Path.GetFileName(assembly.Location));

            var builder = new FunctionBuilder.Builder();

            if (plugins.Any(x => x.ServiceType == ServiceType.MessageHandler))
            {
                using (var buildTargetFolder = new TempDirectory(tempRoot, service.Name + "-service"))
                {
                    var buildContext = new FunctionBuilder.BuildContext(ServiceType.MessageHandler, serviceEntryAssembly, serviceAssembly, buildTargetFolder.Path);
                    builder.Build(buildContext);

                    var packagePath = Path.Combine(artifactsFolder, $"{service.Name}-service.zip");
                    CreateZip(buildTargetFolder, packagePath);
                }
            }

            if (plugins.Any(x => x.ServiceType == ServiceType.Http))
            {
                using (var buildTargetFolder = new TempDirectory(tempRoot, service.Name + "-http"))
                {
                    var buildContext = new FunctionBuilder.BuildContext(ServiceType.Http, httpEntryAssembly, serviceAssembly, buildTargetFolder.Path);
                    builder.Build(buildContext);

                    var packagePath = Path.Combine(artifactsFolder, $"{service.Name}-http.zip");
                    CreateZip(buildTargetFolder, packagePath);
                }
            }
        }

        private void PackageRouter(string outputRoot, string artifactsFolder)
        {
            var projectPath = Path.Combine(_options.Root, "Toxon.Micro.RabbitBlog.Serverless.Router", "Toxon.Micro.RabbitBlog.Serverless.Router.csproj");
            var packagePath = Path.Combine(artifactsFolder, "router.zip");

            using (var tempFolder = RunPublish(Path.Combine(outputRoot, "temp"), projectPath, "router", true))
            {

                CreateZip(tempFolder, packagePath);
            }

            AddRoutesToPackage(outputRoot, packagePath);
        }

        private static void CreateZip(TempDirectory folder, string zipPath)
        {
            File.Delete(zipPath);
            ZipFile.CreateFromDirectory(folder.Path, zipPath);
            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Update))
            {
                foreach (var entry in zip.Entries)
                {
                    // 100777 << 16L

                    // set high order bits to octal 100777
                    entry.ExternalAttributes &= 0xffff;
                    entry.ExternalAttributes |= (0b001_000_000_111_111_111) << 16;
                }
            }
        }

        private TempDirectory RunPublish(string tempRoot, string projectPath, string projectName, bool isExecutable)
        {
            var tempFolder = new TempDirectory(tempRoot, projectName);

            var argsBuilder = new StringBuilder();
            argsBuilder.Append("publish");
            argsBuilder.Append($" {projectPath}");
            argsBuilder.Append($" -c {_options.Configuration}");
            argsBuilder.Append($" -o {tempFolder}");
            if (isExecutable)
            {
                argsBuilder.Append(" -f netcoreapp2.1");
                argsBuilder.Append(" /p:GenerateRuntimeConfigurationFiles=true");
            }
            argsBuilder.Append(" --self-contained false");
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