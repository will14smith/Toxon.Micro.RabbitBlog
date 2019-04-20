using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;

namespace Toxon.Micro.RabbitBlog.Plugins.Reflection
{
    public class PluginLoader : IDisposable
    {
        public IReadOnlyCollection<Assembly> Assemblies { get; }

        private readonly DependencyContext _deps;
        private readonly ICompilationAssemblyResolver _resolver;
        private readonly IReadOnlyCollection<AssemblyLoadContext> _loaders;

        public PluginLoader(IEnumerable<string> pluginPaths)
        {
            var pluginPathsList = pluginPaths.ToList();

            Assemblies = pluginPathsList.Select(pluginPath => AssemblyLoadContext.Default.LoadFromAssemblyPath(pluginPath)).ToList();

            var result = DependencyContext.Default;
            foreach (var assembly in Assemblies)
            {
                var context = DependencyContext.Load(assembly);
                if (context == null)
                {
                    continue;
                }

                result = result.Merge(context);
            }
            _deps = result;
            _resolver = new CompositeCompilationAssemblyResolver(BuildResolvers(pluginPathsList));
            _loaders = BuildLoaders(Assemblies);
        }

        private static ICompilationAssemblyResolver[] BuildResolvers(IEnumerable<string> pluginPathsList)
        {
            var resolvers = pluginPathsList
                .Select(Path.GetDirectoryName)
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .Select(directory => new AppBaseCompilationAssemblyResolver(directory))
                .Cast<ICompilationAssemblyResolver>()
                .ToList();

            resolvers.Add(new ReferenceAssemblyPathResolver());
            resolvers.Add(new PackageCompilationAssemblyResolver());

            return resolvers.ToArray();
        }

        private IReadOnlyCollection<AssemblyLoadContext> BuildLoaders(IEnumerable<Assembly> assemblies)
        {
            var loaders = new List<AssemblyLoadContext>();
            foreach (var assembly in assemblies)
            {
                var loader = AssemblyLoadContext.GetLoadContext(assembly);

                loader.Resolving += OnResolving;

                loaders.Add(loader);
            }

            return loaders;
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

            return loader.LoadFromAssemblyPath(assembly);
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
            foreach (var loader in _loaders)
            {
                loader.Resolving -= OnResolving;
            }
        }
    }
}
