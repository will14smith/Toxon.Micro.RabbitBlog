using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Toxon.Micro.RabbitBlog.Plugins.Core;

namespace Toxon.Micro.RabbitBlog.Plugins.Reflection
{
    public static class PluginDiscoverer
    {
        public static IReadOnlyCollection<PluginMetadata> Discover(IEnumerable<Assembly> assemblies)
        {
            return assemblies.SelectMany(Discover).ToList();
        }

        public static IReadOnlyCollection<PluginMetadata> Discover(Assembly assembly)
        {
            var types = assembly.DefinedTypes;

            return types.SelectMany(Discover).ToList();
        }
        
        private static IReadOnlyCollection<PluginMetadata> Discover(Type type)
        {
            return type.GetCustomAttributes<MessagePluginAttribute>()
                .Select(x => new PluginMetadata(x.ServiceKey, type))
                .ToList();
        }
    }
}
