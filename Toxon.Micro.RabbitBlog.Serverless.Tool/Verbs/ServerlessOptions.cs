using CommandLine;

namespace Toxon.Micro.RabbitBlog.Serverless.Tool.Verbs
{
    [Verb("serverless-config", HelpText = "Discover all the services and create a serverless.yml file")]
    internal class ServerlessOptions : BaseOptions
    {
        [Option('o', "output", Default = "serverless.yml", HelpText = "Output path for the serverless config (default: serverless.yml)")]
        public string Output { get; set; } = "serverless.yml";
    }
}