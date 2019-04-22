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

            var hostFolder = RunPublish(outputRoot, Path.Combine(_options.Root, "Toxon.Micro.RabbitBlog.Serverless.Host", "Toxon.Micro.RabbitBlog.Serverless.Host.csproj"), "host", true);
            PackageRouter(outputRoot, artifactsFolder);

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

                Package(outputRoot, artifactsFolder, service.ProjectPath, service.Name, hostFolder);
            }

            Directory.Delete(hostFolder, true);
        }

        private void PackageRouter(string outputRoot, string artifactsFolder)
        {
            string projectPath = Path.Combine(_options.Root, "Toxon.Micro.RabbitBlog.Serverless.Router", "Toxon.Micro.RabbitBlog.Serverless.Router.csproj");
            var tempFolder = RunPublish(outputRoot, projectPath, "router", true);
            var packagePath = Path.Combine(artifactsFolder, $"{"router"}.zip");

            CreateZip(tempFolder, packagePath);

            Directory.Delete(tempFolder, true);
            AddRoutesToPackage(outputRoot, packagePath);
        }

        private void Package(string outputRoot, string artifactsFolder, string projectPath, string projectName, string hostFolder)
        {
            var tempFolder = RunPublish(outputRoot, projectPath, projectName, false);
            DirectoryCopy(hostFolder, tempFolder, true, true);

            var packagePath = Path.Combine(artifactsFolder, $"{projectName}.zip");

            CreateZip(tempFolder, packagePath);

            Directory.Delete(tempFolder, true);
        }

        private static void CreateZip(string folder, string zipPath)
        {
            File.Delete(zipPath);
            ZipFile.CreateFromDirectory(folder, zipPath);
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

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool recursive, bool replaceExistingFiles = false)
        {
            var dir = new DirectoryInfo(sourceDirName);
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName);
            }

            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            foreach (var file in dir.GetFiles())
            {
                var path = Path.Combine(destDirName, file.Name);

                if (File.Exists(path) && !replaceExistingFiles)
                {
                    continue;
                }

                file.CopyTo(path, replaceExistingFiles);
            }

            if (!recursive)
            {
                return;
            }

            foreach (var sub in dir.GetDirectories())
            {
                DirectoryCopy(sub.FullName, Path.Combine(destDirName, sub.Name), true, replaceExistingFiles);
            }
        }
    }
}