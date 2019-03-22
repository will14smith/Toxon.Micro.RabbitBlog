using System;
using System.Collections.Generic;
using System.Linq;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.Core.Json;
using Toxon.Micro.RabbitBlog.Core.Patterns;

namespace Toxon.Micro.RabbitBlog.Router
{
    internal class Router<TData>
    {
        private readonly List<Entry> _routes = new List<Entry>();

        public bool IsRegistered(string serviceKey, IRequestMatcher route)
        {
            return _routes.Any(x => x.ServiceKey == serviceKey && RequestMatcherEqualityComparer.Instance.Equals(x.Route, route));
        }

        public void Register(string serviceKey, IRequestMatcher route, TData data)
        {
            _routes.Add(new Entry(serviceKey, route, data));
        }

        public class Entry
        {
            public Entry(string serviceKey, IRequestMatcher route, TData data)
            {
                ServiceKey = serviceKey;
                Route = route;
                Data = data;
            }

            public string ServiceKey { get; }
            public IRequestMatcher Route { get; }
            public TData Data { get; }
        }

        public Entry Match(Message message)
        {
            var fields = JsonMessage.Read<Dictionary<string, object>>(message);

            fields = new Dictionary<string, object>(fields, StringComparer.InvariantCultureIgnoreCase);

            return _routes.Single(route => Matches(route.Route, fields));
        }

        private bool Matches(IRequestMatcher route, IReadOnlyDictionary<string, object> fields)
        {
            switch (route)
            {
                case AndMatcher andMatcher:
                    return andMatcher.RequestMatchers.All(matcher => Matches(matcher, fields));

                case FieldMatcher fieldMatcher:
                    if (!(fields.TryGetValue(fieldMatcher.FieldName, out var fieldValue)))
                    {
                        return false;
                    }

                    return Matches(fieldMatcher.FieldValue, fieldValue);

                default: throw new ArgumentOutOfRangeException(nameof(route));
            }
        }

        private bool Matches(IValueMatcher valueMatcher, object value)
        {
            switch (valueMatcher)
            {
                case AnyValueMatcher _:
                    return true;

                case EqualityValueMatcher equalityValueMatcher:
                    return Equals(value, equalityValueMatcher.MatchValue);

                case RegexValueMatcher regexValueMatcher:
                    throw new NotImplementedException();

                default: throw new ArgumentOutOfRangeException(nameof(valueMatcher));
            }
        }
    }

    internal class RequestMatcherEqualityComparer : IEqualityComparer<IRequestMatcher>
    {
        public static readonly RequestMatcherEqualityComparer Instance = new RequestMatcherEqualityComparer();

        public bool Equals(IRequestMatcher x, IRequestMatcher y)
        {
            // TODO yeah...
            return x?.ToString() == y?.ToString();
        }

        public int GetHashCode(IRequestMatcher obj)
        {
            return obj?.ToString()?.GetHashCode() ?? 0;
        }
    }
}
