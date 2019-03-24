using System.Threading.Tasks;
using Toxon.Micro.RabbitBlog.Core.Json;
using Toxon.Micro.RabbitBlog.Core.Routing;
using Toxon.Micro.RabbitBlog.EntryCache.Messages;

namespace Toxon.Micro.RabbitBlog.EntryCache
{
    internal class BusinessLogic
    {
        private readonly IRoutingModel _model;

        public BusinessLogic(IRoutingModel model)
        {
            _model = model;
        }

        public Task<EntryResponse> HandleStoreAsync(StoreRequest request)
        {
            return _model.CallAsync<EntryResponse>(new UncachedStoreRequest
            {
                Store = request.Store,

                User = request.User,
                Text = request.Text,
            });
        }
    }
}
