using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Core;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.Routing.Json;

namespace Toxon.Micro.RabbitBlog.Rabbit.Router
{
    public class ServiceHealthTracker
    {
        private readonly IRpcModel _rpc;
        private readonly Logger _logger;

        private readonly ConcurrentDictionary<string, ConcurrentQueue<string>> _nodes;
        private readonly Thread _healthThread;
        private readonly TimeSpan _healthThreadFrequency = TimeSpan.FromSeconds(5);
        private readonly TimeSpan _healthCheckTimeout = TimeSpan.FromMilliseconds(500);

        private long _nonce;

        public ServiceHealthTracker(IRpcModel rpc, Logger logger)
        {
            _rpc = rpc;
            _logger = logger;

            _nodes = new ConcurrentDictionary<string, ConcurrentQueue<string>>();
            _healthThread = new Thread(CheckHealthThread);
        }

        public Task<bool> RegisterAsync(string serviceKey, string healthEndpoint)
        {
            _nodes.GetOrAdd(serviceKey, _ => new ConcurrentQueue<string>())
                .Enqueue(healthEndpoint);

            return Task.FromResult(true);
        }

        public int GetServiceCount(string serviceKey)
        {
            return _nodes.TryGetValue(serviceKey, out var serviceNodes) ? serviceNodes.Count : 0;
        }

        public void Start()
        {
            _healthThread.Start();
        }

        private async void CheckHealthThread()
        {
            while (true)
            {
                await CheckHealth();
                Thread.Sleep(_healthThreadFrequency);
            }
        }

        private Task CheckHealth()
        {
            return Task.WhenAll(_nodes.Select(x => CheckHealth(x.Key, x.Value)));
        }
        private async Task CheckHealth(string serviceKey, ConcurrentQueue<string> nodes)
        {
            var count = nodes.Count;
            for (var i = 0; i < count; i++)
            {
                if (!nodes.TryPeek(out var healthEndpoint))
                {
                    break;
                }

                if (!await CheckHealth(serviceKey, healthEndpoint))
                {
                    // TODO this is only safe because this is the only consumer
                    // possibly "dequeue" above (maintain count?) and requeue if healthy
                    _logger.Information("Removing dead service {healthEndpoint} from {serviceKey}", healthEndpoint, serviceKey);
                    nodes.TryDequeue(out _);
                }
            }
        }

        private async Task<bool> CheckHealth(string serviceKey, string healthEndpoint)
        {
            var nonce = Interlocked.Increment(ref _nonce);

            var request = new HealthCheck { Health = "ping", Nonce = nonce };
            var requestMessage = JsonMessage.Write(request);

            var cts = new CancellationTokenSource();
            cts.CancelAfter(_healthCheckTimeout);

            try
            {
                var responseMessage = await _rpc.SendAsync(healthEndpoint, requestMessage, cts.Token);
                var response = JsonMessage.Read<HealthCheck>(responseMessage);

                return response.Health == "pong" && response.Nonce == nonce;
            }
            catch (TaskCanceledException)
            {
                return false;
            }
        }

        private class HealthCheck
        {
            public string Health { get; set; }
            public long Nonce { get; set; }
        }
    }
}
