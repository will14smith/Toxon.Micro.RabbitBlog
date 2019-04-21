using System;
using System.IO;
using CommandLine;
using Newtonsoft.Json;
using Toxon.Micro.RabbitBlog.Serverless.Tool.Verbs;

namespace Toxon.Micro.RabbitBlog.Serverless.Tool
{
    class Program
    {
        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<RouterOptions, ServerlessOptions, PackageOptions, BuildOptions>(args)
                .MapResult(
                    (RouterOptions options) => RunRouter(options),
                    (ServerlessOptions options) => RunServerless(options),
                    (PackageOptions options) => RunPackage(options),
                    (BuildOptions options) => RunBuild(options),
                    _ => 1);
        }

        private static int RunRouter(RouterOptions options)
        {
            var root = Path.GetFullPath(options.Root, Environment.CurrentDirectory);
            var outputRoot = Path.Combine(root, options.OutputDir);
            Directory.CreateDirectory(outputRoot);

            var builder = new RouterConfigBuilder(BuildServiceDiscoveryOptions(options, root), new NamingConventions(options.Name));

            var config = builder.Build();
            var configString = JsonConvert.SerializeObject(config, Formatting.Indented);

            var outputPath = Path.GetFullPath(options.Output, outputRoot);
            File.WriteAllText(outputPath, configString);

            return 0;
        }
        
        private static int RunServerless(ServerlessOptions options)
        {
            var root = Path.GetFullPath(options.Root, Environment.CurrentDirectory);
            var outputRoot = Path.Combine(root, options.OutputDir);
            Directory.CreateDirectory(outputRoot);

            var serverlessBuilder = new ServerlessBuilder(BuildServiceDiscoveryOptions(options, root), new NamingConventions(options.Name));

            var serverlessConfig = serverlessBuilder.Build();

            var outputPath = Path.GetFullPath(options.Output, outputRoot);
            File.WriteAllText(outputPath, serverlessConfig);

            return 0;
        }

        private static int RunPackage(PackageOptions options)
        {
            var root = Path.GetFullPath(options.Root, Environment.CurrentDirectory);
            var outputRoot = Path.Combine(root, options.OutputDir);
            Directory.CreateDirectory(outputRoot);

            var packager = new Packager(BuildServiceDiscoveryOptions(options, root), options);

            packager.Package(outputRoot);

            return 0;
        }

        private static int RunBuild(BuildOptions options)
        {
            var result = RunRouter(CopyBaseOptions(new RouterOptions(), options));
            if (result != 0) return result;

            result = RunServerless(CopyBaseOptions(new ServerlessOptions(), options));
            if (result != 0) return result;

            return RunPackage(CopyBaseOptions(new PackageOptions(), options));
        }

        private static T CopyBaseOptions<T>(T target, BuildOptions source)
            where T : BaseOptions
        {
            target.Root = source.Root;
            target.OutputDir = source.OutputDir;
            target.Name = source.Name;
            target.Configuration = source.Configuration;

            return target;
        }

        private static ServiceDiscoveryOptions BuildServiceDiscoveryOptions(BaseOptions options, string root)
            => new ServiceDiscoveryOptions { Root = root };
    }
}
