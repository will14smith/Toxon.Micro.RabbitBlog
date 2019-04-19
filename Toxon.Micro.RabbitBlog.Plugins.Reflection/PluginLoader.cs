using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;

namespace Toxon.Micro.RabbitBlog.Plugins.Reflection
{
    public class PluginLoader : IDisposable
    {
        public Assembly Assembly { get; }

        private readonly DependencyContext _deps;
        private readonly ICompilationAssemblyResolver _resolver;
        private readonly AssemblyLoadContext _loader;

        public PluginLoader(string pluginPath)
        {
            Assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(pluginPath);

            _deps = DependencyContext.Load(Assembly);
            _resolver = new CompositeCompilationAssemblyResolver(new ICompilationAssemblyResolver[]
            {
                new AppBaseCompilationAssemblyResolver(Path.GetDirectoryName(pluginPath)),
                new ReferenceAssemblyPathResolver(),
                new PackageCompilationAssemblyResolver(),
            });
            _loader = AssemblyLoadContext.GetLoadContext(Assembly);

            _loader.Resolving += OnResolving;
        }

        private Assembly OnResolving(AssemblyLoadContext loader, AssemblyName name)
        {
            var library = _deps.RuntimeLibraries.SingleOrDefault(lib => lib.Name == name.Name);
            if (library == null)
            {
                return null;
            }

            var compilationLibrary = ToCompilationLibrary(library);

            var assemblies = new List<string>();
            if (!_resolver.TryResolveAssemblyPaths(compilationLibrary, assemblies))
            {
                return null;
            }

            var assembly = assemblies.Single();

            return _loader.LoadFromAssemblyPath(assembly);
        }

        private CompilationLibrary ToCompilationLibrary(RuntimeLibrary library)
        {
            return new CompilationLibrary(
                library.Type,
                library.Name,
                library.Version,
                library.Hash,
                library.RuntimeAssemblyGroups.SelectMany(x => x.AssetPaths),
                library.Dependencies,
                library.Serviceable,
                library.Path,
                library.HashPath
            );
        }

        public void Dispose()
        {
            _loader.Resolving -= OnResolving;
        }
    }
}
