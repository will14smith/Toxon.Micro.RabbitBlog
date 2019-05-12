using System;
using System.IO;
using Toxon.Micro.RabbitBlog.Plugins.Reflection;

namespace Toxon.Micro.RabbitBlog.Serverless.FunctionBuilder
{
    public class Builder
    {
        public void Build(BuildContext context)
        {
            DirectoryCopy(Path.GetDirectoryName(context.PathToFunctionAssembly), context.PathToTargetFolder, true, true);
            DirectoryCopy(Path.GetDirectoryName(context.PathToFunctionEntryAssembly), context.PathToTargetFolder, true, false);

            var pathToFunctionAssembly = Path.Combine(context.PathToTargetFolder, Path.GetFileName(context.PathToFunctionAssembly));
            var pathToFunctionEntryAssembly = Path.Combine(context.PathToTargetFolder, Path.GetFileName(context.PathToFunctionEntryAssembly));

            var pluginLoaders = Bootstrapper.LoadPlugins(new[] { pathToFunctionAssembly });
            var plugins = PluginDiscoverer.Discover(pluginLoaders.Assemblies);

            FunctionDependencyMerger.Merge(pathToFunctionEntryAssembly, pathToFunctionAssembly);
            FunctionImplementor.Implement(pathToFunctionEntryAssembly, plugins);
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
