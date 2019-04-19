using System;
using System.Threading;
using System.Threading.Tasks;
using Toxon.Micro.RabbitBlog.Core;

namespace Toxon.Micro.RabbitBlog.Plugins.Reflection
{
    public class RouteHandlerFactory
    {
        public delegate Task<Message> RpcHandler(Message message, CancellationToken cancellationToken);
        public delegate Task BusHandler(Message message, CancellationToken cancellationToken);

        public static bool IsRpc(RouteMetadata route)
        {
            var returnType = route.Method.ReturnType;

            if (returnType == typeof(Task))
            {
                return false;
            }

            if (returnType.IsConstructedGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                return true;
            }

            throw new InvalidOperationException("Invalid return type");
        }

        public static RpcHandler BuildRpcHandler(object plugin, RouteMetadata route)
        {
            var method = route.Method;

            if (!method.ReturnType.IsConstructedGenericType || method.ReturnType.GetGenericTypeDefinition() != typeof(Task<>))
            {
                throw new InvalidOperationException("Return type of RPC method should be Task<T>");
            }

            var builder = new RouteHandlerBuilder();

            foreach (var param in method.GetParameters())
            {
                builder.AddArgument(param);
            }

            var returnType = method.ReturnType.GetGenericArguments()[0];
            builder.SetReturnType(returnType);

            return builder.Build<RpcHandler>(plugin, method);
        }

        public static BusHandler BuildBusHandler(object plugin, RouteMetadata route)
        {
            var method = route.Method;

            if (method.ReturnType != typeof(Task))
            {
                throw new InvalidOperationException("Return type of Bus method should be Task");
            }

            var builder = new RouteHandlerBuilder();

            foreach (var param in method.GetParameters())
            {
                builder.AddArgument(param);
            }

            return builder.Build<BusHandler>(plugin, method);
        }
    }
}
