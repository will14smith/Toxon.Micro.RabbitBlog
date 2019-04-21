using System.Threading.Tasks;
using Amazon.Lambda.SQSEvents;
using Newtonsoft.Json;

namespace Toxon.Micro.RabbitBlog.Serverless.Host
{
    public class LambdaFunction
    {
        private readonly LocalModel _handler;

        public LambdaFunction()
        {
            var sender = LambdaConfig.CreateSender();
            _handler = LambdaConfig.CreateHandler(sender);
        }

        public async Task<MessageModel> HandleDirectAsync(MessageModel requestModel)
        {
            var request = requestModel.ToMessage();

            var response = await _handler.CallAsync(request);

            return new MessageModel(response);
        }

        public async Task HandleQueueAsync(SQSEvent message)
        {
            foreach (var record in message.Records)
            {
                var requestModel = JsonConvert.DeserializeObject<MessageModel>(record.Body);
                var request = requestModel.ToMessage();

                await _handler.SendAsync(request);
            }
        }
    }
}
