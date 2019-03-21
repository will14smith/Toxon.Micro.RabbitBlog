namespace Toxon.Micro.RabbitBlog.Front.Outbound
{
    internal class ListEntriesFromStore
    {
        public string Store => "list";
        public string Kind => "entry";

        public string User { get; set; }
    }
}
