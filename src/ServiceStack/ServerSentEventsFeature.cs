using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Host.Handlers;
using ServiceStack.Logging;
using ServiceStack.Web;

namespace ServiceStack
{
    public class ServerSentEventsFeature : IPlugin
    {
        public string AtRestPath { get; set; }

        public Action<IEventSubscription, IRequest> OnCreated { get; set; }
        public Action<IEventSubscription> OnSubscribe { get; set; }
        public Action<IEventSubscription> OnUnsubscribe { get; set; } 

        public ServerSentEventsFeature()
        {
            AtRestPath = "/event-stream";
        }

        public void Register(IAppHost appHost)
        {
            var defaultNotifications = new MemoryNotifications {
                OnSubscribe = OnSubscribe,
                OnUnsubscribe = OnUnsubscribe,
            };
            var container = appHost.GetContainer();

            if (container.TryResolve<IEventSource>() == null)
                container.Register<IEventSource>(defaultNotifications);

            if (container.TryResolve<INotifier>() == null)
                container.Register<INotifier>(defaultNotifications);

            appHost.RawHttpHandlers.Add(httpReq => 
                httpReq.PathInfo.EndsWith(AtRestPath)
                    ? new ServerSentEventsHandler()
                    : null);

            appHost.RegisterService(typeof(ServerSentEventsService), AtRestPath.CombineWith("subscriptions"));
        }
    }

    public class ServerSentEventsHandler : HttpAsyncTaskHandler
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

            var session = req.GetSession();
            var userAuthId = session != null ? session.UserAuthId : null;
            var displayName = (session != null ? session.DisplayName : null) 
                ?? "User" + Interlocked.Increment(ref anonUserId);

            var subscriptionId = SessionExtensions.CreateRandomSessionId();
            var subscription = new EventSubscription(res) 
            {
                Channel = req.QueryString["channel"] ?? req.OperationName,
                SubscriptionId = subscriptionId,
                UserAuthId = userAuthId,
                UserName = session != null ? session.UserName : null,
                SessionId = req.GetPermanentSessionId(),
                Meta = {
                    { "id", subscriptionId },
                    { "userAuthId", userAuthId },
                    { "displayName", displayName },
                    { AuthMetadataProvider.ProfileUrlKey, session.GetProfileUrl() ?? AuthMetadataProvider.DefaultNoProfileImgUrl },
                }
            };
            var feature = HostContext.GetPlugin<ServerSentEventsFeature>();
            if (feature.OnCreated != null)
                feature.OnCreated(subscription, req);

            req.TryResolve<IEventSource>().Register(subscription);

            var tcs = new TaskCompletionSource<bool>();

            subscription.OnDispose = _ => tcs.SetResult(true);

            return tcs.Task;
        }
    }

    public class GetActiveSubscriptions : IReturn<List<Dictionary<string,string>>>
    {
        public string Channel { get; set; }
    }

    [DefaultRequest(typeof(GetActiveSubscriptions))]
    [Restrict(VisibilityTo = RequestAttributes.None)]
    public class ServerSentEventsService : Service
    {
        public IEventSource EventSource { get; set; }

        public object Any(GetActiveSubscriptions request)
        {
            return EventSource.GetActiveSubscriptions(request.Channel);
        }
    }

    /*
    cmd.showPopup message
    cmd.toggle$h1:first-child

    trigger.animateBox$#boxid {"opacity":".5","padding":"-=20px"}
    trigger.animateBox$.boxclass {"marginTop":"+=20px", "padding":"+=20"}

    css.color #0C0
    css.color$h1 black
    css.backgroundColor #f1f1f1
    css.backgroundColor$h1 yellow
    css.backgroundColor$#boxid red
    css.backgroundColor$.boxclass purple
    css.color$#boxid,.boxclass white
    
    document.title Hello World
    window.location http://google.com
    */

    public class EventSubscription : IEventSubscription
    {
        private static ILog Log = LogManager.GetLogger(typeof(EventSubscription));

        private readonly IResponse response;
        private long msgId;

        public EventSubscription(IResponse response)
        {
            this.response = response;
            this.Meta = new Dictionary<string, string>();
        }

        public string Channel { get; set; }
        public string SubscriptionId { get; set; }
        public string UserAuthId { get; set; }
        public string UserName { get; set; }
        public string SessionId { get; set; }

        public Action<IEventSubscription> OnUnsubscribe { get; set; }
        public Action<IEventSubscription> OnDispose { get; set; }

        public void Publish(string selector, object message)
        {
            try
            {
                lock (response)
                {
                    var msg = (message != null ? message.ToJson() : "");
                    var frame = "id: " + Interlocked.Increment(ref msgId) + "\n"
                              + "data: " + selector + " " + msg + "\n\n";
                    response.OutputStream.Write(frame);
                    response.Flush();
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error publishing notification to: " + selector, ex);

                if (OnUnsubscribe != null)
                    OnUnsubscribe(this);
            }
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
        string Channel { get; }
        string UserAuthId { get; }
        string UserName { get; }
        string SessionId { get; }
        string SubscriptionId { get; }

        Action<IEventSubscription> OnUnsubscribe { get; set; }

        void Publish(string selector, object message);
    }

    public class MemoryNotifications : IEventSource, INotifier
    {
        public static int DefaultArraySize = 10;
        public static int ReSizeMultiplier = 2;
        public static int ReSizeBuffer = 100;
        const string UnknownChannel = "*";

        public Action<IEventSubscription> OnSubscribe { get; set; }
        public Action<IEventSubscription> OnUnsubscribe { get; set; }

        public ConcurrentDictionary<string, IEventSubscription[]> Subcriptions =
           new ConcurrentDictionary<string, IEventSubscription[]>();
        public ConcurrentDictionary<string, IEventSubscription[]> ChannelSubcriptions =
           new ConcurrentDictionary<string, IEventSubscription[]>();
        public ConcurrentDictionary<string, IEventSubscription[]> UserIdSubcriptions =
           new ConcurrentDictionary<string, IEventSubscription[]>();
        public ConcurrentDictionary<string, IEventSubscription[]> UserNameSubcriptions =
           new ConcurrentDictionary<string, IEventSubscription[]>();
        public ConcurrentDictionary<string, IEventSubscription[]> SessionSubcriptions =
           new ConcurrentDictionary<string, IEventSubscription[]>();

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

        public void NotifyUserId(string userAuthId, string selector, object message, string channel = null)
        {
            Notify(UserIdSubcriptions, userAuthId, selector, message, channel);
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

            foreach (var subscription in subs)
            {
                if (subscription != null && (channel == null || subscription.Channel == channel))
                    subscription.Publish(selector, message);
            }
        }

        public List<Dictionary<string, string>> GetActiveSubscriptions(string channel=null)
        {
            return ChannelSubcriptions.Values
                .SelectMany(x => x)
                .Where(x => x != null && (channel == null || x.Channel == channel))
                .Select(x => x.Meta)
                .ToList();
        }

        public void Register(IEventSubscription subscription)
        {
            lock (subscription)
            {
                subscription.OnUnsubscribe = HandleUnsubscription;
                RegisterSubscription(subscription, subscription.Channel ?? UnknownChannel, ChannelSubcriptions);
                RegisterSubscription(subscription, subscription.SubscriptionId, Subcriptions);
                RegisterSubscription(subscription, subscription.UserAuthId, UserIdSubcriptions);
                RegisterSubscription(subscription, subscription.UserName, UserNameSubcriptions);
                RegisterSubscription(subscription, subscription.SessionId, SessionSubcriptions);

                if (OnSubscribe != null)
                    OnSubscribe(subscription);
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

            while (!map.TryGetValue(key, out subs));
            if (!TryAdd(subs, subscription))
            {
                IEventSubscription[] snapshot, newArray;
                do
                {
                    while (!map.TryGetValue(key, out snapshot));
                    newArray = new IEventSubscription[subs.Length * ReSizeMultiplier + ReSizeBuffer];
                    Array.Copy(snapshot, 0, newArray, 0, snapshot.Length);
                    if (!TryAdd(subs, subscription, startIndex:snapshot.Length))
                        snapshot = null;
                } while (!map.TryUpdate(key, newArray, snapshot));
            }
        }

        private static bool TryAdd(IEventSubscription[] subs, IEventSubscription subscription, int startIndex=0)
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

        void UnRegisterSubscription(IEventSubscription subscription, string key,
            ConcurrentDictionary<string, IEventSubscription[]> map)
        {
            if (key == null)
                return;

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

        void HandleUnsubscription(IEventSubscription subscription)
        {
            lock (subscription)
            {
                UnRegisterSubscription(subscription, subscription.Channel ?? UnknownChannel, ChannelSubcriptions);
                UnRegisterSubscription(subscription, subscription.SubscriptionId, Subcriptions);
                UnRegisterSubscription(subscription, subscription.UserAuthId, UserIdSubcriptions);
                UnRegisterSubscription(subscription, subscription.UserName, UserNameSubcriptions);
                UnRegisterSubscription(subscription, subscription.SessionId, SessionSubcriptions);

                if (OnUnsubscribe != null)
                    OnUnsubscribe(subscription);

                subscription.Dispose();
            }
        }
    }

    public interface IEventSource
    {
        List<Dictionary<string, string>> GetActiveSubscriptions(string channel = null);

        void Register(IEventSubscription subscription);
    }

    public interface INotifier
    {
        void NotifyAll(string selector, object message);

        void NotifyChannel(string channel, string selector, object message);

        void NotifySubscription(string subscriptionId, string selector, object message, string channel = null);

        void NotifyUserId(string userAuthId, string selector, object message, string channel = null);

        void NotifyUserName(string userName, string selector, object message, string channel = null);

        void NotifySession(string sspid, string selector, object message, string channel = null);
    }
}