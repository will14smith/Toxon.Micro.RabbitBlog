using System.Text.RegularExpressions;

namespace Toxon.Micro.RabbitBlog.Routing.Patterns
{
    public class RegexValueMatcher : IValueMatcher
    {
        public Regex Regex { get; }

        public RegexValueMatcher(string regex)
        {
            Regex = new Regex(regex);
        }

        public override string ToString()
        {
            return Regex.ToString();
        }
    }
}