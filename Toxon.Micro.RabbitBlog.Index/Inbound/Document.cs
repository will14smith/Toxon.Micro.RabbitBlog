using System.Collections.Generic;

namespace Toxon.Micro.RabbitBlog.Index.Inbound
{
    public class Document
    {
        public string Kind { get; set; }
        public int Id { get; set; }

        public Dictionary<string, string> Fields { get; set; }
    }
}