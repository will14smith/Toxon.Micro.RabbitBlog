using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;
using Toxon.Micro.RabbitBlog.Routing;
using Message = Toxon.Micro.RabbitBlog.Core.Message;

namespace Toxon.Micro.RabbitBlog.Serverless.Core
{
    public class LambdaSender : IRoutingSender
    {
        private readonly string _routerFunctionName;
        private readonly Lazy<Task<string>> _routerQueueUrl;

        private readonly IAmazonLambda _lambda;
        private readonly IAmazonSQS _sqs;

        private readonly JsonSerializer _jsonSerializer = new JsonSerializer();

        public LambdaSender(string routerQueueName, string routerFunctionName)
        {
            _routerFunctionName = routerFunctionName;

            _lambda = new AmazonLambdaClient();
            _sqs = new AmazonSQSClient();

            _routerQueueUrl = new Lazy<Task<string>>(() => Task.Run(async () =>
            {
                var response = await _sqs.GetQueueUrlAsync(routerQueueName);

                return response.QueueUrl;
            }));
        }

        public async Task SendAsync(Message message, CancellationToken cancellationToken = default)
        {
            var model = new MessageModel(message);

            await _sqs.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = await _routerQueueUrl.Value,
                MessageBody = JsonConvert.SerializeObject(model)
            }, cancellationToken);
        }

        public async Task<Message> CallAsync(Message request, CancellationToken cancellationToken = default)
        {
            var requestModel = new MessageModel(request);

            var route = await GetRouteAsync(requestModel, cancellationToken);

            var response = await _lambda.InvokeAsync(new InvokeRequest
            {
                FunctionName = route.Target,
                InvocationType = InvocationType.RequestResponse,
                Payload = JsonConvert.SerializeObject(request)
            }, cancellationToken);

            var responseModel = DeserializeStream<MessageModel>(response.Payload);
            return responseModel.ToMessage();
        }

        private async Task<RouteResponse> GetRouteAsync(MessageModel requestModel, CancellationToken cancellationToken)
        {
            var response = await _lambda.InvokeAsync(new InvokeRequest
            {
                FunctionName = _routerFunctionName,
                InvocationType = InvocationType.RequestResponse,
                Payload = JsonConvert.SerializeObject(requestModel),
            }, cancellationToken);

            return DeserializeStream<RouteResponse>(response.Payload);
        }


        private T DeserializeStream<T>(Stream stream)
        {
            using (var textReader = new StreamReader(stream))
            using (var reader = new JsonTextReader(textReader))
            {
                return _jsonSerializer.Deserialize<T>(reader);
            }
        }
    }

    internal class RouteResponse
    {
        public string Target { get; set; }
    }
}