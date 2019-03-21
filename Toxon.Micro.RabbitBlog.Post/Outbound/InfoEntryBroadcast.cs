namespace Toxon.Micro.RabbitBlog.Post.Outbound
{
    internal class InfoEntryBroadcast
    {
        public string Info => "entry";

        public int Id { get; set; }

        public string User { get; set; }
        public string Text { get; set; }
    }
}
