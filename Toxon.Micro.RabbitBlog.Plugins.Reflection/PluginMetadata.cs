using System;

namespace Toxon.Micro.RabbitBlog.Plugins.Reflection
{
    public class PluginMetadata
    {
        public PluginMetadata(string serviceKey, Type type)
        {
            ServiceKey = serviceKey;
            Type = type;
        }

        public string ServiceKey { get; }
        public Type Type { get; }
    }
}