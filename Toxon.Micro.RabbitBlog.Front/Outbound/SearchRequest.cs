namespace Toxon.Micro.RabbitBlog.Front.Outbound
{
    public class SearchRequest
    {
        public string Search => "query";
        public string Kind { get; set; }
        public string Query { get; set; }
    }
}
