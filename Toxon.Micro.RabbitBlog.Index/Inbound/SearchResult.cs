namespace Toxon.Micro.RabbitBlog.Index.Inbound
{
    public class SearchResult
    {
        public decimal Score { get; set; }
        public Document Document { get; set; }
    }
}