using System.Threading.Tasks;
using Toxon.Micro.RabbitBlog.Plugins.Core;
using Toxon.Micro.RabbitBlog.Post.Inbound;
using Toxon.Micro.RabbitBlog.Post.Outbound;
using Toxon.Micro.RabbitBlog.Routing;
using Toxon.Micro.RabbitBlog.Routing.Json;

namespace Toxon.Micro.RabbitBlog.Post
{
    [ServicePlugin("post.v1")]
    internal class BusinessLogic
    {
        private readonly IRoutingSender _sender;

        public BusinessLogic(IRoutingSender sender)
        {
            _sender = sender;
        }

        [MessageRoute("post:entry")]
        public async Task<PostEntryResponse> HandlePostEntryAsync(PostEntryRequest message)
        {
            var saveResponse = await _sender.CallAsync<SaveEntryResponse>(new SaveEntryRequest
            {
                User = message.User,
                Text = message.Text,
            });

            await _sender.SendAsync(new InfoEntryBroadcast
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
