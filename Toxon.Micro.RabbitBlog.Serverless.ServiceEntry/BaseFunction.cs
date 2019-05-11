using System;
using System.Threading.Tasks;
using Amazon.Lambda.SQSEvents;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using Newtonsoft.Json;
using Toxon.Micro.RabbitBlog.Serverless.Core;

namespace Toxon.Micro.RabbitBlog.Serverless.ServiceEntry
{
    public abstract class BaseFunction
    {
        private readonly LocalModel _handler;

        protected BaseFunction()
        {
            AWSSDKHandler.RegisterXRayForAllServices();

            _handler = CreateHandler();
        }

        protected abstract LocalModel CreateHandler();

        public async Task<MessageModel> HandleDirectAsync(MessageModel requestModel)
        {
            var request = requestModel.ToMessage();
            var response = await _handler.CallAsync(request);

            return new MessageModel(response);
        }

        public async Task HandleQueueAsync(SQSEvent message)
        {
            Console.WriteLine("HandleQueueAsync");

            foreach (var record in message.Records)
            {
                var requestModel = JsonConvert.DeserializeObject<MessageModel>(record.Body);
                var request = requestModel.ToMessage();

                await _handler.SendAsync(request);
            }
            Console.WriteLine("HandleQueueAsync done");
        }

    }
}
