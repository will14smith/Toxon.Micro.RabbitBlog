using CommandLine;

namespace Toxon.Micro.RabbitBlog.Serverless.Tool.Verbs
{
    [Verb("build", HelpText = "Runs all the actions to create a deploy-able package")]
    internal class BuildOptions : BaseOptions
    {

    }
}