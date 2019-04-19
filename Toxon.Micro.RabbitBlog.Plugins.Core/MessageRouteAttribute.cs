using System;
using Toxon.Micro.RabbitBlog.Routing.Patterns;

namespace Toxon.Micro.RabbitBlog.Plugins.Core
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class MessageRouteAttribute : Attribute
    {
        public MessageRouteAttribute(string route)
            : this(RouterPatternParser.Parse(route)) { }
        public MessageRouteAttribute(IRequestMatcher route)
        {
            Route = route;
        }

        public IRequestMatcher Route { get; }
    }
}