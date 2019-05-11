using System;
using Toxon.Micro.RabbitBlog.Routing;

namespace Toxon.Micro.RabbitBlog.Serverless.Core
{
    public static class LambdaConfig
    {
        public static IRoutingSender CreateSender()
        {
            return new LambdaSender(GetRouterQueueName(), GetRouterFunctionName());
        }  
   
        public static string GetRouterQueueName()
        {
            return Environment.GetEnvironmentVariable("ROUTER_QUEUE_NAME");
        }
        public static string GetRouterFunctionName()
        {
            return Environment.GetEnvironmentVariable("ROUTER_FUNCTION_NAME");
        }
    }
}
