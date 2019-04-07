using Toxon.Micro.RabbitBlog.Routing.Patterns;
using Toxon.Micro.RabbitBlog.Routing.RouteSelection;
using Xunit;

namespace Toxon.Micro.RabbitBlog.Routing.Tests.RouteSelection
{
    public class RouteScoreComparerTests
    {
        [Theory]

        [InlineData("a:1", "a:1", 0)]
        [InlineData("a:*", "a:*", 0)]
        [InlineData("a:1", "a:*", 1)]
        [InlineData("a:*", "a:1", -1)]
        [InlineData("a:1", "b:*", 1)]
        [InlineData("a:*", "b:1", -1)]
        [InlineData("a:1", "b:1", 1)]
        [InlineData("b:1", "a:1", -1)]
        [InlineData("a:1,b:1", "a:1,b:1", 0)]
        [InlineData("a:1,b:1", "a:1", 1)]
        [InlineData("a:1", "a:1,b:1", -1)]
        [InlineData("a:1,b:1", "a:*,b:1", 1)]
        [InlineData("a:*,b:1", "a:1,b:1", -1)]
        [InlineData("a:1,b:1", "a:1,b:*", 1)]
        [InlineData("a:1,b:*", "a:1,b:1", -1)]
        [InlineData("a:1,b:*", "a:*,b:1", 0)]
        [InlineData("b:1", "a:*,b:*", 1)]
        [InlineData("a:*,b:*", "b:1", -1)]
        [InlineData("a:1,b:1", "a:1,c:1", 1)]
        [InlineData("a:1,c:1", "a:1,b:1", -1)]
        public void Run(string left, string right, int expected)
        {
            var comparer = new RouteScoreComparer();

            var result = comparer.Compare(
                RouterPatternParser.Parse(left),
                RouterPatternParser.Parse(right)
            );

            Assert.Equal(expected, result);
        }
    }
}
