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

        public Action<IRequest> OnInit { get; set; }
        public Action<IRequest> OnHeartbeatInit { get; set; }
        public Action<IEventSubscription, IRequest> OnCreated { get; set; }
        public Action<IEventSubscription, Dictionary<string, string>> OnConnect { get; set; }
        public Action<IEventSubscription> OnSubscribe { get; set; }
        public Action<IEventSubscription> OnUnsubscribe { get; set; }
        public Action<IResponse, string> OnPublish { get; set; }
        public bool NotifyChannelOfSubscriptions { get; set; }
        public bool LimitToAuthenticatedUsers { get; set; }
        public bool ValidateUserAddress { get; set; }

        public ServerEventsFeature()
        {
            StreamPath = "/event-stream";
            HeartbeatPath = "/event-heartbeat";
            UnRegisterPath = "/event-unregister";
            SubscribersPath = "/event-subscribers";

            IdleTimeout = TimeSpan.FromSeconds(30);
            HeartbeatInterval = TimeSpan.FromSeconds(10);

            NotifyChannelOfSubscriptions = true;
            ValidateUserAddress = true;
        }

        public void Register(IAppHost appHost)
        {
            var broker = new MemoryServerEvents
            {
                IdleTimeout = IdleTimeout,
                OnSubscribe = OnSubscribe,
                OnUnsubscribe = OnUnsubscribe,
                NotifyChannelOfSubscriptions = NotifyChannelOfSubscriptions,
            };
            var container = appHost.GetContainer();

            if (container.TryResolve<IServerEvents>() == null)
                container.Register<IServerEvents>(broker);

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

        public bool CanAccessSubscription(IRequest req, string subscriptionId)
        {
            if (!ValidateUserAddress)
                return true;

            var sub = req.TryResolve<IServerEvents>().GetSubscriptionInfo(subscriptionId);
            return sub.UserAddress == req.UserHostAddress;
        }

        public bool CanAccessSubscription(IRequest req, SubscriptionInfo sub)
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
                return EmptyTask;

            var feature = HostContext.GetPlugin<ServerEventsFeature>();

            var session = req.GetSession();
            if (feature.LimitToAuthenticatedUsers && !session.IsAuthenticated)
            {
                session.ReturnFailedAuthentication(req);
                return EmptyTask;
            }

            res.ContentType = MimeTypes.ServerSentEvents;
            res.AddHeader(HttpHeaders.CacheControl, "no-cache");
            res.ApplyGlobalResponseHeaders();
            res.UseBufferedStream = false;
            res.KeepAlive = true;

            if (feature.OnInit != null)
                feature.OnInit(req);

            res.Flush();

            var serverEvents = req.TryResolve<IServerEvents>();
            var userAuthId = session != null ? session.UserAuthId : null;
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
                UserName = session != null ? session.UserName : null,
                DisplayName = displayName,
                SessionId = req.GetPermanentSessionId(),
                IsAuthenticated = session != null && session.IsAuthenticated,
                UserAddress = req.UserHostAddress,
                OnPublish = feature.OnPublish,
                Meta = {
                    { "userId", userId },
                    { "displayName", displayName },
                    { "channels", string.Join(",", channels) },
                    { AuthMetadataProvider.ProfileUrlKey, session.GetProfileUrl() ?? AuthMetadataProvider.DefaultNoProfileImgUrl },
                }
            };

            if (feature.OnCreated != null)
                feature.OnCreated(subscription, req);

            if (req.Response.IsClosed)
                return EmptyTask; //Allow short-circuiting in OnCreated callback

            var heartbeatUrl = req.ResolveAbsoluteUrl("~/".CombineWith(feature.HeartbeatPath))
                .AddQueryParam("id", subscriptionId);
            var unRegisterUrl = req.ResolveAbsoluteUrl("~/".CombineWith(feature.UnRegisterPath))
                .AddQueryParam("id", subscriptionId);
            subscription.ConnectArgs = new Dictionary<string, string>(subscription.Meta) {
                {"id", subscriptionId },
                {"unRegisterUrl", unRegisterUrl},
                {"heartbeatUrl", heartbeatUrl},
                {"heartbeatIntervalMs", ((long)feature.HeartbeatInterval.TotalMilliseconds).ToString(CultureInfo.InvariantCulture) },
                {"idleTimeoutMs", ((long)feature.IdleTimeout.TotalMilliseconds).ToString(CultureInfo.InvariantCulture)}
            };

            if (feature.OnConnect != null)
                feature.OnConnect(subscription, subscription.ConnectArgs);

            serverEvents.Register(subscription, subscription.ConnectArgs);

            var tcs = new TaskCompletionSource<bool>();

            subscription.OnDispose = _ =>
            {
                try
                {
                    res.EndHttpHandlerRequest(skipHeaders: true);
                }
                catch { }
                tcs.SetResult(true);
            };

            return tcs.Task;
        }
    }

    public class ServerEventsHeartbeatHandler : HttpAsyncTaskHandler
    {
        public override bool RunAsAsync() { return true; }

        public override Task ProcessRequestAsync(IRequest req, IResponse res, string operationName)
        {
            if (HostContext.ApplyCustomHandlerRequestFilters(req, res))
                return EmptyTask;

            res.ApplyGlobalResponseHeaders();

            var serverEvents = req.TryResolve<IServerEvents>();

            var feature = HostContext.GetPlugin<ServerEventsFeature>();
            if (feature.OnHeartbeatInit != null)
                feature.OnHeartbeatInit(req);

            if (req.Response.IsClosed)
                return EmptyTask;

            var subscriptionId = req.QueryString["id"];
            if (!feature.CanAccessSubscription(req, subscriptionId))
            {
                res.StatusCode = 403;
                res.StatusDescription = "Invalid User Address";
                res.EndHttpHandlerRequest(skipHeaders: true);
                return EmptyTask;
            }

            if (!serverEvents.Pulse(subscriptionId))
            {
                res.StatusCode = 404;
                res.StatusDescription = "Subscription {0} does not exist".Fmt(subscriptionId);
            }
            res.EndHttpHandlerRequest(skipHeaders: true);
            return EmptyTask;
        }
    }

    [Exclude(Feature.Soap)]
    public class GetEventSubscribers : IReturn<List<Dictionary<string, string>>>
    {
        public string[] Channel { get; set; } //deprecated
        public string[] Channels { get; set; }
    }

    [DefaultRequest(typeof(GetEventSubscribers))]
    [Restrict(VisibilityTo = RequestAttributes.None)]
    public class ServerEventsSubscribersService : Service
    {
        public IServerEvents ServerEvents { get; set; }

        public object Any(GetEventSubscribers request)
        {
            var channels = new List<string>();

            if (request.Channel != null)
                channels.AddRange(request.Channel);
            if (request.Channels != null)
                channels.AddRange(request.Channels);

            return ServerEvents.GetSubscriptionsDetails(channels.ToArray());
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
                throw HttpError.NotFound(ErrorMessages.SubscriptionNotExistsFmt.Fmt(request.Id));

            var feature = HostContext.GetPlugin<ServerEventsFeature>();
            if (!feature.CanAccessSubscription(base.Request, subscription))
                throw HttpError.Forbidden(ErrorMessages.SubscriptionForbiddenFmt.Fmt(request.Id));

            ServerEvents.UnRegister(subscription.SubscriptionId);

            return subscription.Meta;
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
        public static string[] UnknownChannel = new[] { "*" };

        public DateTime LastPulseAt { get; set; }

        private readonly IResponse response;
        private long msgId;

        public EventSubscription(IResponse response)
        {
            this.response = response;
            this.Meta = new Dictionary<string, string>();
        }

        public Action<IEventSubscription> OnUnsubscribe { get; set; }
        public Action<IEventSubscription> OnDispose { get; set; }
        public Action<IResponse, string> OnPublish { get; set; }

        public void Publish(string selector)
        {
            Publish(selector, null);
        }

        public void Publish(string selector, string message)
        {
            try
            {
                var msg = message ?? "";
                var frame = "id: " + Interlocked.Increment(ref msgId) + "\n"
                          + "data: " + selector + " " + msg + "\n\n";

                lock (response)
                {
                    response.OutputStream.Write(frame);
                    response.Flush();

                    if (OnPublish != null)
                        OnPublish(response, frame);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error publishing notification to: " + selector, ex);
                Unsubscribe();
            }
        }

        public void Pulse()
        {
            LastPulseAt = DateTime.UtcNow;
        }

        public void Unsubscribe()
        {
            if (OnUnsubscribe != null)
                OnUnsubscribe(this);
        }

        public void Dispose()
        {
            OnUnsubscribe = null;
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

            if (OnDispose != null)
                OnDispose(this);
        }
    }

    public interface IEventSubscription : IMeta, IDisposable
    {
        DateTime CreatedAt { get; set; }
        DateTime LastPulseAt { get; set; }

        string[] Channels { get; }
        string UserId { get; }
        string UserName { get; }
        string DisplayName { get; }
        string SessionId { get; }
        string SubscriptionId { get; }
        string UserAddress { get; set; }
        bool IsAuthenticated { get; set; }

        Action<IEventSubscription> OnUnsubscribe { get; set; }
        void Unsubscribe();

        void Publish(string selector, string message);
        void Pulse();
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
    }

    public class MemoryServerEvents : IServerEvents
    {
        private static ILog Log = LogManager.GetLogger(typeof(MemoryServerEvents));

        public static int DefaultArraySize = 2;
        public static int ReSizeMultiplier = 2;
        public static int ReSizeBuffer = 20;

        public TimeSpan IdleTimeout { get; set; }

        public Action<IEventSubscription> OnSubscribe { get; set; }
        public Action<IEventSubscription> OnUnsubscribe { get; set; }

        public Action<IEventSubscription> NotifyJoin { get; set; }
        public Action<IEventSubscription> NotifyLeave { get; set; }
        public Action<IEventSubscription> NotifyHeartbeat { get; set; }
        public Func<object, string> Serialize { get; set; }


        public bool NotifyChannelOfSubscriptions { get; set; }

        public ConcurrentDictionary<string, IEventSubscription[]> Subcriptions;
        public ConcurrentDictionary<string, IEventSubscription[]> ChannelSubcriptions;
        public ConcurrentDictionary<string, IEventSubscription[]> UserIdSubcriptions;
        public ConcurrentDictionary<string, IEventSubscription[]> UserNameSubcriptions;
        public ConcurrentDictionary<string, IEventSubscription[]> SessionSubcriptions;

        public MemoryServerEvents()
        {
            Reset();

            NotifyJoin = s => NotifyChannels(s.Channels, "cmd.onJoin", s.Meta);
            NotifyLeave = s => NotifyChannels(s.Channels, "cmd.onLeave", s.Meta);
            NotifyHeartbeat = s => NotifySubscription(s.SubscriptionId, "cmd.onHeartbeat", s.Meta);
            Serialize = o => o != null ? o.ToJson() : null;
        }

        public void Reset()
        {
            Subcriptions = new ConcurrentDictionary<string, IEventSubscription[]>();
            ChannelSubcriptions = new ConcurrentDictionary<string, IEventSubscription[]>();
            UserIdSubcriptions = new ConcurrentDictionary<string, IEventSubscription[]>();
            UserNameSubcriptions = new ConcurrentDictionary<string, IEventSubscription[]>();
            SessionSubcriptions = new ConcurrentDictionary<string, IEventSubscription[]>();
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
            foreach (var entry in Subcriptions)
            {
                foreach (var sub in entry.Value)
                {
                    if (sub != null)
                        sub.Publish(selector, Serialize(message));
                }
            }
        }

        public void NotifySubscription(string subscriptionId, string selector, object message, string channel = null)
        {
            Notify(Subcriptions, subscriptionId, selector, message, channel);
        }

        public void NotifyChannels(string[] channels, string selector, Dictionary<string, string> meta)
        {
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

        protected void Notify(ConcurrentDictionary<string, IEventSubscription[]> map, string key,
            string selector, object message, string channel = null)
        {
            IEventSubscription[] subs;
            if (!map.TryGetValue(key, out subs)) return;

            var expired = new List<IEventSubscription>();
            var now = DateTime.UtcNow;

            foreach (var subscription in subs)
            {
                if (subscription.HasChannel(channel))
                {
                    if (now - subscription.LastPulseAt > IdleTimeout)
                    {
                        if (Log.IsDebugEnabled)
                            Log.DebugFormat("[SSE-SERVER] Expired {0} Sub {1} on ({2})", selector, subscription.SubscriptionId, string.Join(", ", subscription.Channels));

                        expired.Add(subscription);
                    }
                    if (Log.IsDebugEnabled)
                        Log.DebugFormat("[SSE-SERVER] Sending {0} msg to {1} on ({2})", selector, subscription.SubscriptionId, string.Join(", ", subscription.Channels));

                    subscription.Publish(selector, Serialize(message));
                }
            }

            foreach (var sub in expired)
            {
                sub.Unsubscribe();
            }
        }

        public bool Pulse(string id)
        {
            var sub = GetSubscription(id);
            if (sub == null)
                return false;
            sub.Pulse();

            if (NotifyHeartbeat != null)
                NotifyHeartbeat(sub);

            return true;
        }

        public IEventSubscription GetSubscription(string id)
        {
            if (id == null) return null;
            IEventSubscription[] subs;
            if (!Subcriptions.TryGetValue(id, out subs))
                return null;

            foreach (var sub in subs)
            {
                if (sub != null)
                    return sub;
            }

            return null;
        }

        public SubscriptionInfo GetSubscriptionInfo(string id)
        {
            return GetSubscription(id).GetInfo();
        }

        public List<SubscriptionInfo> GetSubscriptionInfosByUserId(string userId)
        {
            var subInfos = new List<SubscriptionInfo>();
            if (userId == null) return subInfos;
            
            IEventSubscription[] subs;
            if (!UserIdSubcriptions.TryGetValue(userId, out subs))
                return subInfos;

            foreach (var sub in subs)
            {
                var info = sub.GetInfo();
                if (info != null)
                    subInfos.Add(info);
            }
            return subInfos;
        }

        ConcurrentDictionary<string, long> SequenceCounters = new ConcurrentDictionary<string, long>();

        public long GetNextSequence(string sequenceId)
        {
            return SequenceCounters.AddOrUpdate(sequenceId, 1, (id, count) => count + 1);
        }

        public List<Dictionary<string, string>> GetSubscriptionsDetails(params string[] channels)
        {
            var ret = new List<Dictionary<string, string>>();
            var alreadyAdded = new HashSet<string>();

            foreach (var channel in channels)
            {
                IEventSubscription[] subs;
                if (!ChannelSubcriptions.TryGetValue(channel, out subs))
                    continue;

                foreach (var sub in subs)
                {
                    if (sub == null)
                        continue;

                    if (!alreadyAdded.Contains(sub.SubscriptionId))
                    {
                        ret.Add(sub.Meta);
                        alreadyAdded.Add(sub.SubscriptionId);
                    }
                }
            }

            return ret;
        }

        public void Register(IEventSubscription subscription, Dictionary<string, string> connectArgs = null)
        {
            try
            {
                lock (subscription)
                {
                    if (connectArgs != null)
                        subscription.Publish("cmd.onConnect", connectArgs.ToJson());

                    subscription.OnUnsubscribe = HandleUnsubscription;
                    foreach (var channel in subscription.Channels ?? EventSubscription.UnknownChannel)
                    {
                        RegisterSubscription(subscription, channel, ChannelSubcriptions);
                    }
                    RegisterSubscription(subscription, subscription.SubscriptionId, Subcriptions);
                    RegisterSubscription(subscription, subscription.UserId, UserIdSubcriptions);
                    RegisterSubscription(subscription, subscription.UserName, UserNameSubcriptions);
                    RegisterSubscription(subscription, subscription.SessionId, SessionSubcriptions);

                    if (OnSubscribe != null)
                        OnSubscribe(subscription);
                }

                if (NotifyChannelOfSubscriptions && subscription.Channels != null && NotifyJoin != null)
                    NotifyJoin(subscription);
            }
            catch (Exception ex)
            {
                Log.Error("Register: " + ex.Message, ex);
                throw;
            }
        }

        void RegisterSubscription(IEventSubscription subscription, string key,
            ConcurrentDictionary<string, IEventSubscription[]> map)
        {
            if (key == null)
                return;

            IEventSubscription[] subs;
            if (!map.TryGetValue(key, out subs))
            {
                subs = new IEventSubscription[DefaultArraySize];
                subs[0] = subscription;
                if (map.TryAdd(key, subs))
                    return;
            }

            while (!map.TryGetValue(key, out subs)) ;
            if (!TryAdd(subs, subscription))
            {
                IEventSubscription[] snapshot, newArray;
                do
                {
                    while (!map.TryGetValue(key, out snapshot)) ;
                    newArray = new IEventSubscription[subs.Length * ReSizeMultiplier + ReSizeBuffer];
                    Array.Copy(snapshot, 0, newArray, 0, snapshot.Length);
                    if (!TryAdd(newArray, subscription, startIndex: snapshot.Length))
                        snapshot = null;
                } while (!map.TryUpdate(key, newArray, snapshot));
            }
        }

        private static bool TryAdd(IEventSubscription[] subs, IEventSubscription subscription, int startIndex = 0)
        {
            for (int i = startIndex; i < subs.Length; i++)
            {
                if (subs[i] != null) continue;
                lock (subs)
                {
                    if (subs[i] != null) continue;
                    subs[i] = subscription;
                    return true;
                }
            }
            return false;
        }

        public void UnRegister(string subscriptionId)
        {
            var subscription = GetSubscription(subscriptionId);
            if (subscription == null)
                return;

            HandleUnsubscription(subscription);
        }

        void UnRegisterSubscription(IEventSubscription subscription, string key,
            ConcurrentDictionary<string, IEventSubscription[]> map)
        {
            if (key == null)
                return;

            try
            {
                IEventSubscription[] subs;
                if (!map.TryGetValue(key, out subs)) return;

                for (int i = 0; i < subs.Length; i++)
                {
                    if (subs[i] != subscription) continue;
                    lock (subs)
                    {
                        if (subs[i] == subscription)
                        {
                            subs[i] = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("UnRegister: " + ex.Message, ex);
                throw;
            }
        }

        void HandleUnsubscription(IEventSubscription subscription)
        {
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

                if (OnUnsubscribe != null)
                    OnUnsubscribe(subscription);

                subscription.Dispose();
            }

            if (NotifyChannelOfSubscriptions && subscription.Channels != null && NotifyLeave != null)
                NotifyLeave(subscription);
        }

        public void Dispose()
        {
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

        // Admin API's
        void Register(IEventSubscription subscription, Dictionary<string, string> connectArgs = null);

        void UnRegister(string subscriptionId);

        long GetNextSequence(string sequenceId);

        // Client API's
        List<Dictionary<string, string>> GetSubscriptionsDetails(params string[] channels);

        bool Pulse(string subscriptionId);

        // Clear all Registrations
        void Reset();
        void Start();
        void Stop();
    }

    public static class Selector
    {
        public static string Id(Type type)
        {
            return "cmd." + type.Name;
        }

        public static string Id<T>()
        {
            return "cmd." + typeof(T).Name;
        }
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
    }
}