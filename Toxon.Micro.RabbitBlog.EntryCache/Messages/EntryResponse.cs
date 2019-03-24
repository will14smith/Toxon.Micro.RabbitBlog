namespace Toxon.Micro.RabbitBlog.EntryCache.Messages
{
    // TODO cache shouldn't care about the message data
    internal class EntryResponse
    {
        public int Id { get; set; }

        public string User { get; set; }
        public string Text { get; set; }
    }
}
