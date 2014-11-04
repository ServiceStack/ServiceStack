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

    public class ServerEventMessage : IMeta
    {
        public long EventId { get; set; }
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

        public string EventStreamUri { get; set; }
        public string Channel { get; set; }
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

        public ServerEventsClient(string baseUri, string channel=null)
        {
            this.EventStreamUri = baseUri.CombineWith("event-stream");
            this.Channel = channel;

            if (Channel != null)
                this.EventStreamUri = this.EventStreamUri.AddQueryParam("channel", Channel);

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
            if (messageTcs == null || messageTcs.Task.IsCompleted)
                messageTcs = new TaskCompletionSource<ServerEventMessage>();

            LastPulseAt = DateTime.UtcNow;

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

        private TaskCompletionSource<ServerEventMessage> messageTcs;
        public Task<ServerEventMessage> WaitForNextMessage()
        {
            Contract.Assert(!messageTcs.Task.IsCompleted);
            return messageTcs.Task;
        }

        protected void OnConnectReceived()
        {
            if (log.IsDebugEnabled)
                log.DebugFormat("OnConnectReceived: {0} on #{1} / {2}", 
                    ConnectionInfo.EventId, ConnectionInfo.DisplayName, ConnectionInfo.Id);

            var hold = connectTcs;
            connectTcs = new TaskCompletionSource<ServerEventConnect>();

            if (OnConnect != null)
                OnConnect(ConnectionInfo);

            hold.SetResult(ConnectionInfo);

            StartNewHeartbeat();
        }

        private void StartNewHeartbeat()
        {
            if (string.IsNullOrEmpty(ConnectionInfo.HeartbeatUrl)) 
                return;

            if (heartbeatTimer != null)
                heartbeatTimer.Cancel();

            heartbeatTimer = PclExportClient.Instance.CreateTimer(Heartbeat,
                TimeSpan.FromMilliseconds(ConnectionInfo.HeartbeatIntervalMs), this);
        }

        protected void Heartbeat(object state)
        {
            if (cancel.IsCancellationRequested)
                return;

            var elapsedMs = (DateTime.UtcNow - LastPulseAt).TotalMilliseconds;
            if (elapsedMs > ConnectionInfo.IdleTimeoutMs)
            {
                OnExceptionReceived(new TimeoutException("Last Heartbeat Pulse was {0}ms ago".Fmt(elapsedMs)));
                return;
            }

            EnsureSynchronizationContext();

            ConnectionInfo.HeartbeatUrl.GetStringFromUrlAsync(requestFilter:HeartbeatRequestFilter)
                .Success(t => {
                    if (cancel.IsCancellationRequested)
                        return;

                    if (log.IsDebugEnabled)
                        log.DebugFormat("Heartbeat sent to: " + ConnectionInfo.HeartbeatUrl);

                    StartNewHeartbeat();
                })
                .Error(ex => {
                    if (cancel.IsCancellationRequested)
                        return;

                    if (log.IsDebugEnabled)
                        log.DebugFormat("Error from Heartbeat: {0}", ex.UnwrapIfSingleException().Message);
                    OnExceptionReceived(ex);
                });
        }

        private static void EnsureSynchronizationContext()
        {
            if (SynchronizationContext.Current != null) return;
            
            //Unit test runner
            if (log.IsDebugEnabled)
                log.DebugFormat("SynchronizationContext.Current == null");

            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
        }

        protected void OnCommandReceived(ServerEventCommand e)
        {
            if (log.IsDebugEnabled)
                log.DebugFormat("OnCommandReceived: {0} on #{1}", e.EventId, ConnectionInfo.DisplayName);

            var hold = commandTcs;
            commandTcs = new TaskCompletionSource<ServerEventCommand>();

            if (OnCommand != null)
                OnCommand(e);

            hold.SetResult(e);
        }

        protected void OnMessageReceived(ServerEventMessage e)
        {
            if (log.IsDebugEnabled)
                log.DebugFormat("OnMessageReceived: {0} on #{1}", e.EventId, ConnectionInfo.DisplayName);

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
            log.Error("OnExceptionReceived: {0} on #{1}".Fmt(ex.Message, ConnectionInfo.DisplayName), ex);

            if (OnException != null)
                OnException(ex);

            Restart();
        }

        private void Restart()
        {
            try
            {
                Stop();
                SleepBackOffMultiplier(errorsCount)
                    .ContinueWith(t =>
                        Start());
            }
            catch (Exception ex)
            {
                log.Error("Error whilst restarting: {0}".Fmt(ex.Message), ex);
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
                        if (text == "\n")
                        {
                            ProcessEventMessage(currentMsg);
                            currentMsg = null;
                            text = "";
                            break;
                        }

                        var line = text.Substring(0, pos);
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
                        log.DebugFormat("Connection ended on {0}", 
                            ConnectionInfo != null ? ConnectionInfo.DisplayName : null);
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
            e.Json = parts[1];

            if (!string.IsNullOrEmpty(e.Selector))
            {
                parts = e.Selector.SplitOnFirst('.');
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
            var msg = JsonObject.Parse(e.Json);
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
            var msg = JsonObject.Parse(e.Json);
            var joinMsg = new ServerEventJoin().Populate(e, msg);
            joinMsg.UserId = msg.Get("userId");
            joinMsg.DisplayName = msg.Get("displayName");
            joinMsg.ProfileUrl = msg.Get("profileUrl");

            OnCommandReceived(joinMsg);
        }

        private void ProcessOnLeaveMessage(ServerEventMessage e)
        {
            var msg = JsonObject.Parse(e.Json);
            var leaveMsg = new ServerEventLeave().Populate(e, msg);

            OnCommandReceived(leaveMsg);
        }

        private void ProcessOnHeartbeatMessage(ServerEventMessage e)
        {
            LastPulseAt = DateTime.UtcNow;
            var msg = JsonObject.Parse(e.Json);
            var heartbeatMsg = new ServerEventLeave().Populate(e, msg);

            if (OnHeartbeat != null)
                OnHeartbeat();

            OnCommandReceived(heartbeatMsg);
        }

        public virtual void Stop()
        {
            if (log.IsDebugEnabled)
                log.DebugFormat("Stop()");

            if (cancel != null)
                cancel.Cancel();

            if (ConnectionInfo != null && ConnectionInfo.UnRegisterUrl != null)
            {
                EnsureSynchronizationContext();
                ConnectionInfo.UnRegisterUrl.GetStringFromUrlAsync()
                    .Error(ex => { /*ignore*/});
            }

            using (response)
            {
                response = null;
            }

            ConnectionInfo = null;
            httpReq = null;
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