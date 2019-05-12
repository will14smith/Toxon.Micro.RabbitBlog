using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;

namespace Toxon.Micro.RabbitBlog.Plugins.Reflection
{
    public class PluginsAssemblyLoader : IDisposable
    {
        public IReadOnlyCollection<Assembly> Assemblies { get; }

        private readonly DependencyContext _deps;
        private readonly IReadOnlyDictionary<string, (string Name, RuntimeLibrary Library, RuntimeAssetGroup Assets)> _assemblyMap;
        private readonly ICompilationAssemblyResolver _resolver;
        private readonly IReadOnlyCollection<AssemblyLoadContext> _loaders;

        public PluginsAssemblyLoader(IEnumerable<string> pluginPaths)
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
            _assemblyMap = BuildAssemblyMap(_deps);
            _resolver = new CompositeCompilationAssemblyResolver(BuildResolvers(pluginPathsList));
            _loaders = BuildLoaders(Assemblies);
        }

        private IReadOnlyDictionary<string, (string Name, RuntimeLibrary Library, RuntimeAssetGroup Assets)> BuildAssemblyMap(DependencyContext deps)
        {
            var runtimeIdentifier = RuntimeEnvironment.GetRuntimeIdentifier();
            var fallbacks = _deps.RuntimeGraph
                .FirstOrDefault(fallback => string.Equals(fallback.Runtime, runtimeIdentifier, StringComparison.OrdinalIgnoreCase))
                ?.Fallbacks;
            if (fallbacks == null)
            {
                runtimeIdentifier = GetFallbackRuntime();
                fallbacks = _deps.RuntimeGraph
                    .FirstOrDefault(fallback => string.Equals(fallback.Runtime, runtimeIdentifier, StringComparison.OrdinalIgnoreCase))
                    ?.Fallbacks;
            }
            if (fallbacks == null)
            {
                fallbacks = new RuntimeFallbacks("unknown", "any", "base").Fallbacks;
            }

            var runtimes = new[] { runtimeIdentifier }.Concat(fallbacks).Append(string.Empty).ToList();

            return _deps.RuntimeLibraries
                            .Where(library => library.RuntimeAssemblyGroups.Count > 0)
                            .Select(library => FindCompatibleAssemblies(runtimes, library))
                            .SelectMany(ToAssemblyMapEntries)
                            // Remove duplicates
                            .GroupBy(x => x.Name).Select(x => x.First())
                            .ToDictionary(x => x.Name);
        }

        private string GetFallbackRuntime()
        {
            string result;
            switch (RuntimeEnvironment.OperatingSystemPlatform)
            {
                case Platform.Windows:
                    result = "win10" + RuntimeEnvironment.RuntimeArchitecture;
                    break;

                case Platform.Linux:
                    result = "linux" + RuntimeEnvironment.RuntimeArchitecture;
                    break;

                default:
                    result = "unknown";
                    break;
            }

            return result;
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
            if (!_assemblyMap.TryGetValue(name.Name, out var mapResult))
            {
                return null;
            }

            var (_, library, assets) = mapResult;

            var compilationLibrary = new CompilationLibrary(
                library.Type,
                library.Name,
                library.Version,
                library.Hash,
                assets.AssetPaths,
                library.Dependencies,
                library.Serviceable,
                library.Path,
                library.HashPath
            );

            var assemblies = new List<string>();
            if (!_resolver.TryResolveAssemblyPaths(compilationLibrary, assemblies))
            {
                return null;
            }

            var assemblyPath = assemblies.Single();

            return loader.LoadFromAssemblyPath(assemblyPath);
        }

        private IEnumerable<(string Name, RuntimeLibrary Library, RuntimeAssetGroup Assets)> ToAssemblyMapEntries((RuntimeLibrary Library, RuntimeAssetGroup Assets) input)
        {
            var (library, assets) = input;

            return assets?.AssetPaths.Select(path => (Path.GetFileNameWithoutExtension(path), library, assets)) 
                   ?? Enumerable.Empty<(string Name, RuntimeLibrary Library, RuntimeAssetGroup Assets)>();
        }

        private (RuntimeLibrary Library, RuntimeAssetGroup Assets) FindCompatibleAssemblies(IEnumerable<string> runtimes, RuntimeLibrary library)
        {
            return runtimes
                .Select(runtime => FindAssembliesForRuntime(runtime, library))
                .FirstOrDefault(x => x.Assets?.AssetPaths != null);
        }
        private (RuntimeLibrary Library, RuntimeAssetGroup Assets) FindAssembliesForRuntime(string runtime, RuntimeLibrary library)
        {
            var assets = library.RuntimeAssemblyGroups
                .SingleOrDefault(x => x.Runtime == runtime);

            return (library, assets);
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
