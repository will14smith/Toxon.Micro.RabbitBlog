namespace Toxon.Micro.RabbitBlog.Post.Inbound
{
    internal class PostEntryRequest
    {
        public string Post => "entry";

        public string User { get; set; }
        public string Text { get; set; }
    }
}
