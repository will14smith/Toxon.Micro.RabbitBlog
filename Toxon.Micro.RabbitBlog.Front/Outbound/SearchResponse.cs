using System.Collections.Generic;

namespace Toxon.Micro.RabbitBlog.Front.Outbound
{
    public class SearchResponse
    {
        public List<SearchResult> Results { get; set; }
    }
}