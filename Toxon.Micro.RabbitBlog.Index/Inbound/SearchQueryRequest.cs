namespace Toxon.Micro.RabbitBlog.Index.Inbound
{
    public class SearchQueryRequest
    {
        public string Search => "query";

        public string Kind { get; set; }
        public string Query { get; set; }
    }
}
