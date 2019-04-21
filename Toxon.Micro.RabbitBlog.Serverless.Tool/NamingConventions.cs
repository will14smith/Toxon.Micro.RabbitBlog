using System.Reflection;
using Toxon.Micro.RabbitBlog.Plugins.Reflection;

namespace Toxon.Micro.RabbitBlog.Serverless.Tool
{
    internal class NamingConventions
    {
        private readonly string _baseName;

        public NamingConventions(string baseName)
        {
            _baseName = baseName;
        }

        public string GetServiceName()
        {
            return _baseName;
        }

        public string GetLambdaName(PluginMetadata plugin)
        {
            return $"{_baseName}-{GetSafeName(plugin)}";
        }

        public string GetLambdaArn(PluginMetadata plugin)
        {
            return $"arn:aws:lambda:{{region}}:{{account-id}}:function:{GetLambdaName(plugin)}";
        }

        public string GetSqsName(PluginMetadata plugin)
        {
            return $"{_baseName}-{GetSafeName(plugin)}";
        }

        public string GetSqsArn(PluginMetadata plugin)
        {
            return $"arn:aws:sqs:{{region}}:{{account-id}}:{GetSqsName(plugin)}";
        }

        private static string GetSafeName(PluginMetadata plugin)
        {
            return plugin.ServiceKey.Replace('.', '-').ToLowerInvariant();
        }
    }
}