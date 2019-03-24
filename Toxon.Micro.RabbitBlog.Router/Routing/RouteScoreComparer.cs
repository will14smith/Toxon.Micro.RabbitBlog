using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Toxon.Micro.RabbitBlog.Core.Patterns;

namespace Toxon.Micro.RabbitBlog.Router.Routing
{
    internal class RouteScoreComparer : IComparer<IRequestMatcher>
    {
        private readonly IComparer<string> _keyComparer =  StringComparer.OrdinalIgnoreCase;

        public int Compare(IRequestMatcher x, IRequestMatcher y)
        {
            var xFields = GetFields(x);
            var yFields = GetFields(y);

            var specificityComparison = CompareSpecificity(xFields, yFields);
            if (specificityComparison != 0)
            {
                return specificityComparison;
            }

            if (xFields.Count == yFields.Count)
            {
                return CompareKeyOrder(xFields, yFields);
            }

            return xFields.Count - yFields.Count;
        }

        private static int CompareSpecificity(IEnumerable<FieldMatcher> x, IEnumerable<FieldMatcher> y)
        {
            var xExact = x.Count(f => f.FieldValue is EqualityValueMatcher);
            var yExact = y.Count(f => f.FieldValue is EqualityValueMatcher);

            return xExact - yExact;
        }

        private int CompareKeyOrder(IReadOnlyCollection<FieldMatcher> x, IReadOnlyCollection<FieldMatcher> y)
        {
            Debug.Assert(x.Count == y.Count);

            var xOrderedKeys = x.Select(f => f.FieldName).OrderBy(k => k).ToList();
            var yOrderedKeys = y.Select(f => f.FieldName).OrderBy(k => k).ToList();

            for (var i = 0; i < x.Count; i++)
            {
                var keyComparison = _keyComparer.Compare(xOrderedKeys[i], yOrderedKeys[i]);
                if (keyComparison != 0)
                {
                    return -keyComparison;
                }
            }

            return 0;
        }

        private IReadOnlyCollection<FieldMatcher> GetFields(IRequestMatcher matcher)
        {
            switch (matcher)
            {
                case AndMatcher andMatcher: return andMatcher.RequestMatchers.SelectMany(GetFields).ToList();
                case FieldMatcher fieldMatcher: return new[] { fieldMatcher };

                default: throw new ArgumentOutOfRangeException(nameof(matcher));
            }
        }
    }
}