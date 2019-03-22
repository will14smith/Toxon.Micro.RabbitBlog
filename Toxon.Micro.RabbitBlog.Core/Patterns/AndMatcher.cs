using System.Collections.Generic;
using System.Linq;

namespace Toxon.Micro.RabbitBlog.Core.Patterns
{
    public class AndMatcher : IRequestMatcher
    {
        public IReadOnlyCollection<IRequestMatcher> RequestMatchers { get; }

        public AndMatcher(params IRequestMatcher[] requestMatchers)
        {
            RequestMatchers = requestMatchers;
        }

        public override string ToString()
        {
            return $"&& {string.Join(" ", RequestMatchers.Select(x => $"({x})"))}";
        }
    }
}
