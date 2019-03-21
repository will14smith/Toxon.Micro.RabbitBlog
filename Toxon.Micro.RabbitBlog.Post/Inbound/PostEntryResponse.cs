namespace Toxon.Micro.RabbitBlog.Post.Inbound
{
    internal class PostEntryResponse
    {
        public int Id { get; set; }

        public string User { get; set; }
        public string Text { get; set; }
    }
}
