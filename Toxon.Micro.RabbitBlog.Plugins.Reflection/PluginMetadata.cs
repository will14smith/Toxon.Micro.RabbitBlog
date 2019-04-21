using System;
using Toxon.Micro.RabbitBlog.Plugins.Core;

namespace Toxon.Micro.RabbitBlog.Plugins.Reflection
{
    public class PluginMetadata
    {
        public PluginMetadata(string serviceKey, ServiceType serviceType, Type type)
        {
            ServiceKey = serviceKey;
            ServiceType = serviceType;
            Type = type;
        }

        public string ServiceKey { get; }
        public ServiceType ServiceType { get; }
        public Type Type { get; }
    }
}