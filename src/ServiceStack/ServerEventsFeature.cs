using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using ServiceStack.Auth;
using ServiceStack.DataAnnotations;
using ServiceStack.Host.Handlers;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public class ServerEventsFeature : IPlugin
    {
        public string StreamPath { get; set; }
        public string HeartbeatPath { get; set; }
        public string SubscribersPath { get; set; }
        public string UnRegisterPath { get; set; }

        public TimeSpan IdleTimeout { get; set; }
        public TimeSpan HeartbeatInterval { get; set; }
        public TimeSpan HouseKeepingInterval { get; set; }

        public Action<IRequest> OnInit { get; set; }
        public Action<IRequest> OnHeartbeatInit { get; set; }
        public Action<IEventSubscription, IRequest> OnCreated { get; set; }
        public Action<IEventSubscription, Dictionary<string, string>> OnConnect { get; set; }
        public Action<IEventSubscription> OnSubscribe { get; set; }
        public Action<IEventSubscription, IRequest> OnDispose { get; set; }
        /// <summary>
        /// Calls sync OnSubscribe if exists by default
        /// </summary>
        public Func<IEventSubscription, Task> OnSubscribeAsync { get; set; }
        public Action<IEventSubscription> OnUnsubscribe { get; set; }
        /// <summary>
        /// Calls sync OnUnsubscribe if exists by default
        /// </summary>
        public Func<IEventSubscription,Task> OnUnsubscribeAsync { get; set; }
        /// <summary>
        /// Fired for both Sync/Async Notifications 
        /// </summary>
        public Action<IEventSubscription, IResponse, string> OnPublish { get; set; }
        /// <summary>
        /// Only fired for async notifications. Calls sync OnPublish if exists by default.
        /// </summary>
        public Func<IEventSubscription, IResponse, string, Task> OnPublishAsync { get; set; }

        public Action<IResponse, string> WriteEvent { get; private set; }
        public Func<IResponse, string, Task> WriteEventAsync { get; private set; }

        public Action<IEventSubscription, Exception> OnError { get; set; }
        public bool NotifyChannelOfSubscriptions { get; set; }
        public bool LimitToAuthenticatedUsers { get; set; }
        public bool ValidateUserAddress { get; set; }

        public ServerEventsFeature()
        {
            StreamPath = "/event-stream";
            HeartbeatPath = "/event-heartbeat";
            UnRegisterPath = "/event-unregister";
            SubscribersPath = "/event-subscribers";

            WriteEvent = (res, frame) =>
            {
                if (res is IWriteEvent writeEvent)
                {
                    writeEvent.WriteEvent(frame);
                }
                else
                {
                    MemoryProvider.Instance.Write(res.AllowSyncIO().OutputStream, frame.AsMemory());
                    res.Flush();
                }
            };

            WriteEventAsync = async (res, frame) => 
            {
                if (res is IWriteEventAsync writeEvent)
                {
                    await writeEvent.WriteEventAsync(frame);
                }
                else
                {
                    await MemoryProvider.Instance.WriteAsync(res.OutputStream, frame.AsMemory());
                    await res.FlushAsync();
                }
            };

            IdleTimeout = TimeSpan.FromSeconds(30);
            HeartbeatInterval = TimeSpan.FromSeconds(10);
            HouseKeepingInterval = TimeSpan.FromSeconds(5);

            NotifyChannelOfSubscriptions = true;
            ValidateUserAddress = true;

            OnSubscribeAsync = sub => {
                OnSubscribe?.Invoke(sub);
                return TypeConstants.EmptyTask;
            };

            OnUnsubscribeAsync = sub => {
                OnUnsubscribe?.Invoke(sub);
                return TypeConstants.EmptyTask;
            };

            OnPublishAsync = (sub, res, msg) => {
                OnPublish?.Invoke(sub, res, msg);
                return TypeConstants.EmptyTask;
            };
        }

        public void Register(IAppHost appHost)
        {
            var container = appHost.GetContainer();

            if (!container.Exists<IServerEvents>())
            {
                var broker = new MemoryServerEvents
                {
                    IdleTimeout = IdleTimeout,
                    HouseKeepingInterval = HouseKeepingInterval,
                    OnSubscribeAsync = OnSubscribeAsync,
                    OnUnsubscribeAsync = OnUnsubscribeAsync,
                    NotifyChannelOfSubscriptions = NotifyChannelOfSubscriptions,
                    OnError = OnError,
                };
                container.Register<IServerEvents>(broker);
            }

            appHost.RawHttpHandlers.Add(httpReq =>
                httpReq.PathInfo.EndsWith(StreamPath)
                    ? (IHttpHandler)new ServerEventsHandler()
                    : httpReq.PathInfo.EndsWith(HeartbeatPath)
                      ? new ServerEventsHeartbeatHandler()
                      : null);

            if (UnRegisterPath != null)
            {
                appHost.RegisterService(typeof(ServerEventsUnRegisterService), UnRegisterPath);
            }

            if (SubscribersPath != null)
            {
                appHost.RegisterService(typeof(ServerEventsSubscribersService), SubscribersPath);
            }
        }

        internal bool CanAccessSubscription(IRequest req, SubscriptionInfo sub)
        {
            if (!ValidateUserAddress)
                return true;

            return sub.UserAddress == null || sub.UserAddress == req.UserHostAddress;
        }
    }

    public class ServerEventsHandler : HttpAsyncTaskHandler
    {
        public override bool RunAsAsync()
        {
            return true;
        }

        /// <summary>
        /// Call IServerEvents.RemoveExpiredSubscriptions() after every count
        /// </summary>
        public static int RemoveExpiredSubscriptionsEvery { get; } = 1000;
        private static int ConnectionsCount = 0;

        public override async Task ProcessRequestAsync(IRequest req, IResponse res, string operationName)
        {
            if (HostContext.ApplyCustomHandlerRequestFilters(req, res))
                return;

            var feature = HostContext.AssertPlugin<ServerEventsFeature>();

            var session = req.GetSession();
            if (feature.LimitToAuthenticatedUsers && !session.IsAuthenticated)
            {
                await session.ReturnFailedAuthentication(req);
                return;
            }

            var serverEvents = req.TryResolve<IServerEvents>();
            if ((Interlocked.Increment(ref ConnectionsCount) % RemoveExpiredSubscriptionsEvery) == 0)
            {
                await serverEvents.RemoveExpiredSubscriptionsAsync();
            }

            res.ContentType = MimeTypes.ServerSentEvents;
            res.AddHeader(HttpHeaders.CacheControl, "no-cache");
            res.ApplyGlobalResponseHeaders();
            res.UseBufferedStream = false;
            res.KeepAlive = true;

            feature.OnInit?.Invoke(req);

            await res.FlushAsync();

            var userAuthId = session?.UserAuthId;
            var anonUserId = serverEvents.GetNextSequence("anonUser");
            var userId = userAuthId ?? ("-" + anonUserId);
            var displayName = session.GetSafeDisplayName()
                ?? "user" + anonUserId;

            var now = DateTime.UtcNow;
            var subscriptionId = SessionExtensions.CreateRandomSessionId();

            //Handle both ?channel=A,B,C or ?channels=A,B,C
            var channels = new List<string>();
            var channel = req.QueryString["channel"];
            if (!string.IsNullOrEmpty(channel))
                channels.AddRange(channel.Split(','));
            channel = req.QueryString["channels"];
            if (!string.IsNullOrEmpty(channel))
                channels.AddRange(channel.Split(','));

            if (channels.Count == 0)
                channels = EventSubscription.UnknownChannel.ToList();

            var subscription = new EventSubscription(res)
            {
                CreatedAt = now,
                LastPulseAt = now,
                Channels = channels.ToArray(),
                SubscriptionId = subscriptionId,
                UserId = userId,
                UserName = session?.UserName,
                DisplayName = displayName,
                SessionId = req.GetSessionId(),
                IsAuthenticated = session != null && session.IsAuthenticated,
                UserAddress = req.UserHostAddress,
                OnPublish = feature.OnPublish,
                OnPublishAsync = feature.OnPublishAsync,
                OnError = feature.OnError,
                Meta = {
                    { "userId", userId },
                    { "isAuthenticated", session != null && session.IsAuthenticated ? "true": "false" },
                    { "displayName", displayName },
                    { "channels", string.Join(",", channels) },
                    { "createdAt", now.ToUnixTimeMs().ToString() },
                    { AuthMetadataProvider.ProfileUrlKey, session.GetProfileUrl() ?? Svg.GetDataUri(Svg.Icons.DefaultProfile) },
                },
                ServerArgs = new Dictionary<string, string>(),
            };
            subscription.ConnectArgs = new Dictionary<string, string>(subscription.Meta);

            feature.OnCreated?.Invoke(subscription, req);

            if (req.Response.IsClosed)
                return; //Allow short-circuiting in OnCreated callback

            var heartbeatUrl = feature.HeartbeatPath != null
                ? req.ResolveAbsoluteUrl("~/".CombineWith(feature.HeartbeatPath)).AddQueryParam("id", subscriptionId)
                : null;

            var unRegisterUrl = feature.UnRegisterPath != null
                ? req.ResolveAbsoluteUrl("~/".CombineWith(feature.UnRegisterPath)).AddQueryParam("id", subscriptionId)
                : null;

            heartbeatUrl = AddSessionParamsIfAny(heartbeatUrl, req);
            unRegisterUrl = AddSessionParamsIfAny(unRegisterUrl, req);

            subscription.ConnectArgs = new Dictionary<string, string>(subscription.ConnectArgs) {
                {"id", subscriptionId },
                {"unRegisterUrl", unRegisterUrl},
                {"heartbeatUrl", heartbeatUrl},
                {"updateSubscriberUrl", req.ResolveAbsoluteUrl("~/".CombineWith(feature.SubscribersPath)) },
                {"heartbeatIntervalMs", ((long)feature.HeartbeatInterval.TotalMilliseconds).ToString(CultureInfo.InvariantCulture) },
                {"idleTimeoutMs", ((long)feature.IdleTimeout.TotalMilliseconds).ToString(CultureInfo.InvariantCulture)}
            };

            feature.OnConnect?.Invoke(subscription, subscription.ConnectArgs);

            await serverEvents.RegisterAsync(subscription, subscription.ConnectArgs);

            if (req.Response is IWriteEventAsync) // gRPC
            {
                subscription.OnDispose = sub =>
                {
                    try
                    {
                        feature.OnDispose?.Invoke(sub, req);
                        (sub as EventSubscription)?.EndRequest();
                    } catch { }
                };
                return;
            }
            
            var tcs = new TaskCompletionSource<bool>();

            subscription.OnDispose = sub =>
            {
                try
                {
                    feature.OnDispose?.Invoke(sub, req);
                    (sub as EventSubscription)?.EndRequest();
                } catch { }

                if (!res.IsClosed)
                    System.Diagnostics.Debug.Fail("Should already be closed");

                if (!tcs.Task.IsCompleted)
                    tcs.SetResult(true);
            };

            await tcs.Task;
        }

        static string AddSessionParamsIfAny(string url, IRequest req)
        {
            if (url != null && HostContext.Config.AllowSessionIdsInHttpParams)
            {
                var sessionKeys = new[] { "ss-id", "ss-pid", "ss-opt" };
                foreach (var key in sessionKeys)
                {
                    var value = req.QueryString[key];
                    if (value != null)
                        url = url.AddQueryParam(key, value);
                }
            }

            return url;
        }
    }

    public class ServerEventsHeartbeatHandler : HttpAsyncTaskHandler
    {
        public override bool RunAsAsync() { return true; }

        public override async Task ProcessRequestAsync(IRequest req, IResponse res, string operationName)
        {
            if (HostContext.ApplyCustomHandlerRequestFilters(req, res))
                return;

            res.ApplyGlobalResponseHeaders();

            var serverEvents = req.TryResolve<IServerEvents>();

            await serverEvents.RemoveExpiredSubscriptionsAsync();

            var feature = HostContext.GetPlugin<ServerEventsFeature>();
            feature.OnHeartbeatInit?.Invoke(req);

            if (req.Response.IsClosed)
                return;

            var subscriptionId = req.QueryString["id"];
            var subscription = serverEvents.GetSubscriptionInfo(subscriptionId);
            if (subscription == null)
            {
                res.StatusCode = 404;
                res.StatusDescription = ErrorMessages.SubscriptionNotExistsFmt.Fmt(subscriptionId.SafeInput());
            }
            else if (!feature.CanAccessSubscription(req, subscription))
            {
                res.StatusCode = 403;
                res.StatusDescription = "Invalid User Address";
            }
            else if (!await serverEvents.PulseAsync(subscriptionId))
            {
                res.StatusCode = 404;
                res.StatusDescription = ErrorMessages.SubscriptionNotExistsFmt.Fmt(subscriptionId.SafeInput());
            }
            
            await res.EndHttpHandlerRequestAsync(skipHeaders: true);
        }
    }

    [DefaultRequest(typeof(GetEventSubscribers))]
    [Restrict(VisibilityTo = RequestAttributes.None)]
    public class ServerEventsSubscribersService : Service
    {
        public IServerEvents ServerEvents { get; set; }

        public object Any(GetEventSubscribers request)
        {
            var channels = new List<string>();

            var deprecatedChannels = Request.QueryString["channel"];
            if (!string.IsNullOrEmpty(deprecatedChannels))
                channels.AddRange(deprecatedChannels.Split(','));

            if (request.Channels != null)
                channels.AddRange(request.Channels);

            return channels.Count > 0
                ? ServerEvents.GetSubscriptionsDetails(channels.ToArray())
                : ServerEvents.GetAllSubscriptionsDetails();
        }
    }

    [Exclude(Feature.Soap)]
    public class UnRegisterEventSubscriber : IReturn<Dictionary<string, string>>
    {
        public string Id { get; set; }
    }

    [DefaultRequest(typeof(UnRegisterEventSubscriber))]
    [Restrict(VisibilityTo = RequestAttributes.None)]
    public class ServerEventsUnRegisterService : Service
    {
        public IServerEvents ServerEvents { get; set; }

        public async Task<object> Any(UnRegisterEventSubscriber request)
        {
            var subscription = ServerEvents.GetSubscriptionInfo(request.Id);

            if (subscription == null)
                throw HttpError.NotFound(ErrorMessages.SubscriptionNotExistsFmt.Fmt(request.Id).SafeInput());

            var feature = HostContext.GetPlugin<ServerEventsFeature>();
            if (!feature.CanAccessSubscription(base.Request, subscription))
                throw HttpError.Forbidden(ErrorMessages.SubscriptionForbiddenFmt.Fmt(request.Id.SafeInput()));

            await ServerEvents.UnRegisterAsync(subscription.SubscriptionId);

            return subscription.Meta;
        }

        public async Task<object> Any(UpdateEventSubscriber request)
        {
            var subscription = ServerEvents.GetSubscriptionInfo(request.Id);

            if (subscription == null)
                throw HttpError.NotFound(ErrorMessages.SubscriptionNotExistsFmt.Fmt(request.Id.SafeInput()));

            var feature = HostContext.GetPlugin<ServerEventsFeature>();
            if (!feature.CanAccessSubscription(base.Request, subscription))
                throw HttpError.Forbidden(ErrorMessages.SubscriptionForbiddenFmt.Fmt(request.Id.SafeInput()));

            if (request.UnsubscribeChannels != null)
                await ServerEvents.UnsubscribeFromChannelsAsync(subscription.SubscriptionId, request.UnsubscribeChannels);
            if (request.SubscribeChannels != null)
                await ServerEvents.SubscribeToChannelsAsync(subscription.SubscriptionId, request.SubscribeChannels);

            return new UpdateEventSubscriberResponse();
        }
    }

    /*
    # Commands
    cmd.announce This is your captain speaking ...
    cmd.toggle$#channels

    # CSS
    css.background #eceff1
    css.background$#top #673ab7
    css.background$#right #fffde7
    css.background$#bottom #0091ea
    css.color$#me #ff0
    css.display$img none
    css.display$img inline

    # Receivers
    document.title New Window Title
    window.location http://google.com
    cmd.removeReceiver window
    cmd.addReceiver window
    tv.watch http://youtu.be/518XP8prwZo
    tv.watch https://servicestack.net/img/logo-220.png        
    tv.off

    # Triggers
    trigger.customEvent arg
    */

    public class EventSubscription : SubscriptionInfo, IEventSubscription
    {
        private static ILog Log = LogManager.GetLogger(typeof(EventSubscription));
        public static string[] UnknownChannel = { "*" };

        private long LastPulseAtTicks = DateTime.UtcNow.Ticks;
        public DateTime LastPulseAt
        {
            get
            {
                // assume gRPC connection is always active unless response is closed
                return !response.IsClosed && (response is IWriteEvent || response is IWriteEventAsync)
                   ? DateTime.UtcNow 
                   : new DateTime(Interlocked.Read(ref LastPulseAtTicks), DateTimeKind.Utc);
            }
            set => Interlocked.Exchange(ref LastPulseAtTicks, value.Ticks);
        }

        bool isDisposed;
        private long subscribed = 1;
        private long requestEnded = 0;
        
        // Don't access asyncLock or response if request is already ended
        public bool RequestEnded => Interlocked.Read(ref requestEnded) == 0; 

        private readonly IResponse response;
        private long msgId;

        public IResponse Response => this.response;
        public IRequest Request => this.response.Request;

        public long LastMessageId => Interlocked.Read(ref msgId);

        public EventSubscription(IResponse response)
        {
            this.response = response;
            this.Meta = new Dictionary<string, string>();
            var feature = HostContext.GetPlugin<ServerEventsFeature>();
            this.WriteEvent = feature.WriteEvent;
            this.WriteEventAsync = feature.WriteEventAsync;
        }

        public void UpdateChannels(string[] channels)
        {
            this.Channels = channels;
            this.Meta["channels"] = string.Join(",", channels);
        }

        public Func<IEventSubscription, Task> OnUnsubscribeAsync { get; set; }
        public Action<IEventSubscription> OnUnsubscribe { get; set; }
        public Action<IEventSubscription, IResponse, string> OnPublish { get; set; }
        public Func<IEventSubscription, IResponse, string, Task> OnPublishAsync { get; set; }
        public Action<IEventSubscription> OnDispose { get; set; }
        public Action<IResponse, string> WriteEvent { get; set; }
        public Func<IResponse, string, Task> WriteEventAsync { get; set; }
        public Action<IEventSubscription, Exception> OnError { get; set; }
        public bool IsClosed => this.response.IsClosed;
        public bool IsDisposed => isDisposed;

        StringBuilder buffer = new StringBuilder();
        
        public void Pulse()
        {
            LastPulseAt = DateTime.UtcNow;
        }

        string CreateFrame(string selector, string message)
        {
            var msg = message ?? "";
            var frame = "id: " + Interlocked.Increment(ref msgId) + "\n"
                      + "data: " + selector + " " + msg + "\n\n";
            return frame;
        }

        public void Publish(string selector)
        {
            Publish(selector, null);
        }

        readonly SemaphoreSlim asyncLock = new SemaphoreSlim(1);

        public Task PublishAsync(string selector, string message, CancellationToken token = default) => 
            PublishRawAsync(CreateFrame(selector, message), token);
        public async Task PublishRawAsync(string frame, CancellationToken token = default)
        {
            if (RequestEnded)
                return;
            try
            {
                if (await asyncLock.WaitAsync(0, token))
                {
                    if (!EndRequestIfDisposed())
                    {
                        var pendingWrites = GetAndResetBuffer();
                        if (pendingWrites != null)
                            frame = pendingWrites + frame;

                        try
                        {
                            if (OnPublishAsync != null)
                                await OnPublishAsync(this, response, frame);
                            
                            await WriteEventAsync(response, frame);
                        }
                        catch (Exception ex)
                        {
                            await HandleWriteExceptionAsync(frame, ex, token);
                        }

                        EndRequestIfDisposed();
                    }
                }
                else
                {
                    lock (buffer)
                        buffer.Append(frame);
                }
            }
            finally
            {
                asyncLock.Release();
            }
        }

        public void Publish(string selector, string message) => PublishRaw(CreateFrame(selector, message));

        public void PublishRaw(string frame)
        {
            if (RequestEnded)
                return;
            try
            {
                if (asyncLock.Wait(0))
                {
                    if (!EndRequestIfDisposed())
                    {
                        var pendingWrites = GetAndResetBuffer();
                        if (pendingWrites != null)
                            frame = pendingWrites + frame;

                        try
                        {
                            OnPublish?.Invoke(this, response, frame);
                            WriteEvent(response, frame);
                        }
                        catch (Exception ex)
                        {
                            TaskExt.RunSync(() => HandleWriteExceptionAsync(frame, ex));
                        }

                        EndRequestIfDisposed();
                    }
                }
                else
                {
                    lock (buffer)
                        buffer.Append(frame);
                }
            }
            finally
            {
                asyncLock.Release();
            }
        }

        string GetAndResetBuffer()
        {
            lock (buffer)
            {
                if (buffer.Length == 0)
                    return null;

                var ret = buffer.ToString();
                buffer.Length = 0;
                return ret;
            }
        }

        Task HandleWriteExceptionAsync(string frame, Exception ex, CancellationToken token=default)
        {
            if (ex != null)
            {
                Log.Warn("Could not publish notification to: " + frame.SafeSubstring(0, 50), ex);
                OnError?.Invoke(this, ex);
            }

            if (Env.IsMono)
            {
                // Mono: If we explicitly close OutputStream after the error socket wont leak (response.Close() doesn't work)
                try
                {
                    // This will throw an exception, but on Mono (Linux/OSX) the socket will leak if we not close the OutputStream
                    response.OutputStream.Close();
                }
                catch (Exception innerEx)
                {
                    Log.Error("OutputStream.Close()", innerEx);
                }
            }

            return UnsubscribeAsync();
        }

        private bool EndRequestIfDisposed()
        {
            if (!isDisposed) return false;
            if (response.IsClosed) return true;

            EndRequestNoLock();
            return true;
        }
        
        private void EndRequestNoLock()
        {
            if (Interlocked.CompareExchange(ref requestEnded, 1, 0) == 0)
            {
                try
                {
                    response.EndHttpHandlerRequest(skipHeaders: true);
                }
                catch (Exception ex)
                {
                    Log.Error("Error ending subscription response", ex);
                }
            }
        }

        public void EndRequest()
        {
            if (RequestEnded)
                return;

            try
            {
                if (asyncLock.Wait(1000))
                {
                    EndRequestNoLock();
                }
                else
                {
                    var msg = "Failed to acquire asyncLock to dispose of " + GetType().Name;
                    Log.Error(msg);
                    System.Diagnostics.Debug.Fail(msg);
                }
            }
            finally
            {
                asyncLock.Release();
            }
        }

        [Obsolete("Use UnsubscribeAsync. Will be removed in future.")]
        public void Unsubscribe()
        {
            if (Interlocked.CompareExchange(ref subscribed, 0, 1) == 1)
            {
                var fn = OnUnsubscribeAsync;
                OnUnsubscribeAsync = null;
                response.Request.TryResolve<IServerEvents>()?.QueueAsyncTask(() => fn(this));
            }
            Dispose();
        }

        public Task UnsubscribeAsync()
        {
            if (Interlocked.CompareExchange(ref subscribed, 0, 1) == 1)
            {
                var fn = OnUnsubscribeAsync;
                OnUnsubscribeAsync = null;
                if (fn != null)
                    return fn(this);
            }
            Dispose();
            return TypeConstants.EmptyTask;
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            EndRequest();

            asyncLock?.Dispose();
            OnDispose?.Invoke(this);
        }

    }

    public interface IEventSubscription : IMeta, IDisposable
    {
        DateTime CreatedAt { get; set; }
        DateTime LastPulseAt { get; set; }
        long LastMessageId { get; }

        string[] Channels { get; }
        string UserId { get; }
        string UserName { get; }
        string DisplayName { get; }
        string SessionId { get; }
        string SubscriptionId { get; }
        string UserAddress { get; set; }
        bool IsAuthenticated { get; set; }
        bool IsClosed { get; }

        void UpdateChannels(string[] channels);

        Func<IEventSubscription, Task> OnUnsubscribeAsync { get; set; }
        Action<IEventSubscription> OnUnsubscribe { get; set; }
        [Obsolete("Use UnsubscribeAsync. Will be removed in future.")]
        void Unsubscribe();
        Task UnsubscribeAsync();

        void Publish(string selector, string message);
        Task PublishAsync(string selector, string message, CancellationToken token=default);
        void PublishRaw(string frame);
        Task PublishRawAsync(string frame, CancellationToken token=default);
        void Pulse();

        Dictionary<string,string> ServerArgs { get; set; }
        Dictionary<string,string> ConnectArgs { get; set; }
    }

    public class SubscriptionInfo
    {
        public DateTime CreatedAt { get; set; }

        public string[] Channels { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        public string SessionId { get; set; }
        public string SubscriptionId { get; set; }
        public string UserAddress { get; set; }
        public bool IsAuthenticated { get; set; }

        public Dictionary<string, string> Meta { get; set; }
        public Dictionary<string, string> ConnectArgs { get; set; }
        public Dictionary<string, string> ServerArgs { get; set; }
    }

    public class MemoryServerEvents : IServerEvents
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(MemoryServerEvents));
        public static bool FlushNopOnSubscription = true;

        public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan HouseKeepingInterval { get; set; } = TimeSpan.FromSeconds(5);

        public Func<IEventSubscription, Task> OnSubscribeAsync { get; set; }
        public Func<IEventSubscription,Task> OnUnsubscribeAsync { get; set; }

        public Func<IEventSubscription, Task> NotifyJoinAsync { get; set; }
        public Func<IEventSubscription, Task> NotifyLeaveAsync { get; set; }
        public Func<IEventSubscription, Task> NotifyUpdateAsync { get; set; }
        public Func<IEventSubscription, Task> NotifyHeartbeatAsync { get; set; }
        public Func<object, string> Serialize { get; set; }

        public bool NotifyChannelOfSubscriptions { get; set; }

        public ConcurrentDictionary<string, IEventSubscription> Subscriptions;
        public ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>> ChannelSubscriptions;
        public ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>> UserIdSubscriptions;
        public ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>> UserNameSubscriptions;
        public ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>> SessionSubscriptions;

        public Action<IEventSubscription, Exception> OnError { get; set; }

        private bool isDisposed;

        public MemoryServerEvents()
        {
            Reset();

            NotifyJoinAsync = s => NotifyChannelsAsync(s.Channels, "cmd.onJoin", s.Meta);
            NotifyLeaveAsync = s => NotifyChannelsAsync(s.Channels, "cmd.onLeave", s.Meta);
            NotifyUpdateAsync = s => NotifyChannelsAsync(s.Channels, "cmd.onUpdate", s.Meta);
            NotifyHeartbeatAsync = s => NotifySubscriptionAsync(s.SubscriptionId, "cmd.onHeartbeat", s.Meta);
            Serialize = o => o?.ToJson();

            var appHost = HostContext.AppHost;
            var feature = appHost?.GetPlugin<ServerEventsFeature>();
            if (feature != null)
            {
                IdleTimeout = feature.IdleTimeout;
                HouseKeepingInterval = feature.HouseKeepingInterval;
                OnSubscribeAsync = feature.OnSubscribeAsync;
                OnUnsubscribeAsync = feature.OnUnsubscribeAsync;
                NotifyChannelOfSubscriptions = feature.NotifyChannelOfSubscriptions;
            }
        }

        public void Reset()
        {
            Subscriptions = new ConcurrentDictionary<string, IEventSubscription>();
            ChannelSubscriptions = new ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>>();
            UserIdSubscriptions = new ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>>();
            UserNameSubscriptions = new ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>>();
            SessionSubscriptions = new ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>>();
        }

        public void Start()
        {
        }

        public void Stop()
        {
            Reset();
        }

        public void NotifyAll(string selector, object message)
        {
            if (isDisposed) return;

            foreach (var sub in Subscriptions.ValuesWithoutLock())
            {
                sub.Publish(selector, Serialize(message));
            }
        }

        public async Task NotifyAllAsync(string selector, object message, CancellationToken token = default)
        {
            foreach (var sub in Subscriptions.ValuesWithoutLock())
            {
                await sub.PublishAsync(selector, Serialize(message), token);
            }
        }

        public void NotifySubscription(string subscriptionId, string selector, object message, string channel = null) =>
            Notify(Subscriptions, subscriptionId, selector, message, channel);

        public Task NotifySubscriptionAsync(string subscriptionId, string selector, object message, string channel = null, CancellationToken token = default) =>
            NotifyAsync(Subscriptions, subscriptionId, selector, message, channel, token);

        public void NotifyChannels(string[] channels, string selector, Dictionary<string, string> meta)
        {
            if (isDisposed) return;

            foreach (var channel in channels)
            {
                NotifyChannel(channel, selector, meta);
            }
        }

        public async Task NotifyChannelsAsync(string[] channels, string selector, Dictionary<string, string> meta, CancellationToken token = default)
        {
            if (isDisposed) return;

            foreach (var channel in channels)
            {
                await NotifyChannelAsync(channel, selector, meta, token);
            }
        }

        public void NotifyChannel(string channel, string selector, object message) =>
            Notify(ChannelSubscriptions, channel, channel + "@" + selector, message, channel);

        public Task NotifyChannelAsync(string channel, string selector, object message, CancellationToken token = default) =>
            NotifyAsync(ChannelSubscriptions, channel, channel + "@" + selector, message, channel, token);

        public void NotifyUserId(string userId, string selector, object message, string channel = null) =>
            Notify(UserIdSubscriptions, userId, selector, message, channel);

        public Task NotifyUserIdAsync(string userId, string selector, object message, string channel = null, CancellationToken token = default) =>
            NotifyAsync(UserIdSubscriptions, userId, selector, message, channel, token);


        public void NotifyUserName(string userName, string selector, object message, string channel = null) =>
            Notify(UserNameSubscriptions, userName, selector, message, channel);

        public Task NotifyUserNameAsync(string userName, string selector, object message, string channel = null, CancellationToken token = default) =>
            NotifyAsync(UserNameSubscriptions, userName, selector, message, channel, token);

        public void NotifySession(string sessionId, string selector, object message, string channel = null) => 
            Notify(SessionSubscriptions, sessionId, selector, message, channel);

        public Task NotifySessionAsync(string sessionId, string selector, object message, string channel = null, CancellationToken token = default) =>
            NotifyAsync(SessionSubscriptions, sessionId, selector, message, channel, token);


        // Send Update Notification
        readonly ConcurrentBag<IEventSubscription> pendingSubscriptionUpdates = new ConcurrentBag<IEventSubscription>();

        // Full Unsubscription + Notifications
        readonly ConcurrentBag<IEventSubscription> pendingUnSubscriptions = new ConcurrentBag<IEventSubscription>();
        
        // Just Unsubscribe
        readonly ConcurrentBag<IEventSubscription> expiredSubs = new ConcurrentBag<IEventSubscription>();
        
        // Generic Async Tasks
        readonly ConcurrentBag<Func<Task>> pendingAsyncTasks = new ConcurrentBag<Func<Task>>();
        
        public void QueueAsyncTask(Func<Task> task)
        {
            pendingAsyncTasks.Add(task);
        }

        async Task DoAsyncTasks(CancellationToken token = default)
        {
            if (pendingAsyncTasks.IsEmpty && pendingSubscriptionUpdates.IsEmpty && pendingUnSubscriptions.IsEmpty && expiredSubs.IsEmpty)
                return;
            
            while (!pendingAsyncTasks.IsEmpty)
            {
                if (pendingAsyncTasks.TryTake(out var asyncTask))
                {
                    await asyncTask();
                }
            }
            
            while (!pendingSubscriptionUpdates.IsEmpty)
            {
                if (pendingSubscriptionUpdates.TryTake(out var sub))
                {
                    if (NotifyUpdateAsync != null)
                        await NotifyUpdateAsync(sub);
                }
            }
            
            while (!pendingUnSubscriptions.IsEmpty)
            {
                if (pendingUnSubscriptions.TryTake(out var subscription))
                {
                    if (OnUnsubscribeAsync != null)
                        await OnUnsubscribeAsync(subscription);

                    subscription.Dispose();

                    if (NotifyChannelOfSubscriptions && subscription.Channels != null && NotifyLeaveAsync != null)
                        await NotifyLeaveAsync(subscription);
                }
            }
            
            while (!expiredSubs.IsEmpty)
            {
                if (expiredSubs.TryTake(out var sub))
                {
                    await sub.UnsubscribeAsync();
                }
            }
        }

        protected void Notify(ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>> map, string key, 
            string selector, object message, string channel = null)
        {
            if (isDisposed) 
                return;

            var subs = map.TryGet(key);
            if (subs == null)
                return;

            var now = DateTime.UtcNow;

            foreach (var sub in subs.KeysWithoutLock())
            {
                if (sub.HasChannel(channel))
                {
                    if (now - sub.LastPulseAt > IdleTimeout || sub.IsClosed)
                    {
                        if (Log.IsDebugEnabled)
                            Log.DebugFormat("[SSE-SERVER] Expired {0} Sub {1} on ({2})", selector, sub.SubscriptionId,
                                string.Join(", ", sub.Channels));

                        expiredSubs.Add(sub);
	                    continue;
                    }

                    if (Log.IsDebugEnabled)
                        Log.DebugFormat("[SSE-SERVER] Sending {0} msg to {1} on ({2})", selector, sub.SubscriptionId,
                            string.Join(", ", sub.Channels));

                    sub.Publish(selector, Serialize(message));
                }
            }
        }

        protected void Notify(ConcurrentDictionary<string, IEventSubscription> map, string key, 
            string selector, object message, string channel = null)
        {
            if (isDisposed) return;

            var sub = map.TryGet(key);
            if (sub == null || !sub.HasChannel(channel))
                return;

            var now = DateTime.UtcNow;
            if (now - sub.LastPulseAt > IdleTimeout)
            {
                if (Log.IsDebugEnabled)
                    Log.DebugFormat("[SSE-SERVER] Expired {0} Sub {1} on ({2})", selector, sub.SubscriptionId,
                        string.Join(", ", sub.Channels));

                expiredSubs.Add(sub);
                return;
            }

            if (Log.IsDebugEnabled)
                Log.DebugFormat("[SSE-SERVER] Sending {0} msg to {1} on ({2})", selector, sub.SubscriptionId,
                    string.Join(", ", sub.Channels));

            sub.Publish(selector, Serialize(message));
        }

        protected async Task NotifyAsync(ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>> map, string key, 
            string selector, object message, string channel = null, CancellationToken token=default)
        {
            if (isDisposed) 
                return;

            var subs = map.TryGet(key);
            if (subs == null)
                return;

            var now = DateTime.UtcNow;

            foreach (var sub in subs.KeysWithoutLock())
            {
                if (sub.HasChannel(channel))
                {
                    if (now - sub.LastPulseAt > IdleTimeout || sub.IsClosed)
                    {
                        if (Log.IsDebugEnabled)
                            Log.DebugFormat("[SSE-SERVER] Expired {0} Sub {1} on ({2})", selector, sub.SubscriptionId,
                                string.Join(", ", sub.Channels));

                        expiredSubs.Add(sub);
                        continue;
                    }

                    if (Log.IsDebugEnabled)
                        Log.DebugFormat("[SSE-SERVER] Sending {0} msg to {1} on ({2})", selector, sub.SubscriptionId,
                            string.Join(", ", sub.Channels));

                    await sub.PublishAsync(selector, Serialize(message), token);
                }
            }
            await DoAsyncTasks(token);
        }

        protected async Task NotifyAsync(ConcurrentDictionary<string, IEventSubscription> map, string key, 
            string selector, object message, string channel = null, CancellationToken token=default)
        {
            if (isDisposed) 
                return;

            var sub = map.TryGet(key);
            if (sub == null || !sub.HasChannel(channel))
                return;

            var now = DateTime.UtcNow;
            if (now - sub.LastPulseAt > IdleTimeout)
            {
                if (Log.IsDebugEnabled)
                    Log.DebugFormat("[SSE-SERVER] Expired {0} Sub {1} on ({2})", selector, sub.SubscriptionId,
                        string.Join(", ", sub.Channels));

                expiredSubs.Add(sub);
                return;
            }

            if (Log.IsDebugEnabled)
                Log.DebugFormat("[SSE-SERVER] Sending {0} msg to {1} on ({2})", selector, sub.SubscriptionId,
                    string.Join(", ", sub.Channels));

            await sub.PublishAsync(selector, Serialize(message), token);
            await DoAsyncTasks(token);
        }

        protected async Task FlushNopAsync(ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>> map,
            string key, string channel = null, CancellationToken token=default)
        {
            var subs = map.TryGet(key);
            if (subs == null)
                return;

            var now = DateTime.UtcNow;

            foreach (var sub in subs.KeysWithoutLock())
            {
                if (sub.HasChannel(channel))
                {
                    if (now - sub.LastPulseAt > IdleTimeout)
                    {
                        expiredSubs.Add(sub);
                    }
                    await sub.PublishRawAsync("\n", token);
                }
            }
            await DoAsyncTasks(token);
        }

        public bool Pulse(string id)
        {
            if (isDisposed) 
                return false;

            var sub = GetSubscription(id);
            if (sub == null)
                return false;
            sub.Pulse();

            if (NotifyHeartbeatAsync != null)
                pendingAsyncTasks.Add(() => NotifyHeartbeatAsync(sub));

            return true;
        }

        public async Task<bool> PulseAsync(string id, CancellationToken token=default)
        {
            if (isDisposed) return false;

            var sub = GetSubscription(id);
            if (sub == null)
                return false;
            sub.Pulse();

            if (NotifyHeartbeatAsync != null)
                await NotifyHeartbeatAsync(sub);

            return true;
        }

        public IEventSubscription GetSubscription(string id)
        {
            if (id == null)
                return null;

            var sub = Subscriptions.TryGet(id);
            return sub;
        }

        public SubscriptionInfo GetSubscriptionInfo(string id)
        {
            return GetSubscription(id).GetInfo();
        }

        public List<SubscriptionInfo> GetSubscriptionInfosByUserId(string userId)
        {
            var subInfos = new List<SubscriptionInfo>();
            if (userId == null) return subInfos;

            var subs = UserIdSubscriptions.TryGet(userId);
            if (subs == null)
            {
                return subInfos;
            }

            foreach (var sub in subs.KeysWithoutLock())
            {
                var info = sub.GetInfo();
                if (info != null)
                    subInfos.Add(info);
            }

            return subInfos;
        }

        readonly ConcurrentDictionary<string, long> SequenceCounters = new ConcurrentDictionary<string, long>();

        public long GetNextSequence(string sequenceId)
        {
            return SequenceCounters.AddOrUpdate(sequenceId, 1, (id, count) => count + 1);
        }

        private long lastCleanAtTicks = DateTime.UtcNow.Ticks;
        private DateTime LastCleanAt
        {
            get => new DateTime(Interlocked.Read(ref lastCleanAtTicks), DateTimeKind.Utc);
            set => Interlocked.Exchange(ref lastCleanAtTicks, value.Ticks);
        }

        public int RemoveExpiredSubscriptions()
        {
            var now = DateTime.UtcNow;
            if (now - LastCleanAt <= HouseKeepingInterval)
                return -1;

            LastCleanAt = now;
            var count = 0;
            foreach (var sub in Subscriptions.ValuesWithoutLock())
            {
                if (now - sub.LastPulseAt > IdleTimeout)
                {
                    count++;
                    expiredSubs.Add(sub);
                }
            }

            return count;
        }

        public async Task<int> RemoveExpiredSubscriptionsAsync(CancellationToken token = default)
        {
            var count = RemoveExpiredSubscriptions();
            await DoAsyncTasks(token);
            return count;
        }

        public void SubscribeToChannels(string subscriptionId, string[] channels)
        {
            if (isDisposed) return;

            if (subscriptionId == null)
                throw new ArgumentNullException(nameof(subscriptionId));
            if (channels == null)
                throw new ArgumentNullException(nameof(channels));

            var sub = GetSubscription(subscriptionId);
            if (sub == null || channels.Length == 0)
                return;

            lock (sub)
            {
                var subChannels = sub.Channels.ToList();
                foreach (var channel in channels)
                {
                    if (subChannels.Contains(channel))
                        continue;

                    subChannels.Add(channel);
                    RegisterSubscription(sub, channel, ChannelSubscriptions);
                }

                sub.UpdateChannels(subChannels.ToArray());
            }

            if (NotifyChannelOfSubscriptions && NotifyUpdateAsync != null)
                pendingSubscriptionUpdates.Add(sub);
        }

        public Task SubscribeToChannelsAsync(string subscriptionId, string[] channels, CancellationToken token = default)
        {
            SubscribeToChannels(subscriptionId, channels);
            
            return DoAsyncTasks(token);
        }

        public void UnsubscribeFromChannels(string subscriptionId, string[] channels)
        {
            if (isDisposed) return;

            if (subscriptionId == null)
                throw new ArgumentNullException(nameof(subscriptionId));
            if (channels == null)
                throw new ArgumentNullException(nameof(channels));

            var sub = GetSubscription(subscriptionId);
            if (sub == null || channels.Length == 0)
                return;

            lock (sub)
            {
                foreach (var channel in channels)
                {
                    if (!sub.Channels.Contains(channel))
                        continue;

                    UnRegisterSubscription(sub, channel, ChannelSubscriptions);
                }

                var subChannels = sub.Channels.ToList();
                subChannels.RemoveAll(channels.Contains);

                sub.UpdateChannels(subChannels.ToArray());

                if (NotifyChannelOfSubscriptions && NotifyUpdateAsync != null)
                    pendingSubscriptionUpdates.Add(sub);
            }
        }

        public Task UnsubscribeFromChannelsAsync(string subscriptionId, string[] channels, CancellationToken token = default)
        {
            UnsubscribeFromChannels(subscriptionId, channels);

            return DoAsyncTasks(token);
        }

        public List<Dictionary<string, string>> GetSubscriptionsDetails(params string[] channels)
        {
            var ret = new List<Dictionary<string, string>>();
            var alreadyAdded = new HashSet<string>();

            foreach (var channel in channels)
            {
                var subs = ChannelSubscriptions.TryGet(channel);
                if (subs == null)
                    continue;

                foreach (var sub in subs.KeysWithoutLock())
                {
                    if (alreadyAdded.Contains(sub.SubscriptionId))
                        continue;

                    ret.Add(sub.Meta);
                    alreadyAdded.Add(sub.SubscriptionId);
                }
            }

            return ret;
        }

        public List<Dictionary<string, string>> GetAllSubscriptionsDetails()
        {
            var ret = new List<Dictionary<string, string>>();
            foreach (var sub in Subscriptions.ValuesWithoutLock())
            {
                ret.Add(sub.Meta);
            }
            return ret;
        }

        public List<SubscriptionInfo> GetAllSubscriptionInfos()
        {
            var ret = new List<SubscriptionInfo>();
            foreach (var sub in Subscriptions.ValuesWithoutLock())
            {
                ret.Add(sub.GetInfo());
            }
            return ret;
        }

        public async Task RegisterAsync(IEventSubscription subscription, Dictionary<string, string> connectArgs = null, CancellationToken token=default)
        {
            if (isDisposed) 
                return;

            try
            {
                var asyncTasks = new List<Func<Task>>();
                
                lock (subscription)
                {
                    if (connectArgs != null)
                        asyncTasks.Add(() => subscription.PublishAsync("cmd.onConnect", connectArgs.ToJson(), token));

                    subscription.OnUnsubscribeAsync = HandleUnsubscriptionAsync;
                    foreach (string channel in subscription.Channels ?? EventSubscription.UnknownChannel)
                    {
                        RegisterSubscription(subscription, channel, ChannelSubscriptions);
                    }
                    RegisterSubscription(subscription, subscription.SubscriptionId, Subscriptions);
                    RegisterSubscription(subscription, subscription.UserId, UserIdSubscriptions);
                    RegisterSubscription(subscription, subscription.UserName, UserNameSubscriptions);
                    RegisterSubscription(subscription, subscription.SessionId, SessionSubscriptions);

                    if (OnSubscribeAsync != null)
                        asyncTasks.Add(() => OnSubscribeAsync(subscription));

                    if (NotifyChannelOfSubscriptions && subscription.Channels != null && NotifyJoinAsync != null)
                        asyncTasks.Add(() => NotifyJoinAsync(subscription));
                    else if (FlushNopOnSubscription)
                        asyncTasks.Add(() => FlushNopToChannelsAsync(subscription.Channels, token));
                }

                foreach (var asyncTask in asyncTasks)
                {
                    await asyncTask();
                }
            }
            catch (Exception ex)
            {
                Log.Error("Register: " + ex.Message, ex);
                OnError?.Invoke(subscription, ex);

                throw;
            }
        }

        public async Task FlushNopToChannelsAsync(string[] channels, CancellationToken token=default)
        {
            if (isDisposed) return;

            //For some yet-to-be-determined reason we need to send something to all channels to determine
            //which subscriptions are no longer connected so we can dispose of them right then and there.
            //Failing to do this for 10 simultaneous requests on Local IIS will hang the entire Website instance
            //ref: https://forums.servicestack.net/t/serversentevents-with-notifychannelofsubscriptions-set-to-false-leaks-requests/2552/2
            foreach (var channel in channels)
            {
                await FlushNopAsync(ChannelSubscriptions, channel, channel, token);
            }
        }

        void RegisterSubscription(IEventSubscription subscription, string key,
            ConcurrentDictionary<string, IEventSubscription> map)
        {
            if (key == null || subscription == null)
                return;

            map.TryAdd(key, subscription);
        }

        void RegisterSubscription(IEventSubscription subscription, string key,
            ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>> map)
        {
            if (key == null || subscription == null)
                return;

            var subs = map.GetOrAdd(key, k => new ConcurrentDictionary<IEventSubscription, bool>());
            subs.TryAdd(subscription, true);
        }

        public void UnRegister(string subscriptionId, CancellationToken token=default)
        {
            var subscription = GetSubscription(subscriptionId);
            if (subscription == null)
                return;

            HandleUnsubscription(subscription);
        }
        
        void HandleUnsubscription(IEventSubscription subscription)
        {
            if (isDisposed) 
                return;

            lock (subscription)
            {
                foreach (var channel in subscription.Channels ?? EventSubscription.UnknownChannel)
                {
                    UnRegisterSubscription(subscription, channel, ChannelSubscriptions);
                }
                
                UnRegisterSubscription(subscription, subscription.SubscriptionId, Subscriptions);
                UnRegisterSubscription(subscription, subscription.UserId, UserIdSubscriptions);
                UnRegisterSubscription(subscription, subscription.UserName, UserNameSubscriptions);
                UnRegisterSubscription(subscription, subscription.SessionId, SessionSubscriptions);
            }
            
            pendingUnSubscriptions.Add(subscription);
        }

        public Task UnRegisterAsync(string subscriptionId, CancellationToken token=default)
        {
            var subscription = GetSubscription(subscriptionId);
            if (subscription == null)
                return TypeConstants.EmptyTask;

            return HandleUnsubscriptionAsync(subscription, token);
        }

        Task HandleUnsubscriptionAsync(IEventSubscription subscription) => HandleUnsubscriptionAsync(subscription, CancellationToken.None);
        async Task HandleUnsubscriptionAsync(IEventSubscription subscription, CancellationToken token)
        {
            if (isDisposed) 
                return;

            HandleUnsubscription(subscription);

            await DoAsyncTasks(token);
        }

        void UnRegisterSubscription(IEventSubscription subscription, string key,
            ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>> map)
        {
            if (key == null || subscription == null)
                return;

            try
            {
                var subs = map.TryGet(key);
                if (subs == null)
                    return;

                subs.TryRemove(subscription, out bool _);
            }
            catch (Exception ex)
            {
                Log.Error("UnRegisterSubscription: " + ex.Message, ex);
                OnError?.Invoke(subscription, ex);
                throw;
            }
        }

        void UnRegisterSubscription(IEventSubscription subscription, string key,
            ConcurrentDictionary<string, IEventSubscription> map)
        {
            if (key == null || subscription == null)
                return;

            map.TryRemove(key, out _);
        }

        public async Task DisposeAsync()
        {
            if (isDisposed) return;
            isDisposed = true;

            var allSubs = Subscriptions.ValuesWithoutLock().ToArray();
            foreach (var sub in allSubs)
            {
                await sub.UnsubscribeAsync();
            }

            Reset();
        }
        
        public void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;

            TaskExt.RunSync(DisposeAsync);
        }
    }

    public interface IWriteEvent
    {
        void WriteEvent(string msg);
    }

    public interface IWriteEventAsync
    {
        Task WriteEventAsync(string msg, CancellationToken token = default);
    }

    public interface IServerEvents : IDisposable
    {
        // External API's
        void NotifyAll(string selector, object message);
        Task NotifyAllAsync(string selector, object message, CancellationToken token=default);

        void NotifyChannel(string channel, string selector, object message);
        Task NotifyChannelAsync(string channel, string selector, object message, CancellationToken token=default);

        void NotifySubscription(string subscriptionId, string selector, object message, string channel = null);
        Task NotifySubscriptionAsync(string subscriptionId, string selector, object message, string channel = null, CancellationToken token=default);

        void NotifyUserId(string userId, string selector, object message, string channel = null);
        Task NotifyUserIdAsync(string userId, string selector, object message, string channel = null, CancellationToken token=default);

        void NotifyUserName(string userName, string selector, object message, string channel = null);
        Task NotifyUserNameAsync(string userName, string selector, object message, string channel = null, CancellationToken token=default);

        void NotifySession(string sessionId, string selector, object message, string channel = null);
        Task NotifySessionAsync(string sessionId, string selector, object message, string channel = null, CancellationToken token=default);

        SubscriptionInfo GetSubscriptionInfo(string id);

        List<SubscriptionInfo> GetSubscriptionInfosByUserId(string userId);

        List<SubscriptionInfo> GetAllSubscriptionInfos();

        // Admin API's
        Task RegisterAsync(IEventSubscription subscription, Dictionary<string, string> connectArgs = null, CancellationToken token=default);

        Task UnRegisterAsync(string subscriptionId, CancellationToken token=default);

        long GetNextSequence(string sequenceId);

        int RemoveExpiredSubscriptions();
        Task<int> RemoveExpiredSubscriptionsAsync(CancellationToken token = default);

        void SubscribeToChannels(string subscriptionId, string[] channels);
        Task SubscribeToChannelsAsync(string subscriptionId, string[] channels, CancellationToken token=default);

        void UnsubscribeFromChannels(string subscriptionId, string[] channels);
        Task UnsubscribeFromChannelsAsync(string subscriptionId, string[] channels, CancellationToken token=default);

        void QueueAsyncTask(Func<Task> task);

        // Client API's
        List<Dictionary<string, string>> GetSubscriptionsDetails(params string[] channels);

        List<Dictionary<string, string>> GetAllSubscriptionsDetails();

        Task<bool> PulseAsync(string subscriptionId, CancellationToken token=default);

        // Clear all Registrations
        void Reset();
        void Start();
        void Stop();
    }

    public static class Selector
    {
        public static string Id(Type type) => "cmd." + type.Name;

        public static string Id<T>() => "cmd." + typeof(T).Name;
    }

    public static class ServerEventExtensions
    {
        public static SubscriptionInfo GetInfo(this IEventSubscription sub)
        {
            if (sub == null)
                return null;

            return new SubscriptionInfo
            {
                CreatedAt = sub.CreatedAt,
                Channels = sub.Channels,
                UserId = sub.UserId,
                UserName = sub.UserName,
                DisplayName = sub.DisplayName,
                SessionId = sub.SessionId,
                SubscriptionId = sub.SubscriptionId,
                UserAddress = sub.UserAddress,
                IsAuthenticated = sub.IsAuthenticated,
                Meta = sub.Meta,
                ConnectArgs = sub.ConnectArgs,
                ServerArgs = sub.ServerArgs,
            };
        }

        public static bool HasChannel(this IEventSubscription sub, string channel)
        {
            return sub != null && (channel == null || Array.IndexOf(sub.Channels, channel) >= 0);
        }

        public static bool HasAnyChannel(this IEventSubscription sub, string[] channels)
        {
            if (sub == null || channels == null)
                return false;

            foreach (var channel in channels)
            {
                if (sub.HasChannel(channel))
                    return true;
            }

            return false;
        }

        public static void NotifyAll(this IServerEvents server, object message) => 
            server.NotifyAll(Selector.Id(message.GetType()), message);
        public static Task NotifyAllAsync(this IServerEvents server, object message, CancellationToken token=default) => 
            server.NotifyAllAsync(Selector.Id(message.GetType()), message, token);

        public static void NotifyChannel(this IServerEvents server, string channel, object message) => 
            server.NotifyChannel(channel, Selector.Id(message.GetType()), message);

        public static Task NotifyChannelAsync(this IServerEvents server, string channel, object message, CancellationToken token=default) => 
            server.NotifyChannelAsync(channel, Selector.Id(message.GetType()), message, token);

        public static void NotifySubscription(this IServerEvents server, string subscriptionId, object message, string channel = null) => 
            server.NotifySubscription(subscriptionId, Selector.Id(message.GetType()), message, channel);

        public static Task NotifySubscriptionAsync(this IServerEvents server, string subscriptionId, object message, string channel = null, CancellationToken token=default) => 
            server.NotifySubscriptionAsync(subscriptionId, Selector.Id(message.GetType()), message, channel, token);

        public static void NotifyUserId(this IServerEvents server, string userId, object message, string channel = null) => 
            server.NotifyUserId(userId, Selector.Id(message.GetType()), message, channel);

        public static Task NotifyUserIdAsync(this IServerEvents server, string userId, object message, string channel = null, CancellationToken token=default) => 
            server.NotifyUserIdAsync(userId, Selector.Id(message.GetType()), message, channel, token);

        public static void NotifyUserName(this IServerEvents server, string userName, object message, string channel = null) => 
            server.NotifyUserName(userName, Selector.Id(message.GetType()), message, channel);

        public static Task NotifyUserNameAsync(this IServerEvents server, string userName, object message, string channel = null, CancellationToken token=default) => 
            server.NotifyUserNameAsync(userName, Selector.Id(message.GetType()), message, channel, token);

        public static void NotifySession(this IServerEvents server, string sspid, object message, string channel = null) => 
            server.NotifySession(sspid, Selector.Id(message.GetType()), message, channel);

        public static Task NotifySessionAsync(this IServerEvents server, string sspid, object message, string channel = null, CancellationToken token=default) => 
            server.NotifySessionAsync(sspid, Selector.Id(message.GetType()), message, channel, token);

        internal static TElement TryGet<TKey, TElement>(this ConcurrentDictionary<TKey, TElement> dic, TKey key)
        {
            if (dic == null || key == null)
                return default(TElement);

            dic.TryGetValue(key, out var res);
            return res;
        }

        internal static IEnumerable<TElement> ValuesWithoutLock<TKey, TElement>(this ConcurrentDictionary<TKey, TElement> source)
        {
            foreach (var item in source)
            {
                if (item.Value != null)
                    yield return item.Value;
            }
        }

        internal static IEnumerable<TKey> KeysWithoutLock<TKey, TElement>(this ConcurrentDictionary<TKey, TElement> source)
        {
            foreach (var item in source)
            {
                yield return item.Key;
            }
        }
    }
}
