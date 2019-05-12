namespace Toxon.Micro.RabbitBlog.Post.Inbound
{
    public class PostEntryRequest
    {
        public string Post => "entry";

        public string User { get; set; }
        public string Text { get; set; }
    }
}
