namespace Toxon.Micro.RabbitBlog.Post.Outbound
{
    internal class InfoEntryBroadcast
    {
        public string Info => "entry";

        public string Id { get; set; }

        public string User { get; set; }
        public string Text { get; set; }
    }
}
