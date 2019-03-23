using System.Collections.Generic;

namespace Toxon.Micro.RabbitBlog.Front.Outbound
{
    public class SearchDocument
    {
        public string Kind { get; set; }
        public int Id { get; set; }

        public Dictionary<string, string> Fields { get; set; }
    }
}