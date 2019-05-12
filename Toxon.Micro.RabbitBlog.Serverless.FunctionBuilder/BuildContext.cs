using Toxon.Micro.RabbitBlog.Plugins.Core;

namespace Toxon.Micro.RabbitBlog.Serverless.FunctionBuilder
{
    public class BuildContext
    {
        public BuildContext(ServiceType serviceType, string pathToFunctionEntryAssembly, string pathToFunctionAssembly, string pathToTargetFolder)
        {
            ServiceType = serviceType;

            PathToFunctionEntryAssembly = pathToFunctionEntryAssembly;
            PathToFunctionAssembly = pathToFunctionAssembly;

            PathToTargetFolder = pathToTargetFolder;
        }

        public ServiceType ServiceType { get; }

        public string PathToFunctionEntryAssembly { get; }
        public string PathToFunctionAssembly { get; }

        public string PathToTargetFolder { get; }
    }
}