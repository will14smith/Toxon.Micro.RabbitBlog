namespace Toxon.Micro.RabbitBlog.Post.Outbound
{
    internal class SaveEntryRequest
    {
        public string Store => "save";
        public string Kind => "entry";

        public string User { get; set; }
        public string Text { get; set; }
    }
}
