using System;
using System.Threading.Tasks;
using Amazon.Lambda.SQSEvents;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using Newtonsoft.Json;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.EntryStore.Inbound;
using Toxon.Micro.RabbitBlog.Routing;
using Toxon.Micro.RabbitBlog.Routing.Json;
using Toxon.Micro.RabbitBlog.Serverless.Core;

namespace Toxon.Micro.RabbitBlog.Serverless.ServiceEntry
{
    public abstract class BaseFunction
    {
        private readonly LocalModel _handler;

        protected BaseFunction()
        {
            AWSSDKHandler.RegisterXRayForAllServices();

            _handler = CreateHandler(LambdaConfig.CreateSender());
        }

        protected abstract LocalModel CreateHandler(IRoutingSender sender);

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

        protected async Task<Message> JsonHandler<TRequest, TResponse>(Message requestMessage, Func<TRequest, Task<TResponse>> handler)
        {
            var request = JsonMessage.Read<TRequest>(requestMessage);

            var response = await handler(request);
            var responseMessage = JsonMessage.Write(response);

            return responseMessage;
        }
        protected async Task JsonHandler<TRequest>(Message requestMessage, Func<TRequest, Task> handler)
        {
            var request = JsonMessage.Read<TRequest>(requestMessage);

            await handler(request);
        }
    }
}
