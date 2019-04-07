namespace Toxon.Micro.RabbitBlog.Routing.Patterns
{
    public class AnyValueMatcher : IValueMatcher
    {
        public override string ToString()
        {
            return "*";
        }
    }
}