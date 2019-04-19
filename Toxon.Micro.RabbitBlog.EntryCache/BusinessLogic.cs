using System.Collections.Generic;
using System.Threading.Tasks;
using Toxon.Micro.RabbitBlog.EntryCache.Messages;
using Toxon.Micro.RabbitBlog.Plugins.Core;
using Toxon.Micro.RabbitBlog.Routing;
using Toxon.Micro.RabbitBlog.Routing.Json;

namespace Toxon.Micro.RabbitBlog.EntryCache
{
    [MessagePlugin("entry-cache.v1")]
    internal class BusinessLogic
    {
        private readonly IRoutingModel _model;

        public BusinessLogic(IRoutingModel model)
        {
            _model = model;
        }

        [MessageRoute("store:*,kind:entry")]
        public async Task<object> HandleStoreAsync(StoreRequest request)
        {
            if (request.Store == "list")
            {
                return await _model.CallAsync<List<EntryResponse>>(new UncachedStoreRequest
                {
                    Store = request.Store,

                    User = request.User,
                    Text = request.Text,
                });
            }

            return await _model.CallAsync<EntryResponse>(new UncachedStoreRequest
            {
                Store = request.Store,

                User = request.User,
                Text = request.Text,
            });
        }
    }
}
