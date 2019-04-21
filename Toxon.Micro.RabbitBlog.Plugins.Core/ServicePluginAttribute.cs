using System;

namespace Toxon.Micro.RabbitBlog.Plugins.Core
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ServicePluginAttribute : Attribute
    {
        public ServicePluginAttribute(string serviceKey, ServiceType type = ServiceType.MessageHandler)
        {
            ServiceKey = serviceKey;
            Type = type;
        }

        public string ServiceKey { get; }
        public ServiceType Type { get; }
    }
}
