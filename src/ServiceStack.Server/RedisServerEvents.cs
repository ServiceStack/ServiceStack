using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ServiceStack.Logging;
using ServiceStack.Redis;
using ServiceStack.Text;

namespace ServiceStack
{
    public class RedisServerEvents : IServerEvents
    {
        private static ILog Log = LogManager.GetLogger(typeof(RedisServerEvents));

        private MemoryServerEvents local;

        public TimeSpan Timeout
        {
            get => local.IdleTimeout;
            set => local.IdleTimeout = value;
        }

        public TimeSpan HouseKeepingInterval
        {
            get => local.HouseKeepingInterval;
            set => local.HouseKeepingInterval = value;
        }

        public Action<IEventSubscription> OnSubscribe
        {
            get => local.OnSubscribe;
            set => local.OnSubscribe = value;
        }

        public Action<IEventSubscription> OnUnsubscribe
        {
            get => local.OnUnsubscribe;
            set => local.OnUnsubscribe = value;
        }

        public bool NotifyChannelOfSubscriptions
        {
            get => local.NotifyChannelOfSubscriptions;
            set => local.NotifyChannelOfSubscriptions = value;
        }

        public TimeSpan? WaitBeforeNextRestart
        {
            get => RedisPubSub.WaitBeforeNextRestart;
            set => RedisPubSub.WaitBeforeNextRestart = value;
        }

        public static string Topic = "sse:topic";

        public class RedisIndex
        {
            public static string Subscription = "sse:id:{0}";
            public static string ActiveSubscriptionsSet = "sse:ids";
            public static string ChannelSet = "sse:channel:{0}";
            public static string UserIdSet = "sse:userid:{0}";
            public static string UserNameSet = "sse:username:{0}";
            public static string SessionSet = "sse:session:{0}";
        }

        public IRedisClientsManager clientsManager;

        public IRedisPubSubServer RedisPubSub { get; set; }

        public RedisServerEvents(IRedisPubSubServer redisPubSub)
        {
            this.RedisPubSub = redisPubSub;
            this.clientsManager = redisPubSub.ClientsManager;
            redisPubSub.OnInit = OnInit;
            redisPubSub.OnError = ex => Log.Error("Exception in RedisServerEvents: " + ex.Message, ex);
            redisPubSub.OnMessage = HandleMessage;

            WaitBeforeNextRestart = TimeSpan.FromMilliseconds(2000);

            local = new MemoryServerEvents
            {
                NotifyJoin = HandleOnJoin,
                NotifyLeave = HandleOnLeave,
                NotifyUpdate = HandleOnUpdate,
                NotifyHeartbeat = HandleOnHeartbeat,
                Serialize = HandleSerialize,
            };

            var appHost = HostContext.AppHost;
            var feature = appHost?.GetPlugin<ServerEventsFeature>();
            if (feature != null)
            {
                Timeout = feature.IdleTimeout;
                HouseKeepingInterval = feature.HouseKeepingInterval;
                OnSubscribe = feature.OnSubscribe;
                OnUnsubscribe = feature.OnUnsubscribe;
                NotifyChannelOfSubscriptions = feature.NotifyChannelOfSubscriptions;
            }
        }

        private void OnInit()
        {
            UnRegisterExpiredSubscriptions();
        }

        private void UnRegisterExpiredSubscriptions()
        {
            using (var redis = clientsManager.GetClient())
            {
                var lastPulseBefore = (RedisPubSub.CurrentServerTime - Timeout).Ticks;
                var expiredSubIds = redis.GetRangeFromSortedSetByLowestScore(
                    RedisIndex.ActiveSubscriptionsSet, 0, lastPulseBefore);
                foreach (var id in expiredSubIds)
                {
                    NotifyRedis("unregister.id." + id, null, null);
                }

                //Force remove zombie subscriptions which have no listeners
                var infos = GetSubscriptionInfos(redis, expiredSubIds);
                foreach (var info in infos)
                {
                    RemoveSubscriptionFromRedis(info);
                }
            }
        }

        private static List<SubscriptionInfo> GetSubscriptionInfos(IRedisClient redis, IEnumerable<string> subIds)
        {
            var keys = subIds.Map(x => RedisIndex.Subscription.Fmt(x));
            var infos = redis.GetValues<SubscriptionInfo>(keys);
            return infos;
        }

        public RedisServerEvents(IRedisClientsManager clientsManager)
            : this(new RedisPubSubServer(clientsManager, Topic)) { }

        void HandleOnJoin(IEventSubscription sub)
        {
            NotifyChannels(sub.Channels, "cmd.onJoin", sub.Meta);
        }

        void HandleOnLeave(IEventSubscription sub)
        {
            var info = sub.GetInfo();
            RemoveSubscriptionFromRedis(info);

            NotifyChannels(sub.Channels, "cmd.onLeave", sub.Meta);
        }

        void HandleOnUpdate(IEventSubscription sub)
        {
            using (var redis = clientsManager.GetClient())
            {
                StoreSubscriptionInfo(redis, sub.GetInfo());
            }
            NotifyChannels(sub.Channels, "cmd.onUpdate", sub.Meta);
        }

        void HandleOnHeartbeat(IEventSubscription sub)
        {
            NotifySubscription(sub.SubscriptionId, "cmd.onHeartbeat", sub.Meta);
        }

        private void RemoveSubscriptionFromRedis(SubscriptionInfo info)
        {
            var id = info.SubscriptionId;

            using (var redis = clientsManager.GetClient())
            using (var trans = redis.CreateTransaction())
            {
                trans.QueueCommand(r => r.Remove(RedisIndex.Subscription.Fmt(id)));
                trans.QueueCommand(r => r.RemoveItemFromSortedSet(RedisIndex.ActiveSubscriptionsSet, id));
                trans.QueueCommand(r => r.RemoveItemFromSet(RedisIndex.UserIdSet.Fmt(info.UserId), id));

                foreach (var channel in info.Channels)
                {
                    trans.QueueCommand(r => r.RemoveItemFromSet(RedisIndex.ChannelSet.Fmt(channel), id));
                }

                if (info.UserName != null)
                    trans.QueueCommand(r => r.RemoveItemFromSet(RedisIndex.UserNameSet.Fmt(info.UserName), id));
                if (info.SessionId != null)
                    trans.QueueCommand(r => r.RemoveItemFromSet(RedisIndex.SessionSet.Fmt(info.SessionId), id));

                trans.Commit();
            }
        }

        string HandleSerialize(object o)
        {
            return (string)o; //Already a seiralized JSON string
        }

        public void NotifyAll(string selector, object message)
        {
            NotifyRedis("notify.all", selector, message);
        }

        public void NotifyChannels(string[] channels, string selector, Dictionary<string, string> meta)
        {
            foreach (var channel in channels)
            {
                var msg = new Dictionary<string, string>(meta) { { "channel", channel } };
                NotifyRedis("notify.channel." + channel, selector, msg);
            }
        }

        public void NotifyChannel(string channel, string selector, object message)
        {
            NotifyRedis("notify.channel." + channel, selector, message);
        }

        public void NotifySubscription(string subscriptionId, string selector, object message, string channel = null)
        {
            NotifyRedis("notify.subscription." + subscriptionId, selector, message, channel);
        }

        public void NotifyUserId(string userId, string selector, object message, string channel = null)
        {
            NotifyRedis("notify.userid." + userId, selector, message, channel);
        }

        public void NotifyUserName(string userName, string selector, object message, string channel = null)
        {
            NotifyRedis("notify.username." + userName, selector, message, channel);
        }

        public void NotifySession(string sspid, string selector, object message, string channel = null)
        {
            NotifyRedis("notify.session." + sspid, selector, message, channel);
        }

        public SubscriptionInfo GetSubscriptionInfo(string id)
        {
            using (var redis = clientsManager.GetClient())
            {
                var info = redis.Get<SubscriptionInfo>(RedisIndex.Subscription.Fmt(id));
                return info;
            }
        }

        public List<SubscriptionInfo> GetSubscriptionInfosByUserId(string userId)
        {
            using (var redis = clientsManager.GetClient())
            {
                var ids = redis.GetAllItemsFromSet(RedisIndex.UserIdSet.Fmt(userId));
                var keys = ids.Map(x => RedisIndex.Subscription.Fmt(x));
                var infos = redis.GetValues<SubscriptionInfo>(keys);

                return infos;
            }
        }

        public void Register(IEventSubscription sub, Dictionary<string, string> connectArgs = null)
        {
            if (sub == null)
                throw new ArgumentNullException("subscription");

            var info = sub.GetInfo();
            using (var redis = clientsManager.GetClient())
            {
                StoreSubscriptionInfo(redis, info);
            }

            if (connectArgs != null)
                sub.Publish("cmd.onConnect", connectArgs.ToJson());

            local.Register(sub);
        }

        private void StoreSubscriptionInfo(IRedisClient redis, SubscriptionInfo info)
        {
            var id = info.SubscriptionId;
            using (var trans = redis.CreateTransaction())
            {
                trans.QueueCommand(r => r.AddItemToSortedSet(RedisIndex.ActiveSubscriptionsSet, id, RedisPubSub.CurrentServerTime.Ticks));
                trans.QueueCommand(r => r.Set(RedisIndex.Subscription.Fmt(id), info));
                trans.QueueCommand(r => r.AddItemToSet(RedisIndex.UserIdSet.Fmt(info.UserId), id));

                foreach (var channel in info.Channels)
                {
                    trans.QueueCommand(r => r.AddItemToSet(RedisIndex.ChannelSet.Fmt(channel), id));
                }

                if (info.UserName != null)
                    trans.QueueCommand(r => r.AddItemToSet(RedisIndex.UserNameSet.Fmt(info.UserName), id));
                if (info.SessionId != null)
                    trans.QueueCommand(r => r.AddItemToSet(RedisIndex.SessionSet.Fmt(info.SessionId), id));

                trans.Commit();
            }
        }

        public void UnRegister(string subscriptionId)
        {
            var info = GetSubscriptionInfo(subscriptionId);
            if (info == null)
                return;

            NotifyRedis("unregister.id." + subscriptionId, null, null);
        }

        public long GetNextSequence(string sequenceId)
        {
            using (var redis = clientsManager.GetClient())
            {
                return redis.Increment("sse:seq:" + sequenceId, 1);
            }
        }

        public int RemoveExpiredSubscriptions()
        {
            return local.RemoveExpiredSubscriptions();
        }

        public void SubscribeToChannels(string subscriptionId, string[] channels)
        {
            var info = GetSubscriptionInfo(subscriptionId);
            if (info == null)
                return;

            NotifyRedis("subscribe.id." + subscriptionId, null, channels.Join(","));
        }

        public void UnsubscribeFromChannels(string subscriptionId, string[] channels)
        {
            var info = GetSubscriptionInfo(subscriptionId);
            if (info == null)
                return;

            using (var redis = clientsManager.GetClient())
            using (var trans = redis.CreateTransaction())
            {
                foreach (var channel in channels)
                {
                    trans.QueueCommand(r => r.RemoveItemFromSet(RedisIndex.ChannelSet.Fmt(channel), subscriptionId));
                }
                trans.Commit();
            }

            NotifyRedis("unsubscribe.id." + subscriptionId, null, channels.Join(","));
        }

        public List<Dictionary<string, string>> GetSubscriptionsDetails(params string[] channels)
        {
            using (var redis = clientsManager.GetClient())
            {
                var ids = new HashSet<string>();
                foreach (var channel in channels)
                {
                    var channelIds = redis.GetAllItemsFromSet(RedisIndex.ChannelSet.Fmt(channel));
                    foreach (var channelId in channelIds)
                    {
                        ids.Add(channelId);
                    }
                }

                var keys = ids.Map(x => RedisIndex.Subscription.Fmt(x));
                var infos = redis.GetValues<SubscriptionInfo>(keys);

                var metas = infos.Map(x => x.Meta);
                return metas;
            }
        }

        public List<Dictionary<string, string>> GetAllSubscriptionsDetails()
        {
            using (var redis = clientsManager.GetClient())
            {
                var ids = new HashSet<string>();

                var channelSetKeys = redis.ScanAllKeys(pattern: RedisIndex.ChannelSet.Fmt("*"));
                foreach (var channelSetKey in channelSetKeys)
                {
                    var channelIds = redis.GetAllItemsFromSet(channelSetKey);
                    foreach (var channelId in channelIds)
                    {
                        ids.Add(channelId);
                    }
                }

                var keys = ids.Map(x => RedisIndex.Subscription.Fmt(x));
                var infos = redis.GetValues<SubscriptionInfo>(keys);

                var metas = infos.Map(x => x.Meta);
                return metas;
            }
        }

        public List<SubscriptionInfo> GetAllSubscriptionInfos()
        {
            using (var redis = clientsManager.GetClient())
            {
                var ids = new HashSet<string>();

                var channelSetKeys = redis.ScanAllKeys(pattern: RedisIndex.ChannelSet.Fmt("*"));
                foreach (var channelSetKey in channelSetKeys)
                {
                    var channelIds = redis.GetAllItemsFromSet(channelSetKey);
                    foreach (var channelId in channelIds)
                    {
                        ids.Add(channelId);
                    }
                }

                var keys = ids.Map(x => RedisIndex.Subscription.Fmt(x));
                var infos = redis.GetValues<SubscriptionInfo>(keys);
                return infos;
            }
        }

        public bool Pulse(string subscriptionId)
        {
            using (var redis = clientsManager.GetClient())
            {
                var info = redis.Get<SubscriptionInfo>(RedisIndex.Subscription.Fmt(subscriptionId));
                if (info == null)
                    return false;

                redis.AddItemToSortedSet(RedisIndex.ActiveSubscriptionsSet,
                    info.SubscriptionId, RedisPubSub.CurrentServerTime.Ticks);

                NotifyRedis("pulse.id." + subscriptionId, null, null);

                return true;
            }
        }

        public void Reset()
        {
            local.Reset();
            using (var redis = clientsManager.GetClient())
            {
                var keysToDelete = new List<string> { RedisIndex.ActiveSubscriptionsSet };

                keysToDelete.AddRange(redis.SearchKeys(RedisIndex.Subscription.Replace("{0}", "*")));
                keysToDelete.AddRange(redis.SearchKeys(RedisIndex.ChannelSet.Replace("{0}", "*")));
                keysToDelete.AddRange(redis.SearchKeys(RedisIndex.UserIdSet.Replace("{0}", "*")));
                keysToDelete.AddRange(redis.SearchKeys(RedisIndex.UserNameSet.Replace("{0}", "*")));
                keysToDelete.AddRange(redis.SearchKeys(RedisIndex.SessionSet.Replace("{0}", "*")));
                redis.RemoveAll(keysToDelete);
            }
        }

        public void Start()
        {
            RedisPubSub.Start();
            local.Start();
        }

        public void Stop()
        {
            RedisPubSub.Stop();
            local.Stop();
        }

        protected void NotifyRedis(string key, string selector, object message, string channel = null)
        {
            using (var redis = clientsManager.GetClient())
            {
                var json = message?.ToJson();
                var sb = StringBuilderCache.Allocate().Append(key);

                if (selector != null)
                {
                    sb.Append(' ').Append(selector);

                    if (channel != null)
                    {
                        sb.Append('@');
                        sb.Append(channel);
                    }
                }

                if (json != null)
                {
                    sb.Append(' ');
                    sb.Append(json);
                }

                var msg = StringBuilderCache.ReturnAndFree(sb);

                redis.PublishMessage(Topic, msg);
            }
        }

        public void HandleMessage(string channel, string message)
        {
            OnMessage(message);
        }

        protected void OnMessage(string message)
        {
            var parts = message.SplitOnFirst(' ');
            var tokens = parts[0].Split('.');
            var cmd = tokens[0];

            switch (cmd)
            {
                case "notify":
                    var notify = tokens[1];
                    var who = tokens.Length > 2 ? tokens[2] : null;
                    var body = parts[1].SplitOnFirst(' ');
                    var selUri = body[0];
                    var selParts = selUri.SplitOnFirst('@');
                    var selector = selParts[0];
                    var channel = selParts.Length > 1 ? selParts[1] : null;
                    var msg = body.Length > 1 ? body[1] : null;

                    switch (notify)
                    {
                        case "all":
                            local.NotifyAll(selector, msg);
                            break;
                        case "channel":
                            local.NotifyChannel(who, selector, msg);
                            break;
                        case "subscription":
                            local.NotifySubscription(who, selector, msg, channel);
                            break;
                        case "userid":
                            local.NotifyUserId(who, selector, msg, channel);
                            break;
                        case "username":
                            local.NotifyUserName(who, selector, msg, channel);
                            break;
                        case "session":
                            local.NotifySession(who, selector, msg, channel);
                            break;
                    }
                    break;

                case "subscribe":
                    if (tokens[1] == "id" && parts.Length == 2)
                    {
                        var id = tokens.Length > 2 ? tokens[2] : null;
                        var channelsList = parts[1].FromJson<string>();
                        local.SubscribeToChannels(id, channelsList.Split(','));
                    }
                    break;

                case "unsubscribe":
                    if (tokens[1] == "id" && parts.Length == 2)
                    {
                        var id = tokens.Length > 2 ? tokens[2] : null;
                        var channelsList = parts[1].FromJson<string>();
                        local.UnsubscribeFromChannels(id, channelsList.Split(','));
                    }
                    break;

                case "unregister":
                    var unregister = tokens[1];
                    if (unregister == "id")
                    {
                        var id = tokens.Length > 2 ? tokens[2] : null;
                        local.UnRegister(id);
                    }
                    break;

                case "pulse":
                    var pulse = tokens[1];
                    if (pulse == "id")
                    {
                        var id = tokens.Length > 2 ? tokens[2] : null;
                        local.Pulse(id);
                    }
                    break;
            }
        }

        public void Dispose()
        {
            try
            {
                foreach (var entry in local.Subcriptions)
                {
                    var info = local.GetSubscriptionInfo(entry.Key);
                    if (info != null)
                    {
                        RemoveSubscriptionFromRedis(info);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Error trying to remove local.Subcriptions during Dispose()...", ex);
            }

            RedisPubSub?.Dispose();

            local?.Dispose();

            RedisPubSub = null;
            local = null;
        }
    }
}