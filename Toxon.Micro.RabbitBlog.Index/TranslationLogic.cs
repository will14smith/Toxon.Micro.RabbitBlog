using System.Collections.Generic;
using System.Threading.Tasks;
using Toxon.Micro.RabbitBlog.Index.Inbound;
using Toxon.Micro.RabbitBlog.Index.Translation;
using Toxon.Micro.RabbitBlog.Plugins.Core;
using Toxon.Micro.RabbitBlog.Routing;
using Toxon.Micro.RabbitBlog.Routing.Json;

namespace Toxon.Micro.RabbitBlog.Index
{
    [MessagePlugin("index-translator.v1")]
    internal class TranslationLogic
    {
        private readonly IRoutingModel _model;

        public TranslationLogic(IRoutingModel model)
        {
            _model = model;
        }

        [MessageRoute("info:entry")]
        public Task HandlePostInfo(InfoEntryRequest request)
        {
            var translatedRequest = new SearchInsertRequest
            {
                Kind = "entry",
                Id = request.Id,

                Fields = new Dictionary<string, string>
                {
                    {"User", request.User},
                    {"Text", request.Text},
                }
            };

            return _model.SendAsync(translatedRequest);
        }
    }
}
