using System.Threading.Tasks;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.Core.Json;
using Toxon.Micro.RabbitBlog.Core.Routing;
using Toxon.Micro.RabbitBlog.Post.Inbound;
using Toxon.Micro.RabbitBlog.Post.Outbound;

namespace Toxon.Micro.RabbitBlog.Post
{
    internal class BusinessLogic
    {
        public async Task<PostEntryResponse> HandlePostEntryAsync(IRoutingModel model, PostEntryRequest message)
        {
            var saveResponse = await model.CallAsync<SaveEntryResponse>(new SaveEntryRequest
            {
                User = message.User,
                Text = message.Text,
            });

            await model.SendAsync(new InfoEntryBroadcast
            {
                Id = saveResponse.Id,

                User = message.User,
                Text = message.Text,
            });

            return new PostEntryResponse
            {
                Id = saveResponse.Id,

                User = message.User,
                Text = message.Text,
            };
        }
    }
}
