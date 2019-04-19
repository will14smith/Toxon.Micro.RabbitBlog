using System;

namespace Toxon.Micro.RabbitBlog.Plugins.Core
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class PluginStartAttribute : Attribute
    {
    }
}
