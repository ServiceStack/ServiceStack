﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack
{
    public class ServerEventConnect : ServerEventCommand
    {
        public string Id { get; set; }
        public string UnRegisterUrl { get; set; }
        public string HeartbeatUrl { get; set; }
        public long HeartbeatIntervalMs { get; set; }
        public long IdleTimeoutMs { get; set; }
    }

    public class ServerEventJoin : ServerEventCommand { }

    public class ServerEventLeave : ServerEventCommand { }

    public class ServerEventUpdate : ServerEventCommand { }

    public class ServerEventHeartbeat : ServerEventCommand { }

    public class ServerEventCommand : ServerEventMessage
    {
        public string UserId { get; set; }
        public string DisplayName { get; set; }
        public string ProfileUrl { get; set; }
        public bool IsAuthenticated { get; set; }
        public string[] Channels { get; set; }
    }

    public class ServerEventMessage : IMeta
    {
        public long EventId { get; set; }
        public string Channel { get; set; }
        public string Data { get; set; }
        public string Selector { get; set; }
        public string Json { get; set; }
        public string Op { get; set; }
        public string Target { get; set; }
        public string CssSelector { get; set; }

        public Dictionary<string, string> Meta { get; set; }
    }

    public class ServerEventUser : IMeta
    {
        public string UserId { get; set; }
        public string DisplayName { get; set; }
        public string ProfileUrl { get; set; }
        public string[] Channels { get; set; }

        public Dictionary<string, string> Meta { get; set; }
    }

    public partial class ServerEventsClient : IDisposable
    {
        private static ILog log = LogManager.GetLogger(typeof(ServerEventsClient));

        public static int BufferSize = 1024 * 64;
        static int DefaultHeartbeatMs = 10 * 1000;
        static int DefaultIdleTimeoutMs = 30 * 1000;
        private bool stopped = true;
        public bool IsStopped
        {
            get { return stopped; }
        }

        byte[] buffer;
        Encoding encoding = new UTF8Encoding();

        HttpWebRequest httpReq;
        HttpWebResponse response;
        CancellationTokenSource cancel;
        private ITimer heartbeatTimer;

        public ServerEventConnect ConnectionInfo { get; private set; }

        public string SubscriptionId
        {
            get { return ConnectionInfo != null ? ConnectionInfo.Id : null; }
        }

        public string ConnectionDisplayName
        {
            get { return ConnectionInfo != null ? ConnectionInfo.DisplayName : "(not connected)"; }
        }

        private string eventStreamPath;
        public string EventStreamUri { get; private set; }

        public string BaseUri
        {
            get
            {
                var meta = this.ServiceClient as IServiceClientMeta;
                return meta != null ? meta.BaseUri : null;
            }
            set
            {
                this.eventStreamPath = value.CombineWith("event-stream");
                BuildEventStreamUri();

                var meta = this.ServiceClient as IServiceClientMeta;
                if (meta != null)
                    meta.BaseUri = value;
            }
        }

        private string[] channels;
        public string[] Channels
        {
            get { return channels; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("channels");

                this.channels = value;
                BuildEventStreamUri();
            }
        }

        private void BuildEventStreamUri()
        {
            this.EventStreamUri = this.eventStreamPath
                .AddQueryParam("channels", string.Join(",", this.channels));
        }

        public IServiceClient ServiceClient { get; set; }
        public DateTime LastPulseAt { get; set; }

        public Action<ServerEventConnect> OnConnect;
        public Action<ServerEventMessage> OnCommand;
        public Action<ServerEventMessage> OnMessage;
        public Action OnHeartbeat;
        public Action<Exception> OnException;

        public Action<WebRequest> EventStreamRequestFilter { get; set; }
        public Action<WebRequest> HeartbeatRequestFilter { get; set; }

        public ServerEventsClient(string baseUri, params string[] channels)
        {
            this.eventStreamPath = baseUri.CombineWith("event-stream");
            this.Channels = channels;

            this.ServiceClient = new JsonServiceClient(baseUri);

            this.Resolver = new NewInstanceResolver();
            this.ReceiverTypes = new List<Type>();
            this.Handlers = new Dictionary<string, ServerEventCallback>();
            this.NamedReceivers = new Dictionary<string, ServerEventCallback>();
        }

        public ServerEventsClient Start()
        {
            if (log.IsDebugEnabled)
                log.DebugFormat("Start()");

            stopped = false;
            httpReq = (HttpWebRequest)WebRequest.Create(EventStreamUri);
            //share auth cookies
            httpReq.CookieContainer = ((IHasCookieContainer)ServiceClient).CookieContainer;
            //httpReq.AllowReadStreamBuffering = false; //.NET v4.5

            if (EventStreamRequestFilter != null)
                EventStreamRequestFilter(httpReq);

            response = (HttpWebResponse)PclExport.Instance.GetResponse(httpReq);
            var stream = response.GetResponseStream();

            buffer = new byte[BufferSize];
            cancel = new CancellationTokenSource();

            //maintain existing tcs so reconnecting is transparent
            if (connectTcs == null || connectTcs.Task.IsCompleted)
                connectTcs = new TaskCompletionSource<ServerEventConnect>();
            if (commandTcs == null || commandTcs.Task.IsCompleted)
                commandTcs = new TaskCompletionSource<ServerEventCommand>();
            if (heartbeatTcs == null || heartbeatTcs.Task.IsCompleted)
                heartbeatTcs = new TaskCompletionSource<ServerEventHeartbeat>();
            if (messageTcs == null || messageTcs.Task.IsCompleted)
                messageTcs = new TaskCompletionSource<ServerEventMessage>();

            LastPulseAt = DateTime.UtcNow;
            if (log.IsDebugEnabled)
                log.Debug("[SSE-CLIENT] LastPulseAt: " + DateTime.UtcNow.TimeOfDay);

            ProcessResponse(stream);

            return this;
        }

        private TaskCompletionSource<ServerEventConnect> connectTcs;
        public Task<ServerEventConnect> Connect()
        {
            if (httpReq == null)
                Start();

            return connectTcs.Task;
        }

        private TaskCompletionSource<ServerEventCommand> commandTcs;
        public Task<ServerEventCommand> WaitForNextCommand()
        {
            return commandTcs.Task;
        }

        private TaskCompletionSource<ServerEventHeartbeat> heartbeatTcs;
        public Task<ServerEventHeartbeat> WaitForNextHeartbeat()
        {
            return heartbeatTcs.Task;
        }

        private TaskCompletionSource<ServerEventMessage> messageTcs;
        public Task<ServerEventMessage> WaitForNextMessage()
        {
            return messageTcs.Task;
        }
        
        protected void OnConnectReceived()
        {
            if (log.IsDebugEnabled)
                log.DebugFormat("[SSE-CLIENT] OnConnectReceived: {0} on #{1} / {2} on ({3})",
                    ConnectionInfo.EventId, ConnectionDisplayName, ConnectionInfo.Id, string.Join(", ", Channels));

            StartNewHeartbeat();

            var hold = connectTcs;
            connectTcs = new TaskCompletionSource<ServerEventConnect>();

            if (OnConnect != null)
                OnConnect(ConnectionInfo);

            hold.SetResult(ConnectionInfo); //needs to be at end or control yielded before Heartbeat can start
        }

        protected void StartNewHeartbeat()
        {
            if (ConnectionInfo == null || string.IsNullOrEmpty(ConnectionInfo.HeartbeatUrl))
                return;

            if (heartbeatTimer != null)
                heartbeatTimer.Cancel();

#if !NETSTANDARD1_1
            heartbeatTimer = PclExportClient.Instance.CreateTimer(Heartbeat,
                TimeSpan.FromMilliseconds(ConnectionInfo.HeartbeatIntervalMs), this);
#endif
        }

        protected void Heartbeat(object state)
        {
            if (log.IsDebugEnabled)
                log.DebugFormat("[SSE-CLIENT] Prep for Heartbeat...");

            if (cancel.IsCancellationRequested)
                return;

            if (ConnectionInfo == null)
                return;

            var elapsedMs = (DateTime.UtcNow - LastPulseAt).TotalMilliseconds;
            if (elapsedMs > ConnectionInfo.IdleTimeoutMs)
            {
                OnExceptionReceived(new TimeoutException($"Last Heartbeat Pulse was {elapsedMs}ms ago"));
                return;
            }

            EnsureSynchronizationContext();

            ConnectionInfo.HeartbeatUrl.GetStringFromUrlAsync(requestFilter: req =>
            {
                    var hold = httpReq;
                    if (hold != null)
                        req.CookieContainer = hold.CookieContainer;

                HeartbeatRequestFilter?.Invoke(req);
            })
                .Success(t =>
                {
                    if (cancel.IsCancellationRequested)
                        return;

                    if (log.IsDebugEnabled)
                        log.Debug("[SSE-CLIENT] Heartbeat sent to: " + ConnectionInfo.HeartbeatUrl);

                    StartNewHeartbeat();
                })
                .Error(ex =>
                {
                    if (cancel.IsCancellationRequested)
                        return;

                    if (log.IsDebugEnabled)
                        log.Debug("[SSE-CLIENT] Error from Heartbeat: " + ex.UnwrapIfSingleException().Message);
                    OnExceptionReceived(ex);
                });
        }

        private static void EnsureSynchronizationContext()
        {
            if (SynchronizationContext.Current != null) return;

            //Unit test runner
            //if (log.IsDebugEnabled)
            //    log.DebugFormat("[SSE-CLIENT] SynchronizationContext.Current == null");

            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
        }

        protected void OnCommandReceived(ServerEventCommand e)
        {
            if (log.IsDebugEnabled)
                log.Debug($"[SSE-CLIENT] OnCommandReceived: ({e.GetType().Name}) #{e.EventId} on #{ConnectionDisplayName} ({string.Join(", ", Channels)})");

            var hold = commandTcs;
            commandTcs = new TaskCompletionSource<ServerEventCommand>();

            OnCommand?.Invoke(e);

            hold.SetResult(e);
        }

        protected void OnHeartbeatReceived(ServerEventHeartbeat e)
        {
            if (log.IsDebugEnabled)
                log.Debug($"[SSE-CLIENT] OnHeartbeatReceived: ({e.GetType().Name}) #{e.EventId} on #{ConnectionDisplayName} ({string.Join(", ", Channels)})");

            var hold = heartbeatTcs;
            heartbeatTcs = new TaskCompletionSource<ServerEventHeartbeat>();

            OnHeartbeat?.Invoke();

            hold.SetResult(e);
        }

        protected void OnMessageReceived(ServerEventMessage e)
        {
            if (log.IsDebugEnabled)
                log.Debug($"[SSE-CLIENT] OnMessageReceived: {e.EventId} on #{ConnectionDisplayName} ({string.Join(", ", Channels)})");

            var hold = messageTcs;
            messageTcs = new TaskCompletionSource<ServerEventMessage>();

            OnMessage?.Invoke(e);

            hold.SetResult(e);
        }

        private int errorsCount;
        protected void OnExceptionReceived(Exception ex)
        {
            errorsCount++;

            ex = ex.UnwrapIfSingleException();
            log.Error($"[SSE-CLIENT] OnExceptionReceived: {ex.Message} on #{ConnectionDisplayName}", ex);

            OnException?.Invoke(ex);

            Restart();
        }

        public void Restart()
        {
            try
            {
                InternalStop();

                if (stopped)
                    return;

                SleepBackOffMultiplier(errorsCount)
                    .ContinueWith(t =>
                    {
                        if (stopped)
                            return;
                        try
                        {
                            Start();
                        }
                        catch (Exception ex)
                        {
                            OnExceptionReceived(ex);
                        }
                    });
            }
            catch (Exception ex)
            {
                log.Error($"[SSE-CLIENT] Error whilst restarting: {ex.Message}", ex);
            }
        }

        readonly Random rand = new Random(Environment.TickCount);
        private Task SleepBackOffMultiplier(int continuousErrorsCount)
        {
            if (continuousErrorsCount <= 1)
                return TypeConstants.EmptyTask;

            const int MaxSleepMs = 60 * 1000;

            //exponential/random retry back-off.
            var nextTry = Math.Min(
                rand.Next((int)Math.Pow(continuousErrorsCount, 3), (int)Math.Pow(continuousErrorsCount + 1, 3) + 1),
                MaxSleepMs);

            if (log.IsDebugEnabled)
                log.Debug($"Sleeping for {nextTry}ms after {continuousErrorsCount} continuous errors");

            return PclExportClient.Instance.WaitAsync(nextTry);
        }

        private string overflowText = "";
        public void ProcessResponse(Stream stream)
        {
            if (!stream.CanRead) return;

            var task = stream.ReadAsync(buffer, 0, BufferSize, cancel.Token);
            task.ContinueWith(t =>
            {
                if (cancel.IsCancellationRequested || t.IsCanceled)
                {
                    httpReq = null;
                    return;
                }

                if (t.IsFaulted)
                {
                    OnExceptionReceived(t.Exception);
                    httpReq = null;
                    return;
                }

                errorsCount = 0;

                int len = task.Result;
                if (len > 0)
                {
                    var text = overflowText + encoding.GetString(buffer, 0, len);
                    int pos;
                    while ((pos = text.IndexOf('\n')) >= 0)
                    {
                        if (pos == 0)
                        {
                            if (currentMsg != null)
                                ProcessEventMessage(currentMsg);
                            currentMsg = null;

                            text = text.Substring(pos + 1);

                            if (text.Length > 0)
                                continue;

                            break;
                        }

                        var line = text.Substring(0, pos);
                        if (!string.IsNullOrWhiteSpace(line))
                            ProcessLine(line);
                        if (text.Length > pos + 1)
                            text = text.Substring(pos + 1);
                    }

                    overflowText = text;

                    ProcessResponse(stream);
                }
                else
                {
                    if (log.IsDebugEnabled)
                        log.Debug($"Connection ended on {ConnectionDisplayName}");

                    Restart();
                }
            });
        }

        private ServerEventMessage currentMsg;
        void ProcessLine(string line)
        {
            if (line == null) return;

            if (currentMsg == null)
                currentMsg = new ServerEventMessage();

            var parts = line.SplitOnFirst(':');
            var label = parts[0];
            var data = parts[1];
            if (data.Length > 0 && data[0] == ' ')
                data = data.Substring(1);

            switch (label)
            {
                case "id":
                    currentMsg.EventId = long.Parse(data);
                    break;
                case "data":
                    currentMsg.Data = data;
                    break;
            }
        }

        void ProcessEventMessage(ServerEventMessage e)
        {
            var parts = e.Data.SplitOnFirst(' ');
            e.Selector = parts[0];
            var selParts = e.Selector.SplitOnFirst('@');
            if (selParts.Length > 1)
            {
                e.Channel = selParts[0];
                e.Selector = selParts[1];
            }

            e.Json = parts[1];

            if (!string.IsNullOrEmpty(e.Selector))
            {
                parts = e.Selector.SplitOnFirst('.');
                if (parts.Length < 2)
                    throw new ArgumentException($"Invalid Selector '{e.Selector}'");

                e.Op = parts[0];
                var target = parts[1].Replace("%20", " ");

                var tokens = target.SplitOnFirst('$');
                e.Target = tokens[0];
                if (tokens.Length > 1)
                    e.CssSelector = tokens[1];

                if (e.Op == "cmd")
                {
                    switch (e.Target)
                    {
                        case "onConnect":
                            ProcessOnConnectMessage(e);
                            return;
                        case "onJoin":
                            ProcessOnJoinMessage(e);
                            return;
                        case "onLeave":
                            ProcessOnLeaveMessage(e);
                            return;
                        case "onUpdate":
                            ProcessOnUpdateMessage(e);
                            return;
                        case "onHeartbeat":
                            ProcessOnHeartbeatMessage(e);
                            return;
                        default:
                            ServerEventCallback cb;
                            if (Handlers.TryGetValue(e.Target, out cb))
                            {
                                cb(this, e);
                            }
                            break;
                    }
                }

                ServerEventCallback receiver;
                NamedReceivers.TryGetValue(e.Op, out receiver);
                receiver?.Invoke(this, e);
            }

            OnMessageReceived(e);
        }

        private void ProcessOnConnectMessage(ServerEventMessage e)
        {
            var msg = JsonServiceClient.ParseObject(e.Json);
            ConnectionInfo = new ServerEventConnect
            {
                HeartbeatIntervalMs = DefaultHeartbeatMs,
                IdleTimeoutMs = DefaultIdleTimeoutMs,
            }.Populate(e, msg);

            ConnectionInfo.Id = msg.Get("id");
            ConnectionInfo.HeartbeatUrl = msg.Get("heartbeatUrl");
            ConnectionInfo.HeartbeatIntervalMs = msg.Get<long>("heartbeatIntervalMs");
            ConnectionInfo.IdleTimeoutMs = msg.Get<long>("idleTimeoutMs");
            ConnectionInfo.UnRegisterUrl = msg.Get("unRegisterUrl");
            ConnectionInfo.UserId = msg.Get("userId");
            ConnectionInfo.DisplayName = msg.Get("displayName");
            ConnectionInfo.IsAuthenticated = msg.Get("isAuthenticated") == "true";
            ConnectionInfo.ProfileUrl = msg.Get("profileUrl");

            OnConnectReceived();
        }

        private void ProcessOnJoinMessage(ServerEventMessage e)
        {
            var msg = new ServerEventJoin().Populate(e, JsonServiceClient.ParseObject(e.Json));
            OnCommandReceived(msg);
        }

        private void ProcessOnLeaveMessage(ServerEventMessage e)
        {
            var msg = new ServerEventLeave().Populate(e, JsonServiceClient.ParseObject(e.Json));
            OnCommandReceived(msg);
        }

        private void ProcessOnUpdateMessage(ServerEventMessage e)
        {
            var msg = new ServerEventUpdate().Populate(e, JsonServiceClient.ParseObject(e.Json));
            OnCommandReceived(msg);
        }

        private void ProcessOnHeartbeatMessage(ServerEventMessage e)
        {
            LastPulseAt = DateTime.UtcNow;
            if (log.IsDebugEnabled)
                log.Debug("[SSE-CLIENT] LastPulseAt: " + DateTime.UtcNow.TimeOfDay);

            var msg = JsonServiceClient.ParseObject(e.Json);
            var heartbeatMsg = new ServerEventHeartbeat().Populate(e, msg);

            OnHeartbeatReceived(heartbeatMsg);
        }

        public virtual Task Stop()
        {
            stopped = true;
            return InternalStop();
        }

        public virtual Task InternalStop()
        {
            if (log.IsDebugEnabled)
                log.Debug("Stop()");

            cancel?.Cancel();

            if (ConnectionInfo?.UnRegisterUrl != null)
            {
                EnsureSynchronizationContext();
                try {
                    ConnectionInfo.UnRegisterUrl.GetStringFromUrl();
                } catch (Exception ignore) {}
            }

            using (response)
            {
                response = null;
            }

            ConnectionInfo = null;
            httpReq = null;

            return TypeConstants.EmptyTask;
        }

        public void Update(string[] subscribe = null, string[] unsubscribe = null)
        {
            var snapshot = this.Channels.ToList();
            if (subscribe != null)
            {
                foreach (var channel in subscribe)
                {
                    snapshot.AddIfNotExists(channel);
                }
            }
            if (unsubscribe != null)
            {
                snapshot.RemoveAll(unsubscribe.Contains);
            }
            this.Channels = snapshot.ToArray();
        }

        public void Dispose()
        {
            if (log.IsDebugEnabled)
                log.Debug("Dispose()");

            Stop();
        }
    }

    public static class ServerEventClientExtensions
    {
#if !SL5
        public static AuthenticateResponse Authenticate(this ServerEventsClient client, Authenticate request)
        {
            return client.ServiceClient.Post(request);
        }
#endif
        public static Task<AuthenticateResponse> AuthenticateAsync(this ServerEventsClient client, Authenticate request)
        {
            return client.ServiceClient.PostAsync(request);
        }

        public static void UpdateSubscriber(this ServerEventsClient client, UpdateEventSubscriber request)
        {
            if (request.Id == null)
                request.Id = client.ConnectionInfo.Id;
            client.ServiceClient.Post(request);

            client.Update(subscribe:request.SubscribeChannels, unsubscribe:request.UnsubscribeChannels);
        }

        public static Task UpdateSubscriberAsync(this ServerEventsClient client, UpdateEventSubscriber request)
        {
            if (request.Id == null)
                request.Id = client.ConnectionInfo.Id;
            return client.ServiceClient.PostAsync(request)
                .Then(x => {
                    client.Update(subscribe:request.SubscribeChannels, unsubscribe:request.UnsubscribeChannels);
                    return null;
                });
        }

        public static void SubscribeToChannels(this ServerEventsClient client, params string[] channels)
        {
            client.ServiceClient.Post(new UpdateEventSubscriber { Id = client.ConnectionInfo.Id, SubscribeChannels = channels.ToArray() });
            client.Update(subscribe:channels);
        }

        public static Task SubscribeToChannelsAsync(this ServerEventsClient client, params string[] channels)
        {
            return client.ServiceClient.PostAsync(new UpdateEventSubscriber { Id = client.ConnectionInfo.Id, SubscribeChannels = channels.ToArray() })
                .Then(x => {
                    client.Update(subscribe:channels);
                    return null;
                });
        }

        public static void UnsubscribeFromChannels(this ServerEventsClient client, params string[] channels)
        {
            client.ServiceClient.Post(new UpdateEventSubscriber { Id = client.ConnectionInfo.Id, UnsubscribeChannels = channels.ToArray() });
            client.Update(unsubscribe:channels);
        }

        public static Task UnsubscribeFromChannelsAsync(this ServerEventsClient client, params string[] channels)
        {
            return client.ServiceClient.PostAsync(new UpdateEventSubscriber { Id = client.ConnectionInfo.Id, UnsubscribeChannels = channels.ToArray() })
                .Then(x => {
                    client.Update(unsubscribe:channels);
                    return null;
                });
        }

        public static List<ServerEventUser> GetChannelSubscribers(this ServerEventsClient client)
        {
            var response = client.ServiceClient.Get(new GetEventSubscribers { Channels = client.Channels });
            return response.Select(x => x.ToServerEventUser()).ToList();
        }

        public static Task<List<ServerEventUser>> GetChannelSubscribersAsync(this ServerEventsClient client)
        {
            var responseTask = client.ServiceClient.GetAsync(new GetEventSubscribers { Channels = client.Channels });
            return responseTask.ContinueWith(task => task.Result.Select(x => x.ToServerEventUser()).ToList());
        }

        internal static ServerEventUser ToServerEventUser(this Dictionary<string, string> map)
        {
            var channels = map.Get("channels");
            var to = new ServerEventUser
            {
                UserId = map.Get("userId"),
                DisplayName = map.Get("displayName"),
                ProfileUrl = map.Get("profileUrl"),
                Channels = !string.IsNullOrEmpty(channels) ? channels.Split(',') : null,
            };

            foreach (var entry in map)
            {
                if (entry.Key == "userId" || entry.Key == "displayName" || 
                    entry.Key == "profileUrl" || entry.Key == "channels")
                    continue;

                if (to.Meta == null)
                    to.Meta = new Dictionary<string, string>();

                to.Meta[entry.Key] = entry.Value;
            }

            return to;
        } 

        public static T Populate<T>(this T dst, ServerEventMessage src, Dictionary<string, string> msg) where T : ServerEventMessage
        {
            dst.EventId = src.EventId;
            dst.Data = src.Data;
            dst.Selector = src.Selector;
            dst.Channel = src.Channel;
            dst.Json = src.Json;
            dst.Op = src.Op;

            Populate(dst, msg);

            return dst;
        }

        private static void Populate<T>(this T dst, Dictionary<string, string> msg) where T : ServerEventMessage
        {
            if (dst.Meta == null)
                dst.Meta = new Dictionary<string, string>();

            foreach (var entry in msg)
            {
                dst.Meta[entry.Key] = entry.Value;
            }

            var cmd = dst as ServerEventCommand;
            if (cmd != null)
            {
                cmd.UserId = msg.Get("userId");
                cmd.DisplayName = msg.Get("displayName");
                cmd.IsAuthenticated = msg.Get("isAuthenticated") == "true";
                cmd.ProfileUrl = msg.Get("profileUrl");

                var channels = msg.Get("channels");
                if (!string.IsNullOrEmpty(channels))
                    cmd.Channels = channels.Split(',');
            }
        }

        public static ServerEventsClient RegisterHandlers(this ServerEventsClient client, Dictionary<string, ServerEventCallback> handlers)
        {
            foreach (var entry in handlers)
            {
                client.Handlers[entry.Key] = entry.Value;
            }
            return client;
        }
    }
}