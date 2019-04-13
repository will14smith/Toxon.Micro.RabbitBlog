using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.Routing;
using Toxon.Micro.RabbitBlog.Routing.Json;
using Toxon.Micro.RabbitBlog.Routing.Patterns;
using Toxon.Micro.RabbitBlog.Routing.RouteSelection;
using Toxon.Swim;
using Toxon.Swim.Membership;
using Toxon.Swim.Models;

namespace Toxon.Micro.RabbitBlog.Mesh
{
    public class RoutingModel : IRoutingModel
    {
        private const byte InboundType = 1;
        private const byte ResponseType = 2;

        private readonly ISwimBootstrapper _bootstrapper;

        private readonly SwimClient _swim;
        private readonly ConcurrentDictionary<SwimHost, ConcurrentDictionary<int, int>> _routerRegistrations = new ConcurrentDictionary<SwimHost, ConcurrentDictionary<int, int>>();

        private readonly IPEndPoint _serviceEndPoint;
        private UdpClient _udpClient;
        private Thread _listenerThread;
        private CancellationTokenSource _listenerThreadCancel;

        private int _handlerCounter;
        private readonly Dictionary<int, Func<Message, CancellationToken, Task>> _busHandlers = new Dictionary<int, Func<Message, CancellationToken, Task>>();
        private readonly Dictionary<int, Func<Message, CancellationToken, Task<Message>>> _rpcHandlers = new Dictionary<int, Func<Message, CancellationToken, Task<Message>>>();
        private int _responseCounter;
        private readonly ConcurrentDictionary<int, TaskCompletionSource<Message>> _responseHandlers = new ConcurrentDictionary<int, TaskCompletionSource<Message>>();

        private readonly Router<RoutingData> _router = new Router<RoutingData>(new CompositeRouteSelectionStrategy<RoutingData>(
            new MatchingRoutesSelectionStrategy<RoutingData>(),
            new TopScoringRoutesSelectionStrategy<RoutingData>(new RouteScoreComparer()),
            // TODO remove to allow broadcast messages?
            new RandomRouteSelectionStrategy<RoutingData>()
        ));

        public RoutingModel(string serviceKey, ISwimBootstrapper bootstrapper, RoutingModelOptions options)
        {
            _bootstrapper = bootstrapper;

            _serviceEndPoint = new IPEndPoint(options.IPAddress, FindFreeLocal(options.PortSelectionRange));
            var swimEndpoint = new IPEndPoint(options.IPAddress, FindFreeLocal(options.PortSelectionRange));

            _swim = new SwimClient(
                new SwimHost(swimEndpoint),
                new SwimMeta(new Dictionary<string, string> { { "key", serviceKey }, { "ip", _serviceEndPoint.Address.ToString() }, { "port", _serviceEndPoint.Port.ToString() } }),
                new SwimClientOptions()
            );
        }

        private static int FindFreeLocal((int Min, int Max) portSelectionRange)
        {
            var (min, max) = portSelectionRange;

            var r = new Random();

            var ttl = 100;
            while (ttl-- > 0)
            {
                var port = r.Next(min, max + 1);

                var activeListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners();
                if (activeListeners.All(x => x.Port != port))
                {
                    return port;
                }
            }

            throw new Exception("Failed to find a free port for SWIM client");
        }

        public async Task StartAsync()
        {
            _udpClient = new UdpClient(_serviceEndPoint);

            if (_listenerThread != null)
            {
                throw new InvalidOperationException();
            }

            _listenerThreadCancel = new CancellationTokenSource();
            _listenerThread = new Thread(ListenerThread);
            _listenerThread.Start(_listenerThreadCancel.Token);

            _swim.Members.OnJoined += OnMemberJoined;
            _swim.Members.OnUpdated += OnMemberUpdated;
            _swim.Members.OnLeft += OnMemberLeft;

            await _swim.StartAsync();
            await _swim.JoinAsync(_bootstrapper.GetWellKnownHosts());
        }

        public async Task StopAsync()
        {
            await _swim.LeaveAsync();

            _swim.Members.OnJoined -= OnMemberJoined;
            _swim.Members.OnUpdated -= OnMemberUpdated;
            _swim.Members.OnLeft -= OnMemberLeft;

            _listenerThreadCancel.Cancel();
            _listenerThread = null;
            _udpClient?.Dispose();
            _udpClient = null;
        }

        private void OnMemberJoined(object sender, MembershipChangedEventArgs args)
        {
            var host = args.Member.Host;
            var meta = args.Member.Meta;

            var registrations = _routerRegistrations.GetOrAdd(host, _ => new ConcurrentDictionary<int, int>());

            ClearRegistrations(registrations);
            AddMissingRegistrations(registrations, host, meta);
        }

        private void OnMemberUpdated(object sender, MembershipUpdatedEventArgs args)
        {
            var host = args.Member.Host;
            var meta = args.Member.Meta;

            var registrations = _routerRegistrations.GetOrAdd(args.Member.Host, _ => new ConcurrentDictionary<int, int>());

            RemoveInvalidRegistrations(registrations, meta);
            AddMissingRegistrations(registrations, host, meta);
        }

        private void OnMemberLeft(object sender, MemberLeftEventArgs args)
        {
            if (!_routerRegistrations.TryRemove(args.Member.Host, out var registrations))
            {
                return;
            }

            ClearRegistrations(registrations);
        }

        private void AddMissingRegistrations(ConcurrentDictionary<int, int> registrations, SwimHost host, SwimMeta meta)
        {
            var serviceKey = meta.Fields.TryGetValue("key", out var key) ? key : "";

            foreach (var field in meta.Fields)
            {
                if (!field.Key.StartsWith("route-"))
                {
                    continue;
                }

                var (routeId, route, execution, mode) = ReadRouteMeta(field.Value);
                registrations.AddOrUpdate(routeId, _ =>
                {
                    var data = new RoutingData(host, routeId, execution, mode);

                    return _router.Register(serviceKey, route, data);
                }, (_, existingValue) => existingValue);
                
            }
        }
        
        private void RemoveInvalidRegistrations(ConcurrentDictionary<int, int> registrations, SwimMeta meta)
        {
            foreach (var registration in registrations.ToDictionary(x => x.Key, x => x.Value))
            {
                if (!meta.Fields.ContainsKey($"route-{registration.Key}"))
                {
                    _router.Unregister(registration.Value);
                    registrations.TryRemove(registration.Key, out _);
                }
            }
        }

        private void ClearRegistrations(ConcurrentDictionary<int, int> registrations)
        {
            foreach(var registration in registrations)
            {
                _router.Unregister(registration.Value);
            }
        }

        public async Task SendAsync(Message message, CancellationToken cancellationToken = default)
        {
            // TODO broadcast to multiple?
            var route = _router.Match(message).Single();

            await SendMessageAsync(route.Data, message, -1);
        }

        public async Task<Message> CallAsync(Message message, CancellationToken cancellationToken = default)
        {
            var route = _router.Match(message).Single();

            var tcs = new TaskCompletionSource<Message>();
            using (cancellationToken.Register(() => tcs.SetCanceled()))
            {
                var responseId = Interlocked.Increment(ref _responseCounter);
                if (!_responseHandlers.TryAdd(responseId, tcs))
                {
                    throw new InvalidOperationException("Response id was reused?");
                }

                await SendMessageAsync(route.Data, message, responseId);

                return await tcs.Task;
            }
        }

        private async Task SendMessageAsync(RoutingData route, Message message, int responseId)
        {
            var member = _swim.Members.GetFromHost(route.Host);
            var memberServiceEndpoint = new IPEndPoint(IPAddress.Parse(member.Meta.Fields["ip"]), int.Parse(member.Meta.Fields["port"]));

            var buffer = new byte[1024];
            var offset = 0;

            offset += MessagePackBinary.WriteByte(ref buffer, offset, InboundType);
            offset += MessagePackBinary.WriteInt32(ref buffer, offset, route.RouteId);
            offset += MessagePackBinary.WriteInt32(ref buffer, offset, responseId);
            offset += WriteMessage(ref buffer, offset, message);

            await _udpClient.SendAsync(buffer, offset, memberServiceEndpoint);
        }

        public Task RegisterHandlerAsync(IRequestMatcher pattern, Func<Message, CancellationToken, Task> handler, RouteExecution execution = RouteExecution.Asynchronous, RouteMode mode = RouteMode.Observe, CancellationToken cancellationToken = default)
        {
            var id = Interlocked.Increment(ref _handlerCounter);
            _busHandlers.Add(id, handler);

            var newMeta = _swim.Members.Local.Meta.Fields.ToDictionary(x => x.Key, x => x.Value);
            newMeta.Add($"route-{id}", CreateRouteMeta(id, pattern, execution, mode));
            _swim.UpdateMeta(new SwimMeta(newMeta));

            return Task.CompletedTask;
        }

        public Task RegisterHandlerAsync(IRequestMatcher pattern, Func<Message, CancellationToken, Task<Message>> handler, RouteExecution execution = RouteExecution.Synchronous, RouteMode mode = RouteMode.Capture, CancellationToken cancellationToken = default)
        {
            var id = Interlocked.Increment(ref _handlerCounter);
            _rpcHandlers.Add(id, handler);

            var newMeta = _swim.Members.Local.Meta.Fields.ToDictionary(x => x.Key, x => x.Value);
            newMeta.Add($"route-{id}", CreateRouteMeta(id, pattern, execution, mode));
            _swim.UpdateMeta(new SwimMeta(newMeta));

            return Task.CompletedTask;
        }

        private async void ListenerThread(object arg)
        {
            var threadCancellationToken = (CancellationToken)arg;

            while (!threadCancellationToken.IsCancellationRequested)
            {
                UdpReceiveResult result;
                try
                {
                    result = await ReceiveAsync(threadCancellationToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.ConnectionReset)
                    {
                        continue;
                    }

                    throw;
                }

                HandleReceive(result, threadCancellationToken);
            }
        }

        private void HandleReceive(UdpReceiveResult result, CancellationToken cancellationToken)
        {
            var type = MessagePackBinary.ReadByte(result.Buffer, 0, out var offset);
            switch (type)
            {
                case InboundType:
                    HandleInbound(result, offset, cancellationToken);
                    break;
                case ResponseType:
                    HandleResponse(result, offset, cancellationToken);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        private void HandleInbound(UdpReceiveResult result, int offset, CancellationToken cancellationToken)
        {
            var handlerId = MessagePackBinary.ReadInt32(result.Buffer, offset, out var bytesRead);
            offset += bytesRead;

            var responseId = MessagePackBinary.ReadInt32(result.Buffer, offset, out bytesRead);
            offset += bytesRead;

            var (message, finalOffset) = ReadMessage(result.Buffer, offset);
            //if (finalOffset != result.Buffer.Length)
            //{
            //    throw new InvalidOperationException("Part of the buffer wasn't used");
            //}

            if (_busHandlers.TryGetValue(handlerId, out var busHandler))
            {
                if (responseId != -1)
                {
                    throw new InvalidOperationException("expecting a response to a bus handler");
                }

                Task.Run(async () => await busHandler(message, cancellationToken), cancellationToken).ConfigureAwait(false);
            }
            else if (_rpcHandlers.TryGetValue(handlerId, out var rpcHandler))
            {
                Task.Run(async () =>
                {
                    var response = await rpcHandler(message, cancellationToken);

                    var responseBuffer = new byte[1024];
                    var responseLength = 0;

                    responseLength += MessagePackBinary.WriteByte(ref responseBuffer, responseLength, ResponseType);
                    responseLength += MessagePackBinary.WriteInt32(ref responseBuffer, responseLength, responseId);
                    responseLength += WriteMessage(ref responseBuffer, responseLength, response);

                    await _udpClient.SendAsync(responseBuffer, responseLength, result.RemoteEndPoint);
                }, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                throw new InvalidOperationException($"Message handler was not found for handler id {handlerId}");
            }
        }

        private void HandleResponse(UdpReceiveResult result, int offset, CancellationToken cancellationToken)
        {
            var responseId = MessagePackBinary.ReadInt32(result.Buffer, offset, out var bytesRead);
            offset += bytesRead;

            var (message, finalOffset) = ReadMessage(result.Buffer, offset);
            //if (finalOffset != result.Buffer.Length)
            //{
            //    throw new InvalidOperationException("Part of the buffer wasn't used");
            //}

            if (!_responseHandlers.TryRemove(responseId, out var handler))
            {
                return;
            }

            handler.SetResult(message);
        }

        private static (Message Message, int offset) ReadMessage(byte[] buffer, int offset)
        {
            var headers = new Dictionary<string, byte[]>();
            var headerCount = MessagePackBinary.ReadMapHeader(buffer, offset, out var bytesRead);
            offset += bytesRead;
            for (var i = 0; i < headerCount; i++)
            {
                var key = MessagePackBinary.ReadString(buffer, offset, out bytesRead);
                offset += bytesRead;
                var value = MessagePackBinary.ReadBytes(buffer, offset, out bytesRead);
                offset += bytesRead;

                headers.Add(key, value);
            }

            var body = MessagePackBinary.ReadBytes(buffer, offset, out bytesRead);
            offset += bytesRead;

            return (new Message(body, headers), offset);
        }
        private static int WriteMessage(ref byte[] buffer, int offset, Message message)
        {
            offset += MessagePackBinary.WriteMapHeader(ref buffer, offset, message.Headers.Count);
            foreach (var header in message.Headers)
            {
                offset += MessagePackBinary.WriteString(ref buffer, offset, header.Key);
                offset += MessagePackBinary.WriteBytes(ref buffer, offset, header.Value);
            }

            offset += MessagePackBinary.WriteBytes(ref buffer, offset, message.Body);

            return offset;
        }

        private async Task<UdpReceiveResult> ReceiveAsync(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<UdpReceiveResult>();
            cancellationToken.Register(() => tcs.SetCanceled(), useSynchronizationContext: false);

            var receiveTask = _udpClient.ReceiveAsync();
            var cancellationTask = tcs.Task;

            var task = await Task.WhenAny(receiveTask, cancellationTask);

            if (task == cancellationTask)
            {
#pragma warning disable 4014
                receiveTask.ContinueWith(_ => receiveTask.Exception, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
#pragma warning restore 4014
            }

            return await task;
        }

        private static string CreateRouteMeta(int id, IRequestMatcher pattern, RouteExecution execution, RouteMode mode)
        {
            var meta = JsonMessage.Write(new RouteMeta
            {
                I = id,
                P = pattern,
                E = (int)execution,
                M = (int)mode
            }).Body;

            using (var mem = new MemoryStream())
            using (var gzip = new DeflateStream(mem, CompressionLevel.Optimal))
            {
                gzip.Write(meta, 0, meta.Length);
                gzip.Flush();

                return Convert.ToBase64String(mem.ToArray());
            }
        }
        private static (int Id, IRequestMatcher Pattern, RouteExecution Execution, RouteMode Mode) ReadRouteMeta(string raw)
        {
            var bytes = Convert.FromBase64String(raw);

            using (var input = new MemoryStream(bytes))
            using (var output = new MemoryStream())
            using (var gzip = new DeflateStream(input, CompressionMode.Decompress))
            {
                gzip.CopyTo(output);
                var meta = JsonMessage.Read<RouteMeta>(new Message(output.ToArray()));

                return (meta.I, meta.P, (RouteExecution)meta.E, (RouteMode)meta.M);
            }
        }

        private class RouteMeta
        {
            public int I { get; set; }
            public IRequestMatcher P { get; set; }
            public int E { get; set; }
            public int M { get; set; }
        }
    }
}
