using System.Collections.Generic;
using System.Threading.Tasks;
using Toxon.Micro.RabbitBlog.EntryCache.Messages;
using Toxon.Micro.RabbitBlog.Plugins.Core;
using Toxon.Micro.RabbitBlog.Routing;
using Toxon.Micro.RabbitBlog.Routing.Json;

namespace Toxon.Micro.RabbitBlog.EntryCache
{
    [ServicePlugin("entry-cache.v1")]
    internal class BusinessLogic
    {
        private readonly IRoutingSender _sender;

        public BusinessLogic(IRoutingSender sender)
        {
            _sender = sender;
        }

        [MessageRoute("store:*,kind:entry")]
        public async Task<object> HandleStoreAsync(StoreRequest request)
        {
            if (request.Store == "list")
            {
                return await _sender.CallAsync<List<EntryResponse>>(new UncachedStoreRequest
                {
                    Store = request.Store,

                    User = request.User,
                    Text = request.Text,
                });
            }

            return await _sender.CallAsync<EntryResponse>(new UncachedStoreRequest
            {
                Store = request.Store,

                User = request.User,
                Text = request.Text,
            });
        }
    }
}
