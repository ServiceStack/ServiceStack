using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.Text;

#if NET6_0_OR_GREATER
using System.Net.Http;
#endif

namespace ServiceStack
{
    public class ServerEventConnect : ServerEventCommand
    {
        public string Id { get; set; }
        public string UnRegisterUrl { get; set; }
        public string UpdateSubscriberUrl { get; set; }
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
        public DateTime CreatedAt { get; set; }
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

    [DataContract]
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

        private int status;
        private int timesStarted = 0;
        private int noOfContinuousErrors = 0;
        private int noOfErrors = 0;
        private string lastExMsg = null;

        public bool IsStopped => Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Stopped;
        public string Status => WorkerStatus.ToString(Interlocked.CompareExchange(ref status, 0, 0));
        public int TimesStarted => Interlocked.CompareExchange(ref timesStarted, 0, 0);

        byte[] buffer;
        readonly Encoding encoding = Encoding.UTF8;

        CancellationTokenSource cancel;
        private ITimer heartbeatTimer;

        public ServerEventConnect ConnectionInfo { get; private set; }

        public string SubscriptionId => ConnectionInfo?.Id;

        public string ConnectionDisplayName => ConnectionInfo != null ? ConnectionInfo.DisplayName : "(not connected)";

        public Func<string, string> ResolveStreamUrl { get; set; }

        public string BaseUri
        {
            get
            {
                var meta = this.ServiceClient as IServiceClientMeta;
                return meta?.BaseUri;
            }
            set
            {
                this.eventStreamPath = value.CombineWith("event-stream");
                BuildEventStreamUri();

                if (this.ServiceClient is IServiceClientMeta meta)
                    meta.BaseUri = value;
            }
        }

        private string[] channels;
        public string[] Channels
        {
            get => channels;
            set
            {
                this.channels = value ?? throw new ArgumentNullException(nameof(channels));
                BuildEventStreamUri();
            }
        }

        private void BuildEventStreamUri()
        {
            this.EventStreamUri = this.EventStreamPath
                .AddQueryParam("channels", string.Join(",", this.channels));
        }

        private string eventStreamPath;
        public string EventStreamPath
        {
            get => eventStreamPath;
            set
            {
                eventStreamPath = value?.StartsWith("/") == true
                    ? (BaseUri ?? "").CombineWith(value)
                    : value;
                BuildEventStreamUri();
            }
        }

        private string eventStreamUri;
        public string EventStreamUri
        {
            get => ResolveStreamUrl != null ? ResolveStreamUrl(eventStreamUri) : eventStreamUri;
            private set => eventStreamUri = value;
        }

        public IServiceClient ServiceClient { get; set; }
        public DateTime LastPulseAt { get; set; }

        public Action<ServerEventConnect> OnConnect;
        public Action<ServerEventJoin> OnJoin;
        public Action<ServerEventLeave> OnLeave;
        public Action<ServerEventUpdate> OnUpdate;
        public Action<ServerEventMessage> OnCommand;
        public Action<ServerEventMessage> OnMessage;
        public Action OnHeartbeat;
        public Action OnReconnect;
        public Action<Exception> OnException;

#if NET6_0_OR_GREATER
        public Action<HttpRequestMessage> EventStreamRequestFilter { get; set; }
        public Action<HttpRequestMessage> HeartbeatRequestFilter { get; set; }
        public Action<HttpRequestMessage> UnRegisterRequestFilter { get; set; }
        /// <summary>
        /// Apply Request Filter to all ServerEventClient Requests
        /// </summary>
        public Action<HttpRequestMessage> AllRequestFilters { get; set; }
        HttpClient httpClient;
        public Func<IServiceClient,HttpClientHandler> HttpClientHandlerFactory { get; set; } = client => new()
        {
            UseCookies = true,
            CookieContainer = ((IHasCookieContainer)client).CookieContainer,
            UseDefaultCredentials = true,
            AutomaticDecompression =
                DecompressionMethods.Brotli | DecompressionMethods.Deflate | DecompressionMethods.GZip,
        };
#else
        public Action<WebRequest> EventStreamRequestFilter { get; set; }
        public Action<WebRequest> HeartbeatRequestFilter { get; set; }
        public Action<WebRequest> UnRegisterRequestFilter { get; set; }
        /// <summary>
        /// Apply Request Filter to all ServerEventClient Requests
        /// </summary>
        public Action<WebRequest> AllRequestFilters { get; set; }
        HttpWebRequest httpReq;
        HttpWebResponse response;
#endif

        readonly Dictionary<string, List<Action<ServerEventMessage>>> listeners = new();

        public ServerEventsClient(string baseUri, params string[] channels)
        {
            this.eventStreamPath = baseUri.CombineWith("event-stream");
            this.Channels = channels;

#if NET6_0_OR_GREATER
            this.ServiceClient = new JsonApiClient(baseUri);
#else
            this.ServiceClient = new JsonServiceClient(baseUri);
#endif

            this.Resolver = new NewInstanceResolver();
            this.ReceiverTypes = new List<Type>();
            this.Handlers = new ConcurrentDictionary<string, ServerEventCallback>();
            this.NamedReceivers = new ConcurrentDictionary<string, ServerEventCallback>();
        }

#if NET6_0_OR_GREATER
        public async Task<ServerEventsClient> StartAsync(CancellationToken token = default)
        {
            if (log.IsDebugEnabled)
                log.DebugFormat("StartAsync()");

            if (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Disposed)
                throw new ObjectDisposedException(GetType().Name + " has been disposed");

            if (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Started)
                return this;

            if (Interlocked.CompareExchange(ref status, WorkerStatus.Starting, WorkerStatus.Stopped) == WorkerStatus.Stopped ||
                Interlocked.CompareExchange(ref status, WorkerStatus.Starting, WorkerStatus.Stopping) == WorkerStatus.Stopping)
            {
                Interlocked.Increment(ref timesStarted);

                httpClient = new HttpClient(HttpClientHandlerFactory(ServiceClient), disposeHandler:true);
                httpClient.Timeout = Timeout.InfiniteTimeSpan;
                
                var httpReq = new HttpRequestMessage(HttpMethod.Get, EventStreamUri)
                    .With(c => c.Accept = MimeTypes.Json);

                EventStreamRequestFilter?.Invoke(httpReq);
                if (AllRequestFilters != null)
                {
                    AllRequestFilters(httpReq);
                    if (ServiceClient is JsonApiClient apiClient)
                        apiClient.RequestFilter = AllRequestFilters;
                }

                var httpRes = await httpClient.SendAsync(httpReq, HttpCompletionOption.ResponseHeadersRead, token).ConfigAwait();
                httpRes.EnsureSuccessStatusCode();
                var stream = await httpRes.Content.ReadAsStreamAsync(token).ConfigAwait();

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

                if (Interlocked.CompareExchange(ref status, WorkerStatus.Started, WorkerStatus.Starting) != WorkerStatus.Starting)
                    return this;

                ProcessResponse(stream);
            }

            return this;
        }
#endif
        
        public ServerEventsClient Start()
        {
            if (log.IsDebugEnabled)
                log.DebugFormat("Start()");

            if (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Disposed)
                throw new ObjectDisposedException(GetType().Name + " has been disposed");

            if (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Started)
                return this;

            if (Interlocked.CompareExchange(ref status, WorkerStatus.Starting, WorkerStatus.Stopped) == WorkerStatus.Stopped ||
                Interlocked.CompareExchange(ref status, WorkerStatus.Starting, WorkerStatus.Stopping) == WorkerStatus.Stopping)
            {
                Interlocked.Increment(ref timesStarted);

#if NET6_0_OR_GREATER
                httpClient = new HttpClient(HttpClientHandlerFactory(ServiceClient), disposeHandler:true);
                httpClient.Timeout = Timeout.InfiniteTimeSpan;
                
                var httpReq = new HttpRequestMessage(HttpMethod.Get, EventStreamUri)
                    .With(c => c.Accept = MimeTypes.Json);

                EventStreamRequestFilter?.Invoke(httpReq);
                if (AllRequestFilters != null)
                {
                    AllRequestFilters(httpReq);
                    if (ServiceClient is JsonApiClient apiClient)
                        apiClient.RequestFilter = AllRequestFilters;
                }

                var httpRes = httpClient.Send(httpReq, HttpCompletionOption.ResponseHeadersRead);
                httpRes.EnsureSuccessStatusCode();
                var stream = httpRes.Content.ReadAsStream();
#else
                httpReq = WebRequest.CreateHttp(EventStreamUri);
                //share auth cookies
                httpReq.CookieContainer = ((IHasCookieContainer)ServiceClient).CookieContainer;
                httpReq.AllowReadStreamBuffering = false;
                httpReq.Accept = MimeTypes.Json;

                EventStreamRequestFilter?.Invoke(httpReq);
                if (AllRequestFilters != null)
                {
                    AllRequestFilters(httpReq);
                    if (ServiceClient is ServiceClientBase scb)
                        scb.RequestFilter = AllRequestFilters;
                }

                response = (HttpWebResponse)httpReq.GetResponse();
                var stream = response.ResponseStream();
#endif

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

                if (Interlocked.CompareExchange(ref status, WorkerStatus.Started, WorkerStatus.Starting) != WorkerStatus.Starting)
                    return this;

                ProcessResponse(stream);
            }

            return this;
        }

        private bool HasClient() =>
#if NET6_0_OR_GREATER
            this.httpClient != null;
#else
            this.httpReq != null;
#endif
        
        private void UnsetClient()
        {
#if NET6_0_OR_GREATER
            this.httpClient = null;
#else
            using (response)
            {
                response = null;
            }
            this.httpReq = null;
#endif
        }

        private TaskCompletionSource<ServerEventConnect> connectTcs;
        public Task<ServerEventConnect> Connect()
        {
            if (!HasClient())
                Start();

            return connectTcs.Task;
        }

#if NET6_0_OR_GREATER
        public Task<ServerEventConnect> ConnectAsync()
        {
            if (!HasClient())
                _ = StartAsync();

            return connectTcs.Task;
        }
#endif

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

            OnConnect?.Invoke(ConnectionInfo);

            hold.SetResult(ConnectionInfo); //needs to be at end or control yielded before Heartbeat can start
        }

        protected void StartNewHeartbeat()
        {
            if (string.IsNullOrEmpty(ConnectionInfo?.HeartbeatUrl))
                return;

            heartbeatTimer?.Cancel();

            heartbeatTimer = PclExportClient.Instance.CreateTimer(Heartbeat,
                TimeSpan.FromMilliseconds(ConnectionInfo.HeartbeatIntervalMs), this);
        }

        protected void Heartbeat(object state)
        {
            if (log.IsDebugEnabled)
                log.Debug("[SSE-CLIENT] Prep for Heartbeat...");

            if (cancel.IsCancellationRequested)
            {
                if (log.IsDebugEnabled)
                    log.Debug("[SSE-CLIENT] Heartbeat CancellationRequested");

                return;
            }

            if (ConnectionInfo?.HeartbeatUrl == null)
                return;

            var elapsedMs = (DateTime.UtcNow - LastPulseAt).TotalMilliseconds;
            if (elapsedMs > ConnectionInfo.IdleTimeoutMs)
            {
                OnExceptionReceived(new TimeoutException($"Last Heartbeat Pulse was {elapsedMs}ms ago"));
                return;
            }

            EnsureSynchronizationContext();

#if NET6_0_OR_GREATER
            var taskString = httpClient.SendStringToUrlAsync(ConnectionInfo.HeartbeatUrl, method:HttpMethods.Get, requestFilter: req => {
                HeartbeatRequestFilter?.Invoke(req);
                AllRequestFilters?.Invoke(req);

                if (log.IsDebugEnabled)
                    log.Debug("[SSE-CLIENT] Sending Heartbeat...");
            });
#else
            var taskString = ConnectionInfo.HeartbeatUrl.GetStringFromUrlAsync(requestFilter: req => {
                    var hold = httpReq;
                    if (hold != null)
                        req.CookieContainer = hold.CookieContainer;

                    HeartbeatRequestFilter?.Invoke(req);
                    AllRequestFilters?.Invoke(req);

                    if (log.IsDebugEnabled)
                        log.Debug("[SSE-CLIENT] Sending Heartbeat...");
                });
#endif
            
            taskString.Success(t =>
                {
                    if (cancel.IsCancellationRequested)
                    {
                        if (log.IsDebugEnabled)
                            log.Debug("[SSE-CLIENT] Heartbeat is cancelled.");

                        return;
                    }

                    if (log.IsDebugEnabled)
                        log.Debug("[SSE-CLIENT] Heartbeat sent to: " + ConnectionInfo.HeartbeatUrl);

                    StartNewHeartbeat();
                })
                .Error(ex =>
                {
                    if (cancel.IsCancellationRequested)
                    {
                        if (log.IsDebugEnabled)
                            log.Debug("[SSE-CLIENT] Heartbeat error. Heartbeat is cancelled.");

                        return;
                    }

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

        protected void OnJoinReceived(ServerEventJoin e)
        {
            if (log.IsDebugEnabled)
                log.Debug($"[SSE-CLIENT] OnJoinReceived: ({e.GetType().Name}) #{e.EventId} on #{ConnectionDisplayName} ({string.Join(", ", Channels)})");

            OnJoin?.Invoke(e);
        }

        protected void OnLeaveReceived(ServerEventLeave e)
        {
            if (log.IsDebugEnabled)
                log.Debug($"[SSE-CLIENT] OnLeaveReceived: ({e.GetType().Name}) #{e.EventId} on #{ConnectionDisplayName} ({string.Join(", ", Channels)})");

            OnLeave?.Invoke(e);
        }

        protected void OnUpdateReceived(ServerEventUpdate e)
        {
            if (log.IsDebugEnabled)
                log.Debug($"[SSE-CLIENT] OnUpdateReceived: ({e.GetType().Name}) #{e.EventId} on #{ConnectionDisplayName} ({string.Join(", ", Channels)})");

            OnUpdate?.Invoke(e);
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

        protected void OnExceptionReceived(Exception ex)
        {
            Interlocked.Increment(ref noOfErrors);
            Interlocked.Increment(ref noOfContinuousErrors);

            ex = ex.UnwrapIfSingleException();
            log.Error($"[SSE-CLIENT] OnExceptionReceived: {ex.Message} on #{ConnectionDisplayName}", ex);

            OnException?.Invoke(ex);

            Restart();
        }

        public void Restart()
        {
            if (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Disposed)
                throw new ObjectDisposedException(GetType().Name + " has been disposed");

            var statusSnapshot = Interlocked.CompareExchange(ref status, 0, 0);
            if (statusSnapshot == WorkerStatus.Stopping || statusSnapshot == WorkerStatus.Stopped)
                return;

            try
            {
                Interlocked.Exchange(ref status, WorkerStatus.Stopping);
                InternalStop()
                    .ContinueWith(task =>
                    {
                        SleepBackOffMultiplier(Interlocked.CompareExchange(ref noOfContinuousErrors, 0, 0))
#if NET6_0_OR_GREATER
                            .ContinueWith(async t =>
                            {
                                await t.ObserveTaskExceptions();
                                if (IsStopped)
                                    return;
                                try
                                {
                                    await StartAsync();
                                    OnReconnect?.Invoke();
                                }
                                catch (Exception ex)
                                {
                                    OnExceptionReceived(ex);
                                }
                            });
#else
                            .ContinueWith(t =>
                            {
                                t.ObserveTaskExceptions();
                                if (IsStopped)
                                    return;
                                try
                                {
                                    Start();
                                    OnReconnect?.Invoke();
                                }
                                catch (Exception ex)
                                {
                                    OnExceptionReceived(ex);
                                }
                            });
#endif
                    });
            }
            catch (Exception ex)
            {
                log.Error($"[SSE-CLIENT] Error whilst restarting: {ex.Message}", ex);
            }
        }

        readonly Random rand = new(Environment.TickCount);
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
            if (Interlocked.CompareExchange(ref status, 0, 0) != WorkerStatus.Started)
                return;
            
            if (!stream.CanRead) return;

            var task = stream.ReadAsync(buffer, 0, BufferSize, cancel.Token);
            task.ContinueWith(t =>
            {
                t.ObserveTaskExceptions();
                if (cancel.IsCancellationRequested || t.IsCanceled)
                {
                    UnsetClient();
                    return;
                }

                if (t.IsFaulted)
                {
                    OnExceptionReceived(t.Exception);
                    UnsetClient();
                    return;
                }

                Interlocked.Exchange(ref noOfContinuousErrors, 0);

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
                            {
                                try
                                {
                                    ProcessEventMessage(currentMsg);
                                }
                                catch (Exception ex)
                                {
                                    log.Error($"Unhandled Exception processing {currentMsg.Selector}: {ex.Message}");
                                    OnException?.Invoke(ex);
                                }
                            }

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

        private ServerEventMessage currentMsg { get; set; }

        public void ProcessLine(string line)
        {
            if (line == null) 
                return;

            currentMsg ??= new ServerEventMessage();

            var label = line.LeftPart(':');
            var data = line.RightPart(':');
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

        public static ServerEventMessage ToTypedMessage(ServerEventMessage e)
        {
            e.Selector = e.Data.LeftPart(' ');
            if (e.Selector.IndexOf('@') >= 0)
            {
                e.Channel = e.Selector.LeftPart('@');
                e.Selector = e.Selector.RightPart('@');
            }

            e.Json = e.Data.RightPart(' ');

            if (!string.IsNullOrEmpty(e.Selector))
            {
                if (e.Selector.IndexOf('.') == -1)
                    throw new ArgumentException($"Invalid Selector '{e.Selector}'");

                e.Op = e.Selector.LeftPart('.');
                var target = e.Selector.RightPart('.').Replace("%20", " ");

                e.Target = target.LeftPart('$');
                if (e.Target.Length < target.Length)
                    e.CssSelector = target.RightPart('$');

                if (e.Op == "cmd")
                {
                    switch (e.Target)
                    {
                        case "onConnect":
                            var msg = JsonServiceClient.ParseObject(e.Json);
                            var to = new ServerEventConnect
                            {
                                HeartbeatIntervalMs = DefaultHeartbeatMs,
                                IdleTimeoutMs = DefaultIdleTimeoutMs,
                            }.Populate(e, msg);
                            to.Id = msg.Get("id");
                            to.HeartbeatUrl = msg.Get("heartbeatUrl");
                            to.HeartbeatIntervalMs = msg.Get<long>("heartbeatIntervalMs");
                            to.IdleTimeoutMs = msg.Get<long>("idleTimeoutMs");
                            to.UnRegisterUrl = msg.Get("unRegisterUrl");
                            to.UpdateSubscriberUrl = msg.Get("updateSubscriberUrl");
                            to.UserId = msg.Get("userId");
                            to.DisplayName = msg.Get("displayName");
                            to.IsAuthenticated = msg.Get("isAuthenticated") == "true";
                            to.ProfileUrl = msg.Get("profileUrl");
                            return to;
                        case "onJoin":
                            return new ServerEventJoin().Populate(e, JsonServiceClient.ParseObject(e.Json));
                        case "onLeave":
                            return new ServerEventLeave().Populate(e, JsonServiceClient.ParseObject(e.Json));
                        case "onUpdate":
                            return new ServerEventUpdate().Populate(e, JsonServiceClient.ParseObject(e.Json));
                        case "onHeartbeat":
                            return new ServerEventHeartbeat().Populate(e, JsonServiceClient.ParseObject(e.Json));
                    }
                }
            }

            return e;
        }
        
        void ProcessEventMessage(ServerEventMessage e)
        {
            var validMsg = e.Data.IndexOf(' ') >= 0;  
            if (!validMsg) // If it was not a valid msg sent with a Server Events client just fire event + return
            {
                OnMessageReceived(e);
                return;
            }

            var msg = ToTypedMessage(e);
            if (e.Op == "cmd")
            {
                switch (msg)
                {
                    case ServerEventConnect c:
                        ProcessOnConnectMessage(c);
                        return;
                    case ServerEventJoin j:
                        ProcessOnJoinMessage(j);
                        return;
                    case ServerEventLeave l:
                        ProcessOnLeaveMessage(l);
                        return;
                    case ServerEventUpdate u:
                        ProcessOnUpdateMessage(u);
                        return;
                    case ServerEventHeartbeat h:
                        ProcessOnHeartbeatMessage(h);
                        return;
                    default:
                        if (Handlers.TryGetValue(e.Target, out var cb))
                        {
                            cb(this, e);
                        }
                        break;
                }
            }
            else if (e.Op == "trigger")
            {
                RaiseEvent(e.Target, e);
            }

            NamedReceivers.TryGetValue(e.Op, out var receiver);
            receiver?.Invoke(this, e);

            OnMessageReceived(e);
        }

        private void ProcessOnConnectMessage(ServerEventConnect e)
        {
            ConnectionInfo = e;
            OnConnectReceived();
        }

        private void ProcessOnJoinMessage(ServerEventJoin msg)
        {
            OnJoinReceived(msg);
            OnCommandReceived(msg);
        }

        private void ProcessOnLeaveMessage(ServerEventLeave msg)
        {
            OnLeaveReceived(msg);
            OnCommandReceived(msg);
        }

        private void ProcessOnUpdateMessage(ServerEventUpdate msg)
        {
            OnUpdateReceived(msg);
            OnCommandReceived(msg);
        }

        private void ProcessOnHeartbeatMessage(ServerEventHeartbeat msg)
        {
            LastPulseAt = DateTime.UtcNow;
            if (log.IsDebugEnabled)
                log.Debug("[SSE-CLIENT] LastPulseAt: " + DateTime.UtcNow.TimeOfDay);

            OnHeartbeatReceived(msg);
        }

        public virtual Task Stop()
        {
            Interlocked.Exchange(ref status, WorkerStatus.Stopped);
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
#if NET6_0_OR_GREATER
                    httpClient.SendStringToUrl(ConnectionInfo.UnRegisterUrl, method:HttpMethods.Get, requestFilter: req =>
                    {
                        if (log.IsDebugEnabled)
                            log.Debug("[SSE-CLIENT] Unregistering...");
                        
                        UnRegisterRequestFilter?.Invoke(req);
                        AllRequestFilters?.Invoke(req);
                    });
#else                    
                    ConnectionInfo.UnRegisterUrl.GetStringFromUrl(requestFilter: req =>
                    {
                        var hold = httpReq;
                        if (hold != null)
                            req.CookieContainer = hold.CookieContainer;

                        if (log.IsDebugEnabled)
                            log.Debug("[SSE-CLIENT] Unregistering...");
                        
                        UnRegisterRequestFilter?.Invoke(req);
                        AllRequestFilters?.Invoke(req);
                    });
#endif
                } catch (Exception) {}
            }

            ConnectionInfo = null;
            UnsetClient();

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

        public ServerEventsClient AddListener(string eventName, Action<ServerEventMessage> handler)
        {
            lock (listeners)
            {
                if (!listeners.TryGetValue(eventName, out var handlers))
                {
                    listeners[eventName] = handlers = new List<Action<ServerEventMessage>>();
                }

                handlers.Add(handler);
            }

            return this;
        }

        public ServerEventsClient RemoveListener(string eventName, Action<ServerEventMessage> handler)
        {
            lock (listeners)
            {
                if (listeners.TryGetValue(eventName, out var handlers))
                {
                    handlers.Remove(handler);
                }
            }

            return this;
        }

        public ServerEventsClient RemoveListeners(string eventName)
        {
            lock (listeners)
            {
                if (listeners.TryGetValue(eventName, out var handlers))
                    handlers.Clear();
            }

            return this;
        }

        public bool HasListener(string eventName, Action<ServerEventMessage> handler)
        {
            lock (listeners)
            {
                if (listeners.TryGetValue(eventName, out var handlers))
                    return handlers.Contains(handler);
            }
            return false;
        }

        public bool HasListeners(string eventName)
        {
            lock (listeners)
            {
                if (listeners.TryGetValue(eventName, out var handlers))
                    return handlers.Any();
            }
            return false;
        }

        public void RaiseEvent(string eventName, ServerEventMessage message)
        {
            lock (listeners)
            {
                if (listeners.TryGetValue(eventName, out var handlers))
                {
                    foreach (var handler in handlers)
                    {
                        try
                        {
                            handler(message);
                        }
                        catch (Exception ex)
                        {
                            log.Error($"Error whilst executing '{eventName}' handler", ex);
                        }
                    }
                }
            }
        }

        public void RemoveAllListeners()
        {
            lock (listeners)
            {
                listeners.Clear();
            }
        }

        /// <summary>
        /// Removes all registered Handlers, Named Receivers and Listeners
        /// </summary>
        public void RemoveAllRegistrations()
        {
            Handlers.Clear();
            NamedReceivers.Clear();
            ReceiverTypes.Clear();
            RemoveAllListeners();
        }

        public virtual string GetStatsDescription()
        {
            lock (this)
            {
                var sb = StringBuilderCache.Allocate().Append(GetType().Name + " SERVER STATS:\n");
                sb.AppendLine("===============");
                sb.AppendLine("Current Status: " + Status);
                sb.AppendLine("Listening On: " + EventStreamUri);
                sb.AppendLine("Times Started: " + Interlocked.CompareExchange(ref timesStarted, 0, 0));
                sb.AppendLine("Num of Errors: " + Interlocked.CompareExchange(ref noOfErrors, 0, 0));
                sb.AppendLine("Num of Continuous Errors: " + Interlocked.CompareExchange(ref noOfContinuousErrors, 0, 0));
                sb.AppendLine("Last ErrorMsg: " + lastExMsg);
                sb.AppendLine("===============");
                return StringBuilderCache.ReturnAndFree(sb);
            }
        }

        public void Dispose()
        {
            if (log.IsDebugEnabled)
                log.Debug("Dispose()");

            Stop();
            Interlocked.Exchange(ref status, WorkerStatus.Disposed);
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
                if (long.TryParse(msg.Get("createdAt"), out long unixTimeMs))
                {
                    cmd.CreatedAt = unixTimeMs.FromUnixTimeMs();
                }

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

        internal static Task ObserveTaskExceptions(this Task t)
        {
            if (t.IsFaulted)
                t.Exception?.Handle(x => true);
            return t;
        }
    }
}