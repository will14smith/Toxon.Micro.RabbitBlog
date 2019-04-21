using CommandLine;

namespace Toxon.Micro.RabbitBlog.Serverless.Tool.Verbs
{
    [Verb("router-config", HelpText = "Discover all the services and create a routing config")]
    internal class RouterOptions : BaseOptions
    {
        [Option('o', "output", Default = "routes.json", HelpText = "Output path for the router config (default: routes.json)")]
        public string Output { get; set; } = "routes.json";
    }
}