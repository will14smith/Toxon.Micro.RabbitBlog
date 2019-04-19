using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Toxon.Micro.RabbitBlog.Plugins.Core;

namespace Toxon.Micro.RabbitBlog.Plugins.Reflection
{
    public static class RouteDiscoverer
    {
        public static IReadOnlyCollection<RouteMetadata> Discover(PluginMetadata plugin)
        {
            var methods = plugin.Type.GetMethods();

            return methods.SelectMany(Discover).ToList();
        }

        private static IReadOnlyCollection<RouteMetadata> Discover(MethodInfo method)
        {
            return method.GetCustomAttributes<MessageRouteAttribute>()
                .Select(x => new RouteMetadata(x.Route, method))
                .ToList();
        }
    }
}
