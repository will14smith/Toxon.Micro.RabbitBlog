namespace Toxon.Micro.RabbitBlog.Front.Http
{
    internal class SearchResult
    {
        public decimal Score { get; set; }
        public Entry Document { get; set; }
    }
}
