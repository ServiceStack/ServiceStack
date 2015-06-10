using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack
{
    public class ServerEventConnect : ServerEventJoin
    {
        public string Id { get; set; }
        public string UnRegisterUrl { get; set; }
        public string HeartbeatUrl { get; set; }
        public long HeartbeatIntervalMs { get; set; }
        public long IdleTimeoutMs { get; set; }
    }

    public class ServerEventJoin : ServerEventCommand
    {
        public string UserId { get; set; }
        public string DisplayName { get; set; }
        public string ProfileUrl { get; set; }
    }

    public class ServerEventLeave : ServerEventCommand {}

    public class ServerEventCommand : ServerEventMessage { }

    public class ServerEventHeartbeat : ServerEventCommand { }

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

    public partial class ServerEventsClient : IDisposable
    {
        private static ILog log = LogManager.GetLogger(typeof(ServerEventsClient));

        public static int BufferSize = 1024 * 64;
        static int DefaultHeartbeatMs = 10 * 1000;
        static int DefaultIdleTimeoutMs = 30 * 1000;
        private bool stopped = true;

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

        public string EventStreamUri { get; set; }
        public string[] Channels { get; set; }
        public IServiceClient ServiceClient { get; set; }
        public DateTime LastPulseAt { get; set; }

        public Action<ServerEventConnect> OnConnect;
        public Action<ServerEventMessage> OnCommand;
        public Action<ServerEventMessage> OnMessage;
        public Action OnHeartbeat;
        public Action<Exception> OnException;

        public Action<WebRequest> EventStreamRequestFilter { get; set; }
        public Action<WebRequest> HeartbeatRequestFilter { get; set; } 

        public static readonly Task<object> EmptyTask;

        static ServerEventsClient()
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetResult(null);
            EmptyTask = tcs.Task;
        }

        public ServerEventsClient(string baseUri, params string[] channels)
        {
            this.EventStreamUri = baseUri.CombineWith("event-stream");
            this.Channels = channels;

            if (Channels != null && Channels.Length > 0)
                this.EventStreamUri = this.EventStreamUri
                    .AddQueryParam("channel", string.Join(",", Channels));

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
            httpReq.CookieContainer = ((ServiceClientBase)ServiceClient).CookieContainer; //share auth cookies
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

            Contract.Assert(!connectTcs.Task.IsCompleted);
            return connectTcs.Task;
        }

        private TaskCompletionSource<ServerEventCommand> commandTcs;
        public Task<ServerEventCommand> WaitForNextCommand()
        {
            Contract.Assert(!commandTcs.Task.IsCompleted);
            return commandTcs.Task;
        }

        private TaskCompletionSource<ServerEventHeartbeat> heartbeatTcs;
        public Task<ServerEventHeartbeat> WaitForNextHeartbeat()
        {
            Contract.Assert(!heartbeatTcs.Task.IsCompleted);
            return heartbeatTcs.Task;
        }

        private TaskCompletionSource<ServerEventMessage> messageTcs;
        public Task<ServerEventMessage> WaitForNextMessage()
        {
            Contract.Assert(!messageTcs.Task.IsCompleted);
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

            heartbeatTimer = PclExportClient.Instance.CreateTimer(Heartbeat,
                TimeSpan.FromMilliseconds(ConnectionInfo.HeartbeatIntervalMs), this);
        }

        protected void Heartbeat(object state)
        {
            if (log.IsDebugEnabled)
                log.DebugFormat("[SSE-CLIENT] Prep for Heartbeat...");

            if (cancel.IsCancellationRequested)
                return;

            var elapsedMs = (DateTime.UtcNow - LastPulseAt).TotalMilliseconds;
            if (elapsedMs > ConnectionInfo.IdleTimeoutMs)
            {
                OnExceptionReceived(new TimeoutException("Last Heartbeat Pulse was {0}ms ago".Fmt(elapsedMs)));
                return;
            }

            EnsureSynchronizationContext();

            if (ConnectionInfo == null)
                return;

            ConnectionInfo.HeartbeatUrl.GetStringFromUrlAsync(requestFilter:HeartbeatRequestFilter)
                .Success(t => {
                    if (cancel.IsCancellationRequested)
                        return;

                    if (log.IsDebugEnabled)
                        log.DebugFormat("[SSE-CLIENT] Heartbeat sent to: " + ConnectionInfo.HeartbeatUrl);

                    StartNewHeartbeat();
                })
                .Error(ex => {
                    if (cancel.IsCancellationRequested)
                        return;

                    if (log.IsDebugEnabled)
                        log.DebugFormat("[SSE-CLIENT] Error from Heartbeat: {0}", ex.UnwrapIfSingleException().Message);
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
                log.DebugFormat("[SSE-CLIENT] OnCommandReceived: ({0}) #{1} on #{2} ({3})", e.GetType().Name, e.EventId, ConnectionDisplayName, string.Join(", ", Channels));

            var hold = commandTcs;
            commandTcs = new TaskCompletionSource<ServerEventCommand>();

            if (OnCommand != null)
                OnCommand(e);

            hold.SetResult(e);
        }

        protected void OnHeartbeatReceived(ServerEventHeartbeat e)
        {
            if (log.IsDebugEnabled)
                log.DebugFormat("[SSE-CLIENT] OnHeartbeatReceived: ({0}) #{1} on #{2} ({3})", e.GetType().Name, e.EventId, ConnectionDisplayName, string.Join(", ", Channels));

            var hold = heartbeatTcs;
            heartbeatTcs = new TaskCompletionSource<ServerEventHeartbeat>();

            if (OnHeartbeat != null)
                OnHeartbeat();

            hold.SetResult(e);
        }

        protected void OnMessageReceived(ServerEventMessage e)
        {
            if (log.IsDebugEnabled)
                log.DebugFormat("[SSE-CLIENT] OnMessageReceived: {0} on #{1} ({2})", e.EventId, ConnectionDisplayName, string.Join(", ", Channels));

            var hold = messageTcs;
            messageTcs = new TaskCompletionSource<ServerEventMessage>();

            if (OnMessage != null)
                OnMessage(e);

            hold.SetResult(e);
        }

        private int errorsCount;
        protected void OnExceptionReceived(Exception ex)
        {
            errorsCount++;

            ex = ex.UnwrapIfSingleException();
            log.Error("[SSE-CLIENT] OnExceptionReceived: {0} on #{1}".Fmt(ex.Message, ConnectionDisplayName), ex);

            if (OnException != null)
                OnException(ex);

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
                log.Error("[SSE-CLIENT] Error whilst restarting: {0}".Fmt(ex.Message), ex);
            }
        }

        readonly Random rand = new Random(Environment.TickCount);
        private Task SleepBackOffMultiplier(int continuousErrorsCount)
        {
            if (continuousErrorsCount <= 1) 
                return EmptyTask;

            const int MaxSleepMs = 60 * 1000;

            //exponential/random retry back-off.
            var nextTry = Math.Min(
                rand.Next((int)Math.Pow(continuousErrorsCount, 3), (int)Math.Pow(continuousErrorsCount + 1, 3) + 1),
                MaxSleepMs);

            if (log.IsDebugEnabled)
                log.Debug("Sleeping for {0}ms after {1} continuous errors".Fmt(nextTry, continuousErrorsCount));

            return PclExportClient.Instance.WaitAsync(nextTry);
        }

        private string overflowText = "";
        public void ProcessResponse(Stream stream)
        {
            if (!stream.CanRead) return;

            var task = stream.ReadAsync(buffer, 0, 2048, cancel.Token);
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
                        log.DebugFormat("Connection ended on {0}", ConnectionDisplayName);

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
                    throw new ArgumentException("Invalid Selector '{0}'".Fmt(e.Selector));

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
                if (receiver != null)
                {
                    receiver(this, e);
                }
            }

            OnMessageReceived(e);
        }

        private void ProcessOnConnectMessage(ServerEventMessage e)
        {
            var msg = JsonServiceClient.ParseObject(e.Json);
            ConnectionInfo = new ServerEventConnect {
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
            ConnectionInfo.ProfileUrl = msg.Get("profileUrl");

            OnConnectReceived();
        }

        private void ProcessOnJoinMessage(ServerEventMessage e)
        {
            var msg = JsonServiceClient.ParseObject(e.Json);
            var joinMsg = new ServerEventJoin().Populate(e, msg);
            joinMsg.UserId = msg.Get("userId");
            joinMsg.DisplayName = msg.Get("displayName");
            joinMsg.ProfileUrl = msg.Get("profileUrl");

            OnCommandReceived(joinMsg);
        }

        private void ProcessOnLeaveMessage(ServerEventMessage e)
        {
            var msg = JsonServiceClient.ParseObject(e.Json);
            var leaveMsg = new ServerEventLeave().Populate(e, msg);
            leaveMsg.Channel = msg.Get("channel");

            OnCommandReceived(leaveMsg);
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
                log.DebugFormat("Stop()");

            if (cancel != null)
                cancel.Cancel();

            Task task = EmptyTask;

            if (ConnectionInfo != null && ConnectionInfo.UnRegisterUrl != null)
            {
                EnsureSynchronizationContext();
                task = ConnectionInfo.UnRegisterUrl.GetStringFromUrlAsync();
                task.Error(ex => { /*ignore*/});
            }

            using (response)
            {
                response = null;
            }

            ConnectionInfo = null;
            httpReq = null;

            return task;
        }

        public void Dispose()
        {
            if (log.IsDebugEnabled)
                log.DebugFormat("Dispose()");

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

        public static T Populate<T>(this T dst, ServerEventMessage src, JsonObject msg) where T : ServerEventMessage
        {
            dst.EventId = src.EventId;
            dst.Data = src.Data;
            dst.Selector = src.Selector;
            dst.Channel = src.Channel;
            dst.Json = src.Json;
            dst.Op = src.Op;

            if (dst.Meta == null)
                dst.Meta = new Dictionary<string, string>();

            foreach (var entry in msg)
            {
                dst.Meta[entry.Key] = entry.Value;
            }

            return dst;
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