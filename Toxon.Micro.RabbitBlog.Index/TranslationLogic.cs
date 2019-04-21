using System.Collections.Generic;
using System.Threading.Tasks;
using Toxon.Micro.RabbitBlog.Index.Inbound;
using Toxon.Micro.RabbitBlog.Index.Translation;
using Toxon.Micro.RabbitBlog.Plugins.Core;
using Toxon.Micro.RabbitBlog.Routing;
using Toxon.Micro.RabbitBlog.Routing.Json;

namespace Toxon.Micro.RabbitBlog.Index
{
    [ServicePlugin("index-translator.v1")]
    internal class TranslationLogic
    {
        private readonly IRoutingSender _sender;

        public TranslationLogic(IRoutingSender sender)
        {
            _sender = sender;
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

            return _sender.SendAsync(translatedRequest);
        }
    }
}
