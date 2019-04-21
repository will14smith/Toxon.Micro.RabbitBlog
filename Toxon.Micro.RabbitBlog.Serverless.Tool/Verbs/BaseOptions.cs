using CommandLine;

namespace Toxon.Micro.RabbitBlog.Serverless.Tool.Verbs
{
    public class BaseOptions
    {
        [Value(0, Default = ".", HelpText = "The root directory of the solution, used for service discovery")]
        public string Root { get; set; }
        [Option("output-directory", Default = "output", HelpText = "The relative path from the root directory to place the output")]
        public string OutputDir { get; set; }

        [Option('n', "name", Default = "rabbitblog", HelpText = "The name of the service, used for resource naming")]
        public string Name { get; set; }

        [Option('c', "configuration", Default = "Debug", HelpText = "The build configuration to use (default: Debug)")]
        public string Configuration { get; set; }
    }
}