using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Toxon.Micro.RabbitBlog.EntryStore.Inbound;
using Toxon.Micro.RabbitBlog.Plugins.Core;

namespace Toxon.Micro.RabbitBlog.EntryStore
{
    [ServicePlugin("entry-store.v2")]
    internal class BusinessLogic
    {
        private readonly List<EntryResponse> _entries = new List<EntryResponse>();

        [MessageRoute("store:*,kind:entry,cache:true")]
        [MessageRoute("store:*,kind:entry")]
        public async Task<object> HandleStoreAsync(StoreRequest message)
        {
            switch (message.Store)
            {
                case "list":
                    IEnumerable<EntryResponse> entries = _entries;
                    if (message.User != null)
                    {
                        entries = entries.Where(x => x.User == message.User);
                    }

                    return entries;

                case "save":
                    var entry = new EntryResponse
                    {
                        Id = _entries.Count + 1,

                        User = message.User,
                        Text = message.Text,
                    };
                    _entries.Add(entry);

                    return entry;

                default: throw new NotImplementedException();
            }
        }
    }
}
