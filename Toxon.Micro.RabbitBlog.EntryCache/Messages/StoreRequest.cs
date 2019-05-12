namespace Toxon.Micro.RabbitBlog.EntryCache.Messages
{
    public class StoreRequest
    {
        public string Store { get; set; }
        public string Kind => "entry";

        // TODO cache shouldn't care about the message data
        public string User { get; set; }
        public string Text { get; set; }
    }

    internal class UncachedStoreRequest : StoreRequest
    {
        public bool Cache => true;
    }
}
