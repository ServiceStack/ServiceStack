using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        public Action<IEventSubscription> OnUnsubscribe { get; set; }
        public Action<IEventSubscription, IResponse, string> OnPublish { get; set; }
        public Action<IResponse, string> WriteEvent { get; set; }
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
                var bytes = frame.ToUtf8Bytes();
                res.OutputStream.WriteAsync(bytes, 0, bytes.Length)
                    .Then(_ => res.OutputStream.FlushAsync());
            };

            IdleTimeout = TimeSpan.FromSeconds(30);
            HeartbeatInterval = TimeSpan.FromSeconds(10);
            HouseKeepingInterval = TimeSpan.FromSeconds(5);

            NotifyChannelOfSubscriptions = true;
            ValidateUserAddress = true;
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
                    OnSubscribe = OnSubscribe,
                    OnUnsubscribe = OnUnsubscribe,
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

        public override Task ProcessRequestAsync(IRequest req, IResponse res, string operationName)
        {
            if (HostContext.ApplyCustomHandlerRequestFilters(req, res))
                return TypeConstants.EmptyTask;

            var feature = HostContext.GetPlugin<ServerEventsFeature>();

            var session = req.GetSession();
            if (feature.LimitToAuthenticatedUsers && !session.IsAuthenticated)
            {
                session.ReturnFailedAuthentication(req);
                return TypeConstants.EmptyTask;
            }

            res.ContentType = MimeTypes.ServerSentEvents;
            res.AddHeader(HttpHeaders.CacheControl, "no-cache");
            res.ApplyGlobalResponseHeaders();
            res.UseBufferedStream = false;
            res.KeepAlive = true;

            feature.OnInit?.Invoke(req);

            res.Flush();

            var serverEvents = req.TryResolve<IServerEvents>();
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
                OnError = feature.OnError,
                Meta = {
                    { "userId", userId },
                    { "isAuthenticated", session != null && session.IsAuthenticated ? "true": "false" },
                    { "displayName", displayName },
                    { "channels", string.Join(",", channels) },
                    { "createdAt", now.ToUnixTimeMs().ToString() },
                    { AuthMetadataProvider.ProfileUrlKey, session.GetProfileUrl() ?? AuthMetadataProvider.DefaultNoProfileImgUrl },
                },
                ServerArgs = new Dictionary<string, string>(),
            };
            subscription.ConnectArgs = new Dictionary<string, string>(subscription.Meta);

            feature.OnCreated?.Invoke(subscription, req);

            if (req.Response.IsClosed)
                return TypeConstants.EmptyTask; //Allow short-circuiting in OnCreated callback

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
                {"updateSubscriberUrl", req.ResolveAbsoluteUrl("~/event-subscribers/" + subscriptionId) },
                {"heartbeatIntervalMs", ((long)feature.HeartbeatInterval.TotalMilliseconds).ToString(CultureInfo.InvariantCulture) },
                {"idleTimeoutMs", ((long)feature.IdleTimeout.TotalMilliseconds).ToString(CultureInfo.InvariantCulture)}
            };

            feature.OnConnect?.Invoke(subscription, subscription.ConnectArgs);

            serverEvents.Register(subscription, subscription.ConnectArgs);

            var tcs = new TaskCompletionSource<bool>();

            subscription.OnDispose = _ =>
            {
                try
                {
                    res.EndHttpHandlerRequest(skipHeaders: true);
                }
                catch { }
                if (!tcs.Task.IsCompleted)
                    tcs.SetResult(true);
            };

            return tcs.Task;
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

        public override Task ProcessRequestAsync(IRequest req, IResponse res, string operationName)
        {
            if (HostContext.ApplyCustomHandlerRequestFilters(req, res))
                return TypeConstants.EmptyTask;

            res.ApplyGlobalResponseHeaders();

            var serverEvents = req.TryResolve<IServerEvents>();

            serverEvents.RemoveExpiredSubscriptions();

            var feature = HostContext.GetPlugin<ServerEventsFeature>();
            feature.OnHeartbeatInit?.Invoke(req);

            if (req.Response.IsClosed)
                return TypeConstants.EmptyTask;

            var subscriptionId = req.QueryString["id"];
            var subscription = serverEvents.GetSubscriptionInfo(subscriptionId);
            if (subscription == null)
            {
                res.StatusCode = 404;
                res.StatusDescription = ErrorMessages.SubscriptionNotExistsFmt.Fmt(subscriptionId.SafeInput());
                res.EndHttpHandlerRequest(skipHeaders: true);
                return TypeConstants.EmptyTask;
            }

            if (!feature.CanAccessSubscription(req, subscription))
            {
                res.StatusCode = 403;
                res.StatusDescription = "Invalid User Address";
                res.EndHttpHandlerRequest(skipHeaders: true);
                return TypeConstants.EmptyTask;
            }

            if (!serverEvents.Pulse(subscriptionId))
            {
                res.StatusCode = 404;
                res.StatusDescription = ErrorMessages.SubscriptionNotExistsFmt.Fmt(subscriptionId.SafeInput());
            }
            res.EndHttpHandlerRequest(skipHeaders: true);
            return TypeConstants.EmptyTask;
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

        public object Any(UnRegisterEventSubscriber request)
        {
            var subscription = ServerEvents.GetSubscriptionInfo(request.Id);

            if (subscription == null)
                throw HttpError.NotFound(ErrorMessages.SubscriptionNotExistsFmt.Fmt(request.Id).SafeInput());

            var feature = HostContext.GetPlugin<ServerEventsFeature>();
            if (!feature.CanAccessSubscription(base.Request, subscription))
                throw HttpError.Forbidden(ErrorMessages.SubscriptionForbiddenFmt.Fmt(request.Id.SafeInput()));

            ServerEvents.UnRegister(subscription.SubscriptionId);

            return subscription.Meta;
        }

        public object Any(UpdateEventSubscriber request)
        {
            var subscription = ServerEvents.GetSubscriptionInfo(request.Id);

            if (subscription == null)
                throw HttpError.NotFound(ErrorMessages.SubscriptionNotExistsFmt.Fmt(request.Id.SafeInput()));

            var feature = HostContext.GetPlugin<ServerEventsFeature>();
            if (!feature.CanAccessSubscription(base.Request, subscription))
                throw HttpError.Forbidden(ErrorMessages.SubscriptionForbiddenFmt.Fmt(request.Id.SafeInput()));

            if (request.UnsubscribeChannels != null)
                ServerEvents.UnsubscribeFromChannels(subscription.SubscriptionId, request.UnsubscribeChannels);
            if (request.SubscribeChannels != null)
                ServerEvents.SubscribeToChannels(subscription.SubscriptionId, request.SubscribeChannels);

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
            get => new DateTime(Interlocked.Read(ref LastPulseAtTicks), DateTimeKind.Utc);
            set => Interlocked.Exchange(ref LastPulseAtTicks, value.Ticks);
        }

        private long subscribed = 1;

        private readonly IResponse response;
        private long msgId;

        public IResponse Response => this.response;
        public IRequest Request => this.response.Request;

        public long LastMessageId => Interlocked.Read(ref msgId);

        public EventSubscription(IResponse response)
        {
            this.response = response;
            this.Meta = new Dictionary<string, string>();
            this.WriteEvent = HostContext.GetPlugin<ServerEventsFeature>().WriteEvent;
        }

        public void UpdateChannels(string[] channels)
        {
            this.Channels = channels;
            this.Meta["channels"] = string.Join(",", channels);
        }

        public Action<IEventSubscription> OnUnsubscribe { get; set; }
        public Action<IEventSubscription> OnDispose { get; set; }
        public Action<IEventSubscription, IResponse, string> OnPublish { get; set; }
        public Action<IResponse, string> WriteEvent { get; set; }
        public Action<IEventSubscription, Exception> OnError { get; set; }
        public bool IsClosed => this.response.IsClosed;

        public void Publish(string selector)
        {
            Publish(selector, null);
        }

        public void Publish(string selector, string message)
        {
            var msg = message ?? "";
            var frame = "id: " + Interlocked.Increment(ref msgId) + "\n"
                      + "data: " + selector + " " + msg + "\n\n";

            PublishRaw(frame);
        }

        public void PublishRaw(string frame)
        {
            if (response.IsClosed) return;

            try
            {
                lock (response)
                {
                    WriteEvent(response, frame);

                    OnPublish?.Invoke(this, response, frame);
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Could not publish notification to: " + frame.SafeSubstring(0, 50), ex);
                OnError?.Invoke(this, ex);

                // Mono: If we explicitly close OutputStream after the error socket wont leak (response.Close() doesn't work)
                try
                {
                    // This will throw an exception, but on Mono (Linux/OSX) the socket will leak if we not close the OutputStream
                    response.OutputStream.Close();
                }
                catch(Exception innerEx)
                {
                    Log.Error("OutputStream.Close()", innerEx);
                }

                Unsubscribe();
            }
        }

        public void Pulse()
        {
            LastPulseAt = DateTime.UtcNow;
        }

        public void Unsubscribe()
        {
            if (Interlocked.CompareExchange(ref subscribed, 0, 1) == 1)
            {
                var fn = OnUnsubscribe;
                OnUnsubscribe = null;
                fn?.Invoke(this);
            }
            Dispose();
        }

        public void Dispose()
        {
            OnUnsubscribe = null;
            if (response.IsClosed) return;
            try
            {
                lock (response)
                {
                    response.EndHttpHandlerRequest(skipHeaders: true);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error ending subscription response", ex);
            }

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

        Action<IEventSubscription> OnUnsubscribe { get; set; }
        void Unsubscribe();

        void Publish(string selector, string message);
        void PublishRaw(string frame);
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

        public Action<IEventSubscription> OnSubscribe { get; set; }
        public Action<IEventSubscription> OnUnsubscribe { get; set; }

        public Action<IEventSubscription> NotifyJoin { get; set; }
        public Action<IEventSubscription> NotifyLeave { get; set; }
        public Action<IEventSubscription> NotifyUpdate { get; set; }
        public Action<IEventSubscription> NotifyHeartbeat { get; set; }
        public Func<object, string> Serialize { get; set; }

        public bool NotifyChannelOfSubscriptions { get; set; }

        public ConcurrentDictionary<string, IEventSubscription> Subcriptions;
        public ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>> ChannelSubcriptions;
        public ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>> UserIdSubcriptions;
        public ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>> UserNameSubcriptions;
        public ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>> SessionSubcriptions;

        public Action<IEventSubscription, Exception> OnError { get; set; }

        private bool isDisposed;

        public MemoryServerEvents()
        {
            Reset();

            NotifyJoin = s => NotifyChannels(s.Channels, "cmd.onJoin", s.Meta);
            NotifyLeave = s => NotifyChannels(s.Channels, "cmd.onLeave", s.Meta);
            NotifyUpdate = s => NotifyChannels(s.Channels, "cmd.onUpdate", s.Meta);
            NotifyHeartbeat = s => NotifySubscription(s.SubscriptionId, "cmd.onHeartbeat", s.Meta);
            Serialize = o => o?.ToJson();

            var appHost = HostContext.AppHost;
            var feature = appHost?.GetPlugin<ServerEventsFeature>();
            if (feature != null)
            {
                IdleTimeout = feature.IdleTimeout;
                HouseKeepingInterval = feature.HouseKeepingInterval;
                OnSubscribe = feature.OnSubscribe;
                OnUnsubscribe = feature.OnUnsubscribe;
                NotifyChannelOfSubscriptions = feature.NotifyChannelOfSubscriptions;
            }
        }

        public void Reset()
        {
            Subcriptions = new ConcurrentDictionary<string, IEventSubscription>();
            ChannelSubcriptions = new ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>>();
            UserIdSubcriptions = new ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>>();
            UserNameSubcriptions = new ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>>();
            SessionSubcriptions = new ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>>();
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

            foreach (var sub in Subcriptions.ValuesWithoutLock())
            {
                sub.Publish(selector, Serialize(message));
            }
        }

        public void NotifySubscription(string subscriptionId, string selector, object message, string channel = null)
        {
            Notify(Subcriptions, subscriptionId, selector, message, channel);
        }

        public void NotifyChannels(string[] channels, string selector, Dictionary<string, string> meta)
        {
            if (isDisposed) return;

            foreach (var channel in channels)
            {
                NotifyChannel(channel, selector, meta);
            }
        }

        public void NotifyChannel(string channel, string selector, object message)
        {
            Notify(ChannelSubcriptions, channel, channel + "@" + selector, message, channel);
        }

        public void NotifyUserId(string userId, string selector, object message, string channel = null)
        {
            Notify(UserIdSubcriptions, userId, selector, message, channel);
        }

        public void NotifyUserName(string userName, string selector, object message, string channel = null)
        {
            Notify(UserNameSubcriptions, userName, selector, message, channel);
        }

        public void NotifySession(string sspid, string selector, object message, string channel = null)
        {
            Notify(SessionSubcriptions, sspid, selector, message, channel);
        }

        protected void Notify(ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>> map,
            string key, string selector, object message, string channel = null)
        {
            if (isDisposed) return;

            var subs = map.TryGet(key);
            if (subs == null)
                return;

            var expired = new List<IEventSubscription>();
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

                        expired.Add(sub);
	                    continue;
                    }

                    if (Log.IsDebugEnabled)
                        Log.DebugFormat("[SSE-SERVER] Sending {0} msg to {1} on ({2})", selector, sub.SubscriptionId,
                            string.Join(", ", sub.Channels));

                    sub.Publish(selector, Serialize(message));
                }
            }

            foreach (var sub in expired)
            {
                sub.Unsubscribe();
            }
        }

        protected void FlushNop(ConcurrentDictionary<string, ConcurrentDictionary<IEventSubscription, bool>> map,
            string key, string channel = null)
        {
            var subs = map.TryGet(key);
            if (subs == null)
                return;

            var expired = new List<IEventSubscription>();
            var now = DateTime.UtcNow;

            foreach (var sub in subs.KeysWithoutLock())
            {
                if (sub.HasChannel(channel))
                {
                    if (now - sub.LastPulseAt > IdleTimeout)
                    {
                        expired.Add(sub);
                    }
                    sub.PublishRaw("\n");
                }
            }

            foreach (var sub in expired)
            {
                sub.Unsubscribe();
            }
        }

        protected void Notify(ConcurrentDictionary<string, IEventSubscription> map, string key, string selector,
            object message, string channel = null)
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

                sub.Unsubscribe();
                return;
            }

            if (Log.IsDebugEnabled)
                Log.DebugFormat("[SSE-SERVER] Sending {0} msg to {1} on ({2})", selector, sub.SubscriptionId,
                    string.Join(", ", sub.Channels));

            sub.Publish(selector, Serialize(message));
        }

        public bool Pulse(string id)
        {
            if (isDisposed) return false;

            var sub = GetSubscription(id);
            if (sub == null)
                return false;
            sub.Pulse();

            NotifyHeartbeat?.Invoke(sub);

            return true;
        }

        public IEventSubscription GetSubscription(string id)
        {
            if (id == null)
                return null;

            var sub = Subcriptions.TryGet(id);
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

            var subs = UserIdSubcriptions.TryGet(userId);
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

            var expired = new List<IEventSubscription>();
            LastCleanAt = now;
            foreach (var sub in Subcriptions.ValuesWithoutLock())
            {
                if (now - sub.LastPulseAt > IdleTimeout)
                {
                    expired.Add(sub);
                }
            }

            foreach (var sub in expired)
            {
                sub.Unsubscribe();
            }

            return expired.Count;
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
                    RegisterSubscription(sub, channel, ChannelSubcriptions);
                }

                sub.UpdateChannels(subChannels.ToArray());

                if (NotifyChannelOfSubscriptions)
                    NotifyUpdate?.Invoke(sub);
            }
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

                    UnRegisterSubscription(sub, channel, ChannelSubcriptions);
                }

                var subChannels = sub.Channels.ToList();
                subChannels.RemoveAll(channels.Contains);

                sub.UpdateChannels(subChannels.ToArray());

                if (NotifyChannelOfSubscriptions)
                    NotifyUpdate?.Invoke(sub);
            }
        }

        public List<Dictionary<string, string>> GetSubscriptionsDetails(params string[] channels)
        {
            var ret = new List<Dictionary<string, string>>();
            var alreadyAdded = new HashSet<string>();

            foreach (var channel in channels)
            {
                var subs = ChannelSubcriptions.TryGet(channel);
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
            foreach (var sub in Subcriptions.ValuesWithoutLock())
            {
                ret.Add(sub.Meta);
            }
            return ret;
        }

        public List<SubscriptionInfo> GetAllSubscriptionInfos()
        {
            var ret = new List<SubscriptionInfo>();
            foreach (var sub in Subcriptions.ValuesWithoutLock())
            {
                ret.Add(sub.GetInfo());
            }
            return ret;
        }

        public void Register(IEventSubscription subscription, Dictionary<string, string> connectArgs = null)
        {
            if (isDisposed) return;

            try
            {
                lock (subscription)
                {
                    if (connectArgs != null)
                        subscription.Publish("cmd.onConnect", connectArgs.ToJson());

                    subscription.OnUnsubscribe = HandleUnsubscription;
                    foreach (string channel in subscription.Channels ?? EventSubscription.UnknownChannel)
                    {
                        RegisterSubscription(subscription, channel, ChannelSubcriptions);
                    }
                    RegisterSubscription(subscription, subscription.SubscriptionId, Subcriptions);
                    RegisterSubscription(subscription, subscription.UserId, UserIdSubcriptions);
                    RegisterSubscription(subscription, subscription.UserName, UserNameSubcriptions);
                    RegisterSubscription(subscription, subscription.SessionId, SessionSubcriptions);

                    OnSubscribe?.Invoke(subscription);

                    if (NotifyChannelOfSubscriptions && subscription.Channels != null && NotifyJoin != null)
                        NotifyJoin(subscription);
                    else if (FlushNopOnSubscription)
                        FlushNopToChannels(subscription.Channels);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Register: " + ex.Message, ex);
                OnError?.Invoke(subscription, ex);

                throw;
            }
        }

        public void FlushNopToChannels(string[] channels)
        {
            if (isDisposed) return;

            //For some yet-to-be-determined reason we need to send something to all channels to determine
            //which subscriptions are no longer connected so we can dispose of them right then and there.
            //Failing to do this for 10 simultaneous requests on Local IIS will hang the entire Website instance
            //ref: https://forums.servicestack.net/t/serversentevents-with-notifychannelofsubscriptions-set-to-false-leaks-requests/2552/2
            foreach (var channel in channels)
            {
                FlushNop(ChannelSubcriptions, channel, channel);
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

        public void UnRegister(string subscriptionId)
        {
            var subscription = GetSubscription(subscriptionId);
            if (subscription == null)
                return;

            HandleUnsubscription(subscription);
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

                bool flag;
                subs.TryRemove(subscription, out flag);
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

            IEventSubscription inMap;
            map.TryRemove(key, out inMap);
        }

        void HandleUnsubscription(IEventSubscription subscription)
        {
            if (isDisposed) return;

            lock (subscription)
            {
                foreach (var channel in subscription.Channels ?? EventSubscription.UnknownChannel)
                {
                    UnRegisterSubscription(subscription, channel, ChannelSubcriptions);
                }
                UnRegisterSubscription(subscription, subscription.SubscriptionId, Subcriptions);
                UnRegisterSubscription(subscription, subscription.UserId, UserIdSubcriptions);
                UnRegisterSubscription(subscription, subscription.UserName, UserNameSubcriptions);
                UnRegisterSubscription(subscription, subscription.SessionId, SessionSubcriptions);

                OnUnsubscribe?.Invoke(subscription);

                subscription.Dispose();

                if (NotifyChannelOfSubscriptions && subscription.Channels != null)
                    NotifyLeave?.Invoke(subscription);
            }
        }

        public void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;
            Reset();
        }
    }

    public interface IServerEvents : IDisposable
    {
        // External API's
        void NotifyAll(string selector, object message);

        void NotifyChannel(string channel, string selector, object message);

        void NotifySubscription(string subscriptionId, string selector, object message, string channel = null);

        void NotifyUserId(string userId, string selector, object message, string channel = null);

        void NotifyUserName(string userName, string selector, object message, string channel = null);

        void NotifySession(string sspid, string selector, object message, string channel = null);

        SubscriptionInfo GetSubscriptionInfo(string id);

        List<SubscriptionInfo> GetSubscriptionInfosByUserId(string userId);

        List<SubscriptionInfo> GetAllSubscriptionInfos();

        // Admin API's
        void Register(IEventSubscription subscription, Dictionary<string, string> connectArgs = null);

        void UnRegister(string subscriptionId);

        long GetNextSequence(string sequenceId);

        int RemoveExpiredSubscriptions();

        void SubscribeToChannels(string subscriptionId, string[] channels);

        void UnsubscribeFromChannels(string subscriptionId, string[] channels);

        // Client API's
        List<Dictionary<string, string>> GetSubscriptionsDetails(params string[] channels);

        List<Dictionary<string, string>> GetAllSubscriptionsDetails();

        bool Pulse(string subscriptionId);

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

        public static void NotifyAll(this IServerEvents server, object message)
        {
            server.NotifyAll(Selector.Id(message.GetType()), message);
        }

        public static void NotifyChannel(this IServerEvents server, string channel, object message)
        {
            server.NotifyChannel(channel, Selector.Id(message.GetType()), message);
        }

        public static void NotifySubscription(this IServerEvents server, string subscriptionId, object message, string channel = null)
        {
            server.NotifySubscription(subscriptionId, Selector.Id(message.GetType()), message, channel);
        }

        public static void NotifyUserId(this IServerEvents server, string userId, object message, string channel = null)
        {
            server.NotifyUserId(userId, Selector.Id(message.GetType()), message, channel);
        }

        public static void NotifyUserName(this IServerEvents server, string userName, object message, string channel = null)
        {
            server.NotifyUserName(userName, Selector.Id(message.GetType()), message, channel);
        }

        public static void NotifySession(this IServerEvents server, string sspid, object message, string channel = null)
        {
            server.NotifySession(sspid, Selector.Id(message.GetType()), message, channel);
        }

        internal static TElement TryGet<TKey, TElement>(this ConcurrentDictionary<TKey, TElement> dic, TKey key)
        {
            if (dic == null || key == null)
                return default(TElement);

            TElement res;
            dic.TryGetValue(key, out res);
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
