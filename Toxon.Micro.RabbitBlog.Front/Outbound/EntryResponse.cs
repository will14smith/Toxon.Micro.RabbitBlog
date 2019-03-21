namespace Toxon.Micro.RabbitBlog.Front.Outbound
{
    internal class EntryResponse
    {
        public int Id { get; set; }
        public string User { get; set; }
        public string Text { get; set; }
    }
}
