namespace Toxon.Micro.RabbitBlog.Front.Outbound
{
    internal class SaveEntryToStore
    {
        public string Post => "entry";

        public string User { get; set; }
        public string Text { get; set; }
    }
}
