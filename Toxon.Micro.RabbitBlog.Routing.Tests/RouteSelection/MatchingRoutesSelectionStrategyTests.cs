using System.Linq;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.Routing.Json;
using Toxon.Micro.RabbitBlog.Routing.Patterns;
using Toxon.Micro.RabbitBlog.Routing.RouteSelection;
using Xunit;

namespace Toxon.Micro.RabbitBlog.Routing.Tests.RouteSelection
{
    public class MatchingRoutesSelectionStrategyTests
    {
        [Theory]

        [InlineData("a:1", "a:1")]
        [InlineData("a:1,b:1", "a:1")]
        [InlineData("a:1,b:1", "b:1")]
        [InlineData("a:1,b:1", "a:1,b:1")]
        public void Matching(string input, string pattern)
        {
            var router = new Router<int>(new MatchingRoutesSelectionStrategy<int>());
            router.Register("", RouterPatternParser.Parse(pattern), 1);

            var result = router.Match(CreateMessage(input));

            Assert.Single(result);
        }

        [Theory]

        [InlineData("a:1", "a:2")]
        [InlineData("a:1", "b:1")]
        [InlineData("a:1", "a:1,b:1")]
        [InlineData("a:1,b:1", "a:1,b:2")]
        public void NonMatching(string input, string pattern)
        {
            var router = new Router<int>(new MatchingRoutesSelectionStrategy<int>());
            router.Register("", RouterPatternParser.Parse(pattern), 1);

            var result = router.Match(CreateMessage(input));

            Assert.Empty(result);
        }

        private static Message CreateMessage(string input)
        {
            var data = input.Split(',')
                .Select(section => section.Split(new[] {':'}, 2))
                .ToDictionary(x => x[0].Trim(), x => x[1].Trim());

            return JsonMessage.Write(data);
        }
    }
}
