namespace Toxon.Micro.RabbitBlog.Post.Inbound
{
    public class PostEntryResponse
    {
        public string Id { get; set; }

        public string User { get; set; }
        public string Text { get; set; }
    }
}
