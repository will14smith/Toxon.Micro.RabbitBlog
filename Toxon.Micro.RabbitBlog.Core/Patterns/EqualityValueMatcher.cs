namespace Toxon.Micro.RabbitBlog.Core.Patterns
{
    public class EqualityValueMatcher : IValueMatcher
    {
        public object MatchValue { get; }

        public EqualityValueMatcher(object matchValue)
        {
            MatchValue = matchValue;
        }

        public override string ToString()
        {
            return MatchValue?.ToString() ?? "null";
        }
    }
}