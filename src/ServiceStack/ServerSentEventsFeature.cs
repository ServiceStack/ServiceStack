using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Host.Handlers;
using ServiceStack.Logging;
using ServiceStack.Web;

namespace ServiceStack
{
    public class ServerSentEventsFeature : IPlugin
    {
        public string AtRestPath { get; set; }
        public IEventSource EventSource { get; set; }
        public INotifier Notifier { get; set; }

        public ServerSentEventsFeature()
        {
            AtRestPath = "/event-stream";
            EventSource = (IEventSource) (Notifier = new MemoryNotifications());
        }

        public void Register(IAppHost appHost)
        {
            appHost.Register(EventSource);
            appHost.Register(Notifier);

            appHost.RawHttpHandlers.Add(httpReq => 
                httpReq.PathInfo.EndsWith(AtRestPath)
                    ? new ServerSentEventsHandler() 
                    : null);
        }
    }

    public class ServerSentEventsHandler : HttpAsyncTaskHandler
    {
        public override bool RunAsAsync()
        {
            return true;
        }

        public override Task ProcessRequestAsync(IRequest req, IResponse res, string operationName)
        {
            res.ContentType = MimeTypes.ServerSentEvents;
            res.AddHeader(HttpHeaders.CacheControl, "no-cache");
            //(res.OriginalResponse as HttpListenerResponse).KeepAlive = true;
            res.Flush();

            var session = req.GetSession();            
            var subscription = new EventSubscription(res) 
            {
               OperationName = req.QueryString["op"] ?? req.OperationName,
               UserAuthId = session != null ? session.UserAuthId : null,
               PermSessionId = req.GetPermanentSessionId(),
               TempSessionId = req.GetTemporarySessionId(),
               Channel = req.QueryString["channel"],
            };
            req.TryResolve<IEventSource>().Register(subscription);

            var tcs = new TaskCompletionSource<bool>();

            subscription.OnDispose = _ => tcs.SetResult(true);

            return tcs.Task;
        }
    }

/*
cmd.showPopup {A:1,B:2}
cmd.showPopup [1,2,3]
cmd.showPopup "stringArg"

trigger.evt {A:1,B:2}

window.location= "http://google.com"
*/

    public class EventSubscription : IEventSubscription
    {
        private static ILog Log = LogManager.GetLogger(typeof(EventSubscription));

        private readonly IResponse response;
        private long msgId;

        public EventSubscription(IResponse response)
        {
            this.response = response;
        }

        public string OperationName { get; set; }
        public string UserAuthId { get; set; }
        public string PermSessionId { get; set; }
        public string TempSessionId { get; set; }
        public string Channel { get; set; }
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
    }

    public interface IEventSubscription : IDisposable
    {
        string OperationName { get; }
        string UserAuthId { get; }
        string PermSessionId { get; }
        string TempSessionId { get; }
        string Channel { get; }

        Action<IEventSubscription> OnUnsubscribe { get; set; }

        void Publish(string selector, object message);
    }

    public class MemoryNotifications : IEventSource, INotifier
    {
        public static int DefaultArraySize = 10;
        public static int ReSizeMultiplier = 2;
        public static int ReSizeBuffer = 100;
        const string UnknownOperation = "__unknown";

        public ConcurrentDictionary<string, IEventSubscription[]> OperationSubcriptions =
           new ConcurrentDictionary<string, IEventSubscription[]>();
        public ConcurrentDictionary<string, IEventSubscription[]> UserSubcriptions =
           new ConcurrentDictionary<string, IEventSubscription[]>();
        public ConcurrentDictionary<string, IEventSubscription[]> PermSessionSubcriptions =
           new ConcurrentDictionary<string, IEventSubscription[]>();
        public ConcurrentDictionary<string, IEventSubscription[]> TempSessionSubcriptions =
           new ConcurrentDictionary<string, IEventSubscription[]>();
        public ConcurrentDictionary<string, IEventSubscription[]> ChannelSubcriptions =
           new ConcurrentDictionary<string, IEventSubscription[]>();

        public void NotifyAll(string selector, object message)
        {
            foreach (var entry in OperationSubcriptions)
            {
                foreach (var sub in entry.Value)
                {
                    if (sub != null)
                        sub.Publish(selector, message);
                }
            }
        }

        public void NotifyRequest(string operationName, string selector, object message)
        {
            Notify(OperationSubcriptions, operationName, selector, message);
        }

        public void NotifyUser(string userAuthId, string selector, object message)
        {
            Notify(UserSubcriptions, userAuthId, selector, message);
        }

        public void NotifyPermSession(string sspid, string selector, object message)
        {
            Notify(PermSessionSubcriptions, sspid, selector, message);
        }

        public void NotifyTempSession(string ssid, string selector, object message)
        {
            Notify(TempSessionSubcriptions, ssid, selector, message);
        }

        public void NotifyChannel(string channel, string selector, object message)
        {
            Notify(ChannelSubcriptions, channel, selector, message);
        }

        void Notify(ConcurrentDictionary<string, IEventSubscription[]> map, string key,
            string selector, object message)
        {
            IEventSubscription[] subs;
            if (!map.TryGetValue(key, out subs)) return;

            foreach (var subscription in subs)
            {
                if (subscription != null)
                    subscription.Publish(selector, message);
            }
        }

        public void Register(IEventSubscription subscription)
        {
            lock (subscription)
            {
                subscription.OnUnsubscribe = HandleUnsubscription;
                RegisterSubscription(subscription, subscription.OperationName ?? UnknownOperation, OperationSubcriptions);
                RegisterSubscription(subscription, subscription.UserAuthId, UserSubcriptions);
                RegisterSubscription(subscription, subscription.PermSessionId, PermSessionSubcriptions);
                RegisterSubscription(subscription, subscription.TempSessionId, TempSessionSubcriptions);
                RegisterSubscription(subscription, subscription.Channel, ChannelSubcriptions);
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
                    while (!map.TryGetValue(key, out subs));
                    snapshot = subs;
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
                UnRegisterSubscription(subscription, subscription.OperationName ?? UnknownOperation, OperationSubcriptions);
                UnRegisterSubscription(subscription, subscription.UserAuthId, UserSubcriptions);
                UnRegisterSubscription(subscription, subscription.PermSessionId, PermSessionSubcriptions);
                UnRegisterSubscription(subscription, subscription.TempSessionId, TempSessionSubcriptions);
                UnRegisterSubscription(subscription, subscription.Channel, ChannelSubcriptions);
                subscription.Dispose();
            }
        }
    }

    public interface IEventSource
    {
        void Register(IEventSubscription subscription);
    }

    public interface INotifier
    {
        void NotifyAll(string selector, object message);

        void NotifyRequest(string operationName, string selector, object message);

        void NotifyUser(string userAuthId, string selector, object message);

        void NotifyPermSession(string sspid, string selector, object message);

        void NotifyTempSession(string ssid, string selector, object message);

        void NotifyChannel(string channel, string selector, object message);
    }
}