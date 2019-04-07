namespace Toxon.Micro.RabbitBlog.Routing.Patterns
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