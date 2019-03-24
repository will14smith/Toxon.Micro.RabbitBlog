using System;
using System.Collections.Generic;
using System.Linq;
using Toxon.Micro.RabbitBlog.Core.Patterns;

namespace Toxon.Micro.RabbitBlog.Router.Routing
{
    internal class MatchingRoutesSelectionStrategy<TData> : IRouteSelectionStrategy<TData>
    {
        public IReadOnlyCollection<Router<TData>.Entry> Select(IReadOnlyCollection<Router<TData>.Entry> entries, IReadOnlyDictionary<string, object> message)
        {
            return entries.Where(route => Matches(route.Route, message)).ToList();
        }

        private bool Matches(IRequestMatcher route, IReadOnlyDictionary<string, object> message)
        {
            switch (route)
            {
                case AndMatcher andMatcher:
                    return andMatcher.RequestMatchers.All(matcher => Matches(matcher, message));

                case FieldMatcher fieldMatcher:
                    if (!(message.TryGetValue(fieldMatcher.FieldName, out var fieldValue)))
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
                    if (!(value is string) && equalityValueMatcher.MatchValue is string stringValue)
                    {
                        return string.Equals(value?.ToString(), stringValue, StringComparison.OrdinalIgnoreCase);
                    }
                    return Equals(value, equalityValueMatcher.MatchValue);

                case RegexValueMatcher regexValueMatcher:
                    throw new NotImplementedException();

                default: throw new ArgumentOutOfRangeException(nameof(valueMatcher));
            }
        }
    }
}
