using System.Collections.Generic;

namespace Toxon.Micro.RabbitBlog.Index.Inbound
{
    public class SearchQueryResponse
    {
        public List<SearchResult> Results { get; set; }
    }
}