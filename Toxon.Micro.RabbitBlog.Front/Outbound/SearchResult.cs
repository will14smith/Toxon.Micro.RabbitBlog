namespace Toxon.Micro.RabbitBlog.Front.Outbound
{
    public class SearchResult
    {
        public decimal Score { get; set; }
        public SearchDocument Document { get; set; }
    }
}