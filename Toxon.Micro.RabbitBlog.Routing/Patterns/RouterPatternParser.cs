using System;
using System.Linq;

namespace Toxon.Micro.RabbitBlog.Routing.Patterns
{
    public class RouterPatternParser
    {
        public static IRequestMatcher Parse(string pattern)
        {
            var sections = pattern.Split(',');
            var matchers = sections.Select(section =>
            {
                var parts = section.Split(new[] {':'}, 2);

                var key = parts[0].Trim();
                var value = parts[1].Trim();

                IValueMatcher valueMatcher;
                if (value == "*")
                    valueMatcher = new AnyValueMatcher();
                else if (value.Contains("*"))
                    // TODO yeah...
                    valueMatcher = new RegexValueMatcher(value.Replace("*", ".*"));
                else
                    valueMatcher = new EqualityValueMatcher(value);

                return (IRequestMatcher)new FieldMatcher(key, valueMatcher);
            }).ToArray();

            return matchers.Length == 1 ? matchers.Single() : new AndMatcher(matchers);
        }

        public static string UnParse(IRequestMatcher matcher)
        {
            switch (matcher)
            {
                case AndMatcher andMatcher:
                    return string.Join(",", andMatcher.RequestMatchers.Select(UnParse));
                case FieldMatcher fieldMatcher:
                    return $"{fieldMatcher.FieldName}:{UnParse(fieldMatcher.FieldValue)}";

                default: throw new ArgumentOutOfRangeException(nameof(matcher));
            }
        }

        private static string UnParse(IValueMatcher matcher)
        {
            switch (matcher)
            {
                case AnyValueMatcher _:
                    return "*";
                case EqualityValueMatcher equalityValueMatcher:
                    return equalityValueMatcher.MatchValue.ToString();
                case RegexValueMatcher _:
                    throw new NotImplementedException();

                default: throw new ArgumentOutOfRangeException(nameof(matcher));
            }
        }
    }
}
