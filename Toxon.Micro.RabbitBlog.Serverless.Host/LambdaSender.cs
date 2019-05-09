using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.XRay;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Context;
using Newtonsoft.Json;
using Toxon.Micro.RabbitBlog.Routing;
using JsonSerializer = Amazon.Lambda.Serialization.Json.JsonSerializer;
using Message = Toxon.Micro.RabbitBlog.Core.Message;

namespace Toxon.Micro.RabbitBlog.Serverless.Host
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

            var records = AWSXRayRecorder.Instance;

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

            return _jsonSerializer
                .Deserialize<MessageModel>(response.Payload)
                .ToMessage();
        }

        private async Task<RouteResponse> GetRouteAsync(MessageModel requestModel, CancellationToken cancellationToken)
        {
            var response = await _lambda.InvokeAsync(new InvokeRequest
            {
                FunctionName = _routerFunctionName,
                InvocationType = InvocationType.RequestResponse,
                Payload = JsonConvert.SerializeObject(requestModel),
            }, cancellationToken);

            return _jsonSerializer
                .Deserialize<RouteResponse>(response.Payload);
        }
    }

    internal class RouteResponse
    {
        public string Target { get; set; }
    }
}