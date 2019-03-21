using System.Collections.Generic;
using System.Linq;

namespace Toxon.Micro.RabbitBlog.Core.Patterns
{
    public class AndMatcher : IRequestMatcher
    {
        private readonly IReadOnlyCollection<IRequestMatcher> _requestMatchers;

        public AndMatcher(params IRequestMatcher[] requestMatchers)
        {
            _requestMatchers = requestMatchers;
        }

        public override string ToString()
        {
            return $"&& {string.Join(" ", _requestMatchers.Select(x => $"({x})"))}";
        }
    }
}
