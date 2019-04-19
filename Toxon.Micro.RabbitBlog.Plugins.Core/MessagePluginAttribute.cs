using System;

namespace Toxon.Micro.RabbitBlog.Plugins.Core
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class MessagePluginAttribute : Attribute
    {
        public MessagePluginAttribute(string serviceKey)
        {
            ServiceKey = serviceKey;
        }

        public string ServiceKey { get; }
    }
}
