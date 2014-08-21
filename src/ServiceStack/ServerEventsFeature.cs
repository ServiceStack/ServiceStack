using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using ServiceStack.Auth;
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

        public TimeSpan Timeout { get; set; }
        public TimeSpan HeartbeatInterval { get; set; }

        public Action<IEventSubscription, IRequest> OnCreated { get; set; }
        public Action<IEventSubscription, Dictionary<string, string>> OnConnect { get; set; }
        public Action<IEventSubscription> OnSubscribe { get; set; }
        public Action<IEventSubscription> OnUnsubscribe { get; set; }
        public bool NotifyChannelOfSubscriptions { get; set; }

        public ServerEventsFeature()
        {
            StreamPath = "/event-stream";
            HeartbeatPath = "/event-heartbeat";
            UnRegisterPath = "/event-unregister";
            SubscribersPath = "/event-subscribers";

            Timeout = TimeSpan.FromSeconds(30);
            HeartbeatInterval = TimeSpan.FromSeconds(10);

            NotifyChannelOfSubscriptions = true;
        }

        public void Register(IAppHost appHost)
        {
            var broker = new MemoryServerEvents
            {
                Timeout = Timeout,
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
    }

    public class ServerEventsHandler : HttpAsyncTaskHandler
    {
        static long anonUserId;

        public override bool RunAsAsync()
        {
            return true;
        }

        public override Task ProcessRequestAsync(IRequest req, IResponse res, string operationName)
        {
            res.ContentType = MimeTypes.ServerSentEvents;
            res.AddHeader(HttpHeaders.CacheControl, "no-cache");
            res.KeepAlive = true;
            res.Flush();

            IAuthSession session = req.GetSession();
            var userAuthId = session != null ? session.UserAuthId : null;
            var userId = userAuthId ?? ("-" + Interlocked.Increment(ref anonUserId));
            var displayName = session.GetSafeDisplayName()
                ?? "user" + anonUserId;

            var feature = HostContext.GetPlugin<ServerEventsFeature>();

            var now = DateTime.UtcNow;
            var subscriptionId = SessionExtensions.CreateRandomSessionId();
            var subscription = new EventSubscription(res)
            {
                CreatedAt = now,
                LastPulseAt = now,
                Channel = req.QueryString["channel"] ?? EventSubscription.UnknownChannel,
                SubscriptionId = subscriptionId,
                UserId = userId,
                UserName = session != null ? session.UserName : null,
                DisplayName = displayName,
                SessionId = req.GetPermanentSessionId(),
                IsAuthenticated = session != null && session.IsAuthenticated,
                Meta = {
                    { "userId", userId },
                    { "displayName", displayName },
                    { AuthMetadataProvider.ProfileUrlKey, session.GetProfileUrl() ?? AuthMetadataProvider.DefaultNoProfileImgUrl },
                }
            };

            if (feature.OnCreated != null)
                feature.OnCreated(subscription, req);

            var heartbeatUrl = req.ResolveAbsoluteUrl("~/".CombineWith(feature.HeartbeatPath))
                .AddQueryParam("id", subscriptionId);
            var unRegisterUrl = req.ResolveAbsoluteUrl("~/".CombineWith(feature.UnRegisterPath))
                .AddQueryParam("id", subscriptionId);
            var privateArgs = new Dictionary<string, string>(subscription.Meta) {
                {"id", subscriptionId },
                {"unRegisterUrl", unRegisterUrl},
                {"heartbeatUrl", heartbeatUrl},
                {"heartbeatIntervalMs", ((long)feature.HeartbeatInterval.TotalMilliseconds).ToString(CultureInfo.InvariantCulture) }};

            if (feature.OnConnect != null)
                feature.OnConnect(subscription, privateArgs);

            subscription.Publish("cmd.onConnect", privateArgs);

            req.TryResolve<IServerEvents>().Register(subscription);

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
            req.TryResolve<IServerEvents>().Pulse(req.QueryString["id"]);
            res.EndHttpHandlerRequest(skipHeaders: true);
            return EmptyTask;
        }
    }

    public class GetEventSubscribers : IReturn<List<Dictionary<string, string>>>
    {
        public string Channel { get; set; }
    }

    [DefaultRequest(typeof(GetEventSubscribers))]
    [Restrict(VisibilityTo = RequestAttributes.None)]
    public class ServerEventsSubscribersService : Service
    {
        public IServerEvents ServerEvents { get; set; }

        public object Any(GetEventSubscribers request)
        {
            return ServerEvents.GetSubscriptions(request.Channel);
        }
    }

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
            var subscription = ServerEvents.GetSubscription(request.Id);
            if (subscription == null)
                throw HttpError.NotFound("Subscription '{0}' does not exist.".Fmt(request.Id));

            ServerEvents.UnRegister(subscription);

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

    public class EventSubscription : IEventSubscription
    {
        private static ILog Log = LogManager.GetLogger(typeof(EventSubscription));
        public static string UnknownChannel = "*";

        private readonly IResponse response;
        private long msgId;

        public EventSubscription(IResponse response)
        {
            this.response = response;
            this.Meta = new Dictionary<string, string>();
        }

        public DateTime CreatedAt { get; set; }
        public DateTime LastPulseAt { get; set; }
        public string Channel { get; set; }
        public string SubscriptionId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        public string SessionId { get; set; }
        public bool IsAuthenticated { get; set; }

        public Action<IEventSubscription> OnUnsubscribe { get; set; }
        public Action<IEventSubscription> OnDispose { get; set; }

        public void Publish(string selector, object message)
        {
            try
            {
                var msg = (message != null ? message.ToJson() : "");
                var frame = "id: " + Interlocked.Increment(ref msgId) + "\n"
                          + "data: " + selector + " " + msg + "\n\n";

                lock (response)
                {
                    response.OutputStream.Write(frame);
                    response.Flush();
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

        public Dictionary<string, string> Meta { get; set; }
    }

    public interface IEventSubscription : IMeta, IDisposable
    {
        DateTime CreatedAt { get; set; }
        DateTime LastPulseAt { get; set; }

        string Channel { get; }
        string UserId { get; }
        string UserName { get; }
        string DisplayName { get; }
        string SessionId { get; }
        string SubscriptionId { get; }
        bool IsAuthenticated { get; set; }

        Action<IEventSubscription> OnUnsubscribe { get; set; }
        void Unsubscribe();

        void Publish(string selector, object message);
        void Pulse();
    }

    public class MemoryServerEvents : IServerEvents
    {
        private static ILog Log = LogManager.GetLogger(typeof(MemoryServerEvents));

        public static int DefaultArraySize = 2;
        public static int ReSizeMultiplier = 2;
        public static int ReSizeBuffer = 20;

        public TimeSpan Timeout { get; set; }

        public Action<IEventSubscription> OnSubscribe { get; set; }
        public Action<IEventSubscription> OnUnsubscribe { get; set; }
        public bool NotifyChannelOfSubscriptions { get; set; }

        public ConcurrentDictionary<string, IEventSubscription[]> Subcriptions;
        public ConcurrentDictionary<string, IEventSubscription[]> ChannelSubcriptions;
        public ConcurrentDictionary<string, IEventSubscription[]> UserIdSubcriptions;
        public ConcurrentDictionary<string, IEventSubscription[]> UserNameSubcriptions;
        public ConcurrentDictionary<string, IEventSubscription[]> SessionSubcriptions;

        public MemoryServerEvents()
        {
            Reset();
        }

        public void Reset()
        {
            Subcriptions = new ConcurrentDictionary<string, IEventSubscription[]>();
            ChannelSubcriptions = new ConcurrentDictionary<string, IEventSubscription[]>();
            UserIdSubcriptions = new ConcurrentDictionary<string, IEventSubscription[]>();
            UserNameSubcriptions = new ConcurrentDictionary<string, IEventSubscription[]>();
            SessionSubcriptions = new ConcurrentDictionary<string, IEventSubscription[]>();
        }

        public void NotifyAll(string selector, object message)
        {
            foreach (var entry in Subcriptions)
            {
                foreach (var sub in entry.Value)
                {
                    if (sub != null)
                        sub.Publish(selector, message);
                }
            }
        }

        public void NotifySubscription(string subscriptionId, string selector, object message, string channel = null)
        {
            Notify(Subcriptions, subscriptionId, selector, message, channel);
        }

        public void NotifyChannel(string channel, string selector, object message)
        {
            Notify(ChannelSubcriptions, channel, selector, message, channel);
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

        void Notify(ConcurrentDictionary<string, IEventSubscription[]> map, string key,
            string selector, object message, string channel = null)
        {
            IEventSubscription[] subs;
            if (!map.TryGetValue(key, out subs)) return;

            var expired = new List<IEventSubscription>();
            var now = DateTime.UtcNow;

            foreach (var subscription in subs)
            {
                if (subscription != null && (channel == null || subscription.Channel == channel))
                {
                    if (now - subscription.LastPulseAt > Timeout)
                    {
                        expired.Add(subscription);
                    }
                    subscription.Publish(selector, message);
                }
            }

            foreach (var sub in expired)
            {
                sub.Unsubscribe();
            }
        }

        public void Pulse(string id)
        {
            var sub = GetSubscription(id);
            if (sub == null) return;
            sub.Pulse();
        }

        public IEventSubscription GetSubscription(string id)
        {
            if (id == null) return null;
            foreach (var subs in Subcriptions.Values)
            {
                foreach (var sub in subs)
                {
                    if (sub != null && sub.SubscriptionId == id)
                        return sub;
                }
            }
            return null;
        }

        public List<IEventSubscription> GetSubscriptionsByUserId(string userId)
        {
            var userSubs = new List<IEventSubscription>();
            if (userId == null) return userSubs;
            foreach (var subs in Subcriptions.Values)
            {
                foreach (var sub in subs)
                {
                    if (sub != null && sub.UserId == userId)
                        userSubs.Add(sub);
                }
            }
            return userSubs;
        }

        public List<Dictionary<string, string>> GetSubscriptions(string channel = null)
        {
            var ret = new List<Dictionary<string, string>>();
            foreach (var subs in Subcriptions.Values)
            {
                foreach (var sub in subs)
                {
                    if (sub != null && (channel == null || sub.Channel == channel))
                        ret.Add(sub.Meta);
                }
            }
            return ret;
        }

        public void Register(IEventSubscription subscription)
        {
            try
            {
                lock (subscription)
                {
                    subscription.OnUnsubscribe = HandleUnsubscription;
                    RegisterSubscription(subscription, subscription.Channel ?? EventSubscription.UnknownChannel, ChannelSubcriptions);
                    RegisterSubscription(subscription, subscription.SubscriptionId, Subcriptions);
                    RegisterSubscription(subscription, subscription.UserId, UserIdSubcriptions);
                    RegisterSubscription(subscription, subscription.UserName, UserNameSubcriptions);
                    RegisterSubscription(subscription, subscription.SessionId, SessionSubcriptions);

                    if (OnSubscribe != null)
                        OnSubscribe(subscription);
                }

                if (NotifyChannelOfSubscriptions && subscription.Channel != null)
                    NotifyChannel(subscription.Channel, "cmd.onJoin", subscription.Meta);
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

        public void UnRegister(IEventSubscription subscription)
        {
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
                UnRegisterSubscription(subscription, subscription.Channel ?? EventSubscription.UnknownChannel, ChannelSubcriptions);
                UnRegisterSubscription(subscription, subscription.SubscriptionId, Subcriptions);
                UnRegisterSubscription(subscription, subscription.UserId, UserIdSubcriptions);
                UnRegisterSubscription(subscription, subscription.UserName, UserNameSubcriptions);
                UnRegisterSubscription(subscription, subscription.SessionId, SessionSubcriptions);

                if (OnUnsubscribe != null)
                    OnUnsubscribe(subscription);

                subscription.Dispose();
            }

            if (NotifyChannelOfSubscriptions && subscription.Channel != null)
                NotifyChannel(subscription.Channel, "cmd.onLeave", subscription.Meta);
        }
    }

    public interface IServerEvents
    {
        // External API's
        void NotifyAll(string selector, object message);

        void NotifyChannel(string channel, string selector, object message);

        void NotifySubscription(string subscriptionId, string selector, object message, string channel = null);

        void NotifyUserId(string userId, string selector, object message, string channel = null);

        void NotifyUserName(string userName, string selector, object message, string channel = null);

        void NotifySession(string sspid, string selector, object message, string channel = null);

        IEventSubscription GetSubscription(string id);

        List<IEventSubscription> GetSubscriptionsByUserId(string userId);

        // Admin API's
        void Register(IEventSubscription subscription);

        void UnRegister(IEventSubscription subscription);

        // Client API's

        List<Dictionary<string, string>> GetSubscriptions(string channel = null);

        void Pulse(string id);

        // Clear all Registrations
        void Reset();
    }

    static class Selector
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

        public static void NotifyUserId(this IServerEvents server, string userId, string selector, object message, string channel = null)
        {
            server.NotifyUserId(userId, Selector.Id(message.GetType()), message, channel);
        }

        public static void NotifyUserName(this IServerEvents server, string userName, string selector, object message, string channel = null)
        {
            server.NotifyUserName(userName, Selector.Id(message.GetType()), message, channel);
        }

        public static void NotifySession(this IServerEvents server, string sspid, string selector, object message, string channel = null)
        {
            server.NotifySession(sspid, Selector.Id(message.GetType()), message, channel);
        }
    }
}