using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Lambda.SQSEvents;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using Newtonsoft.Json;
using Toxon.Micro.RabbitBlog.Routing;
using Toxon.Micro.RabbitBlog.Routing.Json;
using Toxon.Micro.RabbitBlog.Routing.RouteSelection;

namespace Toxon.Micro.RabbitBlog.Serverless.Router
{
    public class RouterFunction
    {
        private readonly Router<RouterEntry> _router = new Router<RouterEntry>(new CompositeRouteSelectionStrategy<RouterEntry>(
            new MatchingRoutesSelectionStrategy<RouterEntry>(),
            new TopScoringRoutesSelectionStrategy<RouterEntry>(new RouteScoreComparer()),
            new RandomRouteSelectionStrategy<RouterEntry>()
        ));

        private readonly IAmazonLambda _lambda;
        private readonly IAmazonSQS _sqs;

        private readonly ConcurrentDictionary<string, string> _sqsQueueUrls = new ConcurrentDictionary<string, string>();

        public RouterFunction()
            : this(LoadRouteConfig())
        {
        }

        public RouterFunction(RouterConfig config)
        {
            AWSSDKHandler.RegisterXRayForAllServices();

            _lambda = new AmazonLambdaClient();
            _sqs = new AmazonSQSClient();

            foreach (var entry in config.Routes)
            {
                _router.Register(entry.ServiceKey, entry.Route, entry);
            }
        }

        private static RouterConfig LoadRouteConfig()
        {
            var path = "routes.json";
            var file = File.ReadAllText(path);

            return JsonConvert.DeserializeObject<RouterConfig>(file, JsonMessage.Settings);
        }

        public async Task<object> HandleDirectAsync(MessageModel request)
        {
            var route = _router.Match(request.ToMessage()).First();
            if (route.Data.TargetType != RouteTargetType.Lambda)
            {
                throw new InvalidOperationException("Cannot invoke queue with response");
            }

            return new
            {
                route.Data.ServiceKey,
                route.Data.Target,
                route.Data.TargetType,
            };
        }

        public async Task HandleQueueAsync(SQSEvent message)
        {
            foreach (var record in message.Records)
            {
                var request = JsonConvert.DeserializeObject<MessageModel>(record.Body);

                var routes = _router.Match(request.ToMessage());
                await SendToRoutesAsync(request, routes);
            }
        }

        private async Task SendToRoutesAsync(MessageModel request, IReadOnlyCollection<Router<RouterEntry>.Entry> routes)
        {
            foreach (var route in routes)
            {
                await SendToRouteAsync(request, route);
            }
        }

        private async Task SendToRouteAsync(MessageModel request, Router<RouterEntry>.Entry route)
        {
            switch (route.Data.TargetType)
            {
                case RouteTargetType.Sqs:
                    var queueUrl = await GetQueueUrl(route.Data.Target);
                    await _sqs.SendMessageAsync(new SendMessageRequest
                    {
                        QueueUrl = queueUrl,
                        MessageBody = JsonConvert.SerializeObject(request)
                    });
                    break;
                case RouteTargetType.Lambda:
                    await _lambda.InvokeAsync(new InvokeRequest
                    {
                        FunctionName = route.Data.Target,
                        InvocationType = InvocationType.Event,
                        Payload = JsonConvert.SerializeObject(request)
                    });
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async Task<string> GetQueueUrl(string queueName)
        {
            if (_sqsQueueUrls.TryGetValue(queueName, out var queueUrl))
            {
                return queueUrl;
            }

            var response = await _sqs.GetQueueUrlAsync(new GetQueueUrlRequest
            {
                QueueName = queueName
            });

            return _sqsQueueUrls.GetOrAdd(queueName, response.QueueUrl);
        }
    }
}
