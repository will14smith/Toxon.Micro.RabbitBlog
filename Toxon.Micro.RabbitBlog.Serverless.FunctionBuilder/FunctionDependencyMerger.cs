using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyModel;

namespace Toxon.Micro.RabbitBlog.Serverless.FunctionBuilder
{
    public class FunctionDependencyMerger
    {
        public static DependencyContext Merge(string pathToFunctionEntryAssembly, string pathToFunctionAssembly)
        {
            var reader = new DependencyContextJsonReader();
            var writer = new DependencyContextWriter();

            var entryDependencies = Read(pathToFunctionEntryAssembly, reader);
            var functionDependencies = Read(pathToFunctionAssembly, reader);

            var combinedDependencies = functionDependencies.Merge(entryDependencies);
            
            using (var file = File.Open(GetDependenciesPathFromAssemblyPath(pathToFunctionEntryAssembly), FileMode.Create, FileAccess.Write))
            {
                writer.Write(combinedDependencies, file);
            }
            return combinedDependencies;
        }

        private static DependencyContext Read(string assemblyPath, IDependencyContextReader reader)
        {
            var path = GetDependenciesPathFromAssemblyPath(assemblyPath);

            using (var file = File.OpenRead(path))
            {
                return reader.Read(file);
            }
        }

        private static string GetDependenciesPathFromAssemblyPath(string assemblyPath)
        {
            var folder = Path.GetDirectoryName(assemblyPath);
            var fileName = $"{Path.GetFileNameWithoutExtension(assemblyPath)}.deps.json";

            return Path.Combine(folder, fileName);
        }
    }
}