namespace Toxon.Micro.RabbitBlog.EntryStore.Inbound
{
    internal class StoreRequest
    {
        public string Store { get; set; }
        public string Kind => "entry";

        public string User { get; set; }
        public string Text { get; set; }
    }
}
