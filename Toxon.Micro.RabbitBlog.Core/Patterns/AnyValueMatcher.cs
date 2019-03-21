namespace Toxon.Micro.RabbitBlog.Core.Patterns
{
    public class AnyValueMatcher : IValueMatcher
    {
        public override string ToString()
        {
            return "*";
        }
    }
}