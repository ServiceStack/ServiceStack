using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Logging;
using ServiceStack.Redis;
using ServiceStack.Text;

namespace ServiceStack;

public class RedisServerEvents : IServerEvents
{
    private static ILog Log = LogManager.GetLogger(typeof(RedisServerEvents));

    public MemoryServerEvents Local { get; private set; }

    public TimeSpan Timeout
    {
        get => Local.IdleTimeout;
        set => Local.IdleTimeout = value;
    }

    public TimeSpan HouseKeepingInterval
    {
        get => Local.HouseKeepingInterval;
        set => Local.HouseKeepingInterval = value;
    }

    public Func<IEventSubscription, Task> OnSubscribeAsync
    {
        get => Local.OnSubscribeAsync;
        set => Local.OnSubscribeAsync = value;
    }

    public Func<IEventSubscription, Task> OnUnsubscribeAsync
    {
        get => Local.OnUnsubscribeAsync;
        set => Local.OnUnsubscribeAsync = value;
    }

    public Func<IEventSubscription, Task> OnUpdateAsync
    {
        get => Local.OnUpdateAsync;
        set => Local.OnUpdateAsync = value;
    }

    public bool NotifyChannelOfSubscriptions
    {
        get => Local.NotifyChannelOfSubscriptions;
        set => Local.NotifyChannelOfSubscriptions = value;
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

    public readonly IRedisClientsManager clientsManager;

    public IRedisPubSubServer RedisPubSub { get; set; }

    public RedisServerEvents(IRedisPubSubServer redisPubSub)
    {
        this.RedisPubSub = redisPubSub;
        this.clientsManager = redisPubSub.ClientsManager;
        redisPubSub.OnInit = OnInit;
        redisPubSub.OnError = ex => Log.Error("Exception in RedisServerEvents: " + ex.Message, ex);
        redisPubSub.OnMessage = HandleMessage;

        WaitBeforeNextRestart = TimeSpan.FromMilliseconds(2000);

        Local = new MemoryServerEvents
        {
            NotifyJoinAsync = HandleOnJoinAsync,
            NotifyLeaveAsync = HandleOnLeaveAsync,
            NotifyUpdateAsync = HandleOnUpdate,
            NotifyHeartbeatAsync = NotifyHeartbeatAsync,
            Serialize = HandleSerialize,
            OnRemoveSubscriptionAsync = HandleOnRemoveSubscriptionAsync
        };

        var appHost = HostContext.AppHost;
        var feature = appHost?.GetPlugin<ServerEventsFeature>();
        if (feature != null)
        {
            Timeout = feature.IdleTimeout;
            HouseKeepingInterval = feature.HouseKeepingInterval;
            OnSubscribeAsync = feature.OnSubscribeAsync;
            OnUnsubscribeAsync = feature.OnUnsubscribeAsync;
            OnUpdateAsync = feature.OnUpdateAsync;
            NotifyChannelOfSubscriptions = feature.NotifyChannelOfSubscriptions;
        }
    }

    private Task HandleOnRemoveSubscriptionAsync(IEventSubscription sub)
    {
        var info = sub.GetInfo();
        RemoveSubscriptionFromRedis(info);

        return TypeConstants.EmptyTask;
    }

    private void OnInit()
    {
        UnRegisterExpiredSubscriptions();
    }

    public void UnRegisterExpiredSubscriptions()
    {
        using var redis = clientsManager.GetClient();
        var lastPulseBefore = (RedisPubSub.CurrentServerTime - Timeout).Ticks;
        var expiredSubIds = redis.GetRangeFromSortedSetByLowestScore(
            RedisIndex.ActiveSubscriptionsSet, 0, lastPulseBefore);
                
        UnRegisterSubIds(redis, expiredSubIds);
    }

    public void UnRegisterSubIds(IRedisClient redis, List<string> expiredSubIds)
    {
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

    private static List<SubscriptionInfo> GetSubscriptionInfos(IRedisClient redis, IEnumerable<string> subIds)
    {
        var keys = subIds.Map(x => RedisIndex.Subscription.Fmt(x));
        var infos = redis.GetValues<SubscriptionInfo>(keys);
        return infos;
    }

    public RedisServerEvents(IRedisClientsManager clientsManager)
        : this(new RedisPubSubServer(clientsManager, Topic)) { }

    Task HandleOnJoinAsync(IEventSubscription sub)
    {
        return NotifyChannelsAsync(sub.Channels, "cmd.onJoin", sub.Meta);
    }

    Task HandleOnLeaveAsync(IEventSubscription sub)
    {
        return NotifyChannelsAsync(sub.Channels, "cmd.onLeave", sub.Meta);
    }

    Task HandleOnUpdate(IEventSubscription sub)
    {
        using (var redis = clientsManager.GetClient())
        {
            StoreSubscriptionInfo(redis, sub.GetInfo());
        }
        return NotifyChannelsAsync(sub.Channels, "cmd.onUpdate", sub.Meta);
    }

    Task NotifyHeartbeatAsync(IEventSubscription sub) =>
        NotifySubscriptionAsync(sub.SubscriptionId, "cmd.onHeartbeat", sub.Meta);

    private void RemoveSubscriptionFromRedis(SubscriptionInfo info)
    {
        var id = info.SubscriptionId;

        using var redis = clientsManager.GetClient();
        using var trans = redis.CreateTransaction();
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

    string HandleSerialize(object o)
    {
        return (string)o; //Already a serialized JSON string
    }

    public void NotifyAll(string selector, object message) => NotifyRedis("notify.all", selector, message);

    public Task NotifyAllAsync(string selector, object message, CancellationToken token = default) => NotifyRedisAsync("notify.all", selector, message, token:token);
    public Task NotifyAllJsonAsync(string selector, string json, CancellationToken token = default) => NotifyRedisRawAsync("notify.all", selector, json, token:token);

    public void NotifyChannels(string[] channels, string selector, IDictionary<string, string> meta)
    {
        foreach (var channel in channels)
        {
            var msg = new Dictionary<string, string>(meta) { { "channel", channel } };
            NotifyRedis("notify.channel." + channel, selector, msg);
        }
    }

    public Task NotifyChannelsAsync(string[] channels, string selector, IDictionary<string, string> meta, CancellationToken token=default)
    {
        NotifyChannels(channels, selector, meta);
        return TypeConstants.EmptyTask;
    }

    public void NotifyChannel(string channel, string selector, object message) => NotifyRedis("notify.channel." + channel, selector, message);

    public Task NotifyChannelAsync(string channel, string selector, object message, CancellationToken token = default) =>
        NotifyRedisAsync("notify.channel." + channel, selector, message, token: token);

    public Task NotifyChannelJsonAsync(string channel, string selector, string json, CancellationToken token = default) =>
        NotifyRedisRawAsync("notify.channel." + channel, selector, json, token: token);

    public void NotifySubscription(string subscriptionId, string selector, object message, string channel = null) =>
        NotifyRedis("notify.subscription." + subscriptionId, selector, message, channel);

    public Task NotifySubscriptionAsync(string subscriptionId, string selector, object message, string channel = null, CancellationToken token = default) =>
        NotifyRedisAsync("notify.subscription." + subscriptionId, selector, message, channel, token);

    public Task NotifySubscriptionJsonAsync(string subscriptionId, string selector, string json, string channel = null, CancellationToken token = default) =>
        NotifyRedisRawAsync("notify.subscription." + subscriptionId, selector, json, channel, token);

    public void NotifyUserId(string userId, string selector, object message, string channel = null) => 
        NotifyRedis("notify.userid." + userId, selector, message, channel);

    public Task NotifyUserIdAsync(string userId, string selector, object message, string channel = null, CancellationToken token = default) =>
        NotifyRedisAsync("notify.userid." + userId, selector, message, channel, token);

    public Task NotifyUserIdJsonAsync(string userId, string selector, string json, string channel = null, CancellationToken token = default) =>
        NotifyRedisRawAsync("notify.userid." + userId, selector, json, channel, token);

    public void NotifyUserName(string userName, string selector, object message, string channel = null) => 
        NotifyRedis("notify.username." + userName, selector, message, channel);

    public Task NotifyUserNameAsync(string userName, string selector, object message, string channel = null, CancellationToken token = default) =>
        NotifyRedisAsync("notify.username." + userName, selector, message, channel, token);

    public Task NotifyUserNameJsonAsync(string userName, string selector, string json, string channel = null, CancellationToken token = default) =>
        NotifyRedisRawAsync("notify.username." + userName, selector, json, channel, token);

    public void NotifySession(string sessionId, string selector, object message, string channel = null) =>
        NotifyRedis("notify.session." + sessionId, selector, message, channel);

    public Task NotifySessionAsync(string sessionId, string selector, object message, string channel = null, CancellationToken token = default) =>
        NotifyRedisAsync("notify.session." + sessionId, selector, message, channel, token);

    public Task NotifySessionJsonAsync(string sessionId, string selector, string json, string channel = null, CancellationToken token = default) =>
        NotifyRedisRawAsync("notify.session." + sessionId, selector, json, channel, token);

    public SubscriptionInfo GetSubscriptionInfo(string id)
    {
        using var redis = clientsManager.GetClient();
        var info = redis.Get<SubscriptionInfo>(RedisIndex.Subscription.Fmt(id));
        return info;
    }

    public List<SubscriptionInfo> GetSubscriptionInfosByUserId(string userId)
    {
        using var redis = clientsManager.GetClient();
        var ids = redis.GetAllItemsFromSet(RedisIndex.UserIdSet.Fmt(userId));
        var keys = ids.Map(x => RedisIndex.Subscription.Fmt(x));
        var infos = redis.GetValues<SubscriptionInfo>(keys);

        return infos;
    }

    public async Task RegisterAsync(IEventSubscription sub, Dictionary<string, string> connectArgs = null, CancellationToken token=default)
    {
        if (sub == null)
            throw new ArgumentNullException(nameof(sub));

        var info = sub.GetInfo();
        using (var redis = clientsManager.GetClient())
        {
            StoreSubscriptionInfo(redis, info);
        }

        if (connectArgs != null)
            await sub.PublishAsync("cmd.onConnect", connectArgs.ToJson(), token).ConfigAwait();

        await Local.RegisterAsync(sub, token: token).ConfigAwait();
    }

    private void StoreSubscriptionInfo(IRedisClient redis, SubscriptionInfo info)
    {
        var id = info.SubscriptionId;
        using var trans = redis.CreateTransaction();
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

    public void UnRegister(string subscriptionId)
    {
        var info = GetSubscriptionInfo(subscriptionId);
        if (info == null)
            return;

        NotifyRedis("unregister.id." + subscriptionId, null, null);
    }

    public Task UnRegisterAsync(string subscriptionId, CancellationToken token = default)
    {
        UnRegister(subscriptionId);
        return TypeConstants.EmptyTask;
    }

    public long GetNextSequence(string sequenceId)
    {
        using (var redis = clientsManager.GetClient())
        {
            return redis.Increment("sse:seq:" + sequenceId, 1);
        }
    }

    public int RemoveExpiredSubscriptions() => Local.RemoveExpiredSubscriptions();

    public Task<int> RemoveExpiredSubscriptionsAsync(CancellationToken token=default) => Local.RemoveExpiredSubscriptionsAsync(token);

    public void SubscribeToChannels(string subscriptionId, string[] channels)
    {
        var info = GetSubscriptionInfo(subscriptionId);
        if (info == null)
            return;

        NotifyRedis("subscribe.id." + subscriptionId, null, channels.Join(","));
    }

    public Task SubscribeToChannelsAsync(string subscriptionId, string[] channels, CancellationToken token = default)
    {
        SubscribeToChannels(subscriptionId, channels);
        return TypeConstants.EmptyTask;
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

    public Task UnsubscribeFromChannelsAsync(string subscriptionId, string[] channels, CancellationToken token = default)
    {
        UnsubscribeFromChannels(subscriptionId, channels);
        return TypeConstants.EmptyTask;
    }

    public void QueueAsyncTask(Func<Task> task) => Local.QueueAsyncTask(task);
    public MemoryServerEvents GetMemoryServerEvents() => Local;

    public List<Dictionary<string, string>> GetSubscriptionsDetails(params string[] channels)
    {
        using var redis = clientsManager.GetClient();
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

        var metas = infos.Map(x => x.Meta.ToDictionary());
        return metas;
    }

    public List<Dictionary<string, string>> GetAllSubscriptionsDetails()
    {
        using var redis = clientsManager.GetClient();
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

        var metas = infos.Map(x => x.Meta.ToDictionary());
        return metas;
    }

    public List<SubscriptionInfo> GetAllSubscriptionInfos()
    {
        using var redis = clientsManager.GetClient();
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

    public Task<bool> PulseAsync(string subscriptionId, CancellationToken token=default)
    {
        using var redis = clientsManager.GetClient();
        var info = redis.Get<SubscriptionInfo>(RedisIndex.Subscription.Fmt(subscriptionId));
        if (info == null)
            return TypeConstants.FalseTask;

        redis.AddItemToSortedSet(RedisIndex.ActiveSubscriptionsSet,
            info.SubscriptionId, RedisPubSub.CurrentServerTime.Ticks);

        NotifyRedis("pulse.id." + subscriptionId, null, null);

        return TypeConstants.TrueTask;
    }

    public void Reset()
    {
        Local.Reset();
        using var redis = clientsManager.GetClient();
        var keysToDelete = new List<string> { RedisIndex.ActiveSubscriptionsSet };

        keysToDelete.AddRange(redis.SearchKeys(RedisIndex.Subscription.Replace("{0}", "*")));
        keysToDelete.AddRange(redis.SearchKeys(RedisIndex.ChannelSet.Replace("{0}", "*")));
        keysToDelete.AddRange(redis.SearchKeys(RedisIndex.UserIdSet.Replace("{0}", "*")));
        keysToDelete.AddRange(redis.SearchKeys(RedisIndex.UserNameSet.Replace("{0}", "*")));
        keysToDelete.AddRange(redis.SearchKeys(RedisIndex.SessionSet.Replace("{0}", "*")));
        redis.RemoveAll(keysToDelete);
    }

    public void Start()
    {
        RedisPubSub.Start();
        Local.Start();
    }

    public void Stop()
    {
        RedisPubSub?.Stop();
        Local?.Stop();
    }

    public async Task StopAsync()
    {
        RedisPubSub?.Stop();
        if (Local != null) await Local.StopAsync();
    }

    public Dictionary<string, string> GetStats() => Local.GetStats();

    protected Task NotifyRedisAsync(string key, string selector, object message, string channel = null, CancellationToken token=default)
    {
        NotifyRedis(key, selector, message, channel);
        return TypeConstants.EmptyTask;
    }

    protected Task NotifyRedisRawAsync(string key, string selector, string json, string channel = null, CancellationToken token=default)
    {
        NotifyRedisRaw(key, selector, json, channel);
        return TypeConstants.EmptyTask;
    }
        
    protected void NotifyRedisRaw(string key, string selector, string json, string channel = null)
    {
        using var redis = clientsManager.GetClient();
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

    protected void NotifyRedis(string key, string selector, object message, string channel = null) =>
        NotifyRedisRaw(key, selector, message?.ToJson(), channel);

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
                        Local.NotifyAll(selector, msg);
                        break;
                    case "channel":
                        Local.NotifyChannel(who, selector, msg);
                        break;
                    case "subscription":
                        Local.NotifySubscription(who, selector, msg, channel);
                        break;
                    case "userid":
                        Local.NotifyUserId(who, selector, msg, channel);
                        break;
                    case "username":
                        Local.NotifyUserName(who, selector, msg, channel);
                        break;
                    case "session":
                        Local.NotifySession(who, selector, msg, channel);
                        break;
                }
                break;

            case "subscribe":
                if (tokens[1] == "id" && parts.Length == 2)
                {
                    var id = tokens.Length > 2 ? tokens[2] : null;
                    var channelsList = parts[1].FromJson<string>();
                    Local.SubscribeToChannels(id, channelsList.Split(','));
                }
                break;

            case "unsubscribe":
                if (tokens[1] == "id" && parts.Length == 2)
                {
                    var id = tokens.Length > 2 ? tokens[2] : null;
                    var channelsList = parts[1].FromJson<string>();
                    Local.UnsubscribeFromChannels(id, channelsList.Split(','));
                }
                break;

            case "unregister":
                var unregister = tokens[1];
                if (unregister == "id")
                {
                    var id = tokens.Length > 2 ? tokens[2] : null;
                    Local.UnRegister(id);
                }
                break;

            case "pulse":
                var pulse = tokens[1];
                if (pulse == "id")
                {
                    var id = tokens.Length > 2 ? tokens[2] : null;
                    Local.Pulse(id);
                }
                break;
        }
    }

    public void Dispose()
    {
        try
        {
            foreach (var entry in Local.Subscriptions)
            {
                var info = Local.GetSubscriptionInfo(entry.Key);
                if (info != null)
                {
                    RemoveSubscriptionFromRedis(info);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warn("Error trying to remove local.Subscriptions during Dispose()...", ex);
        }

        RedisPubSub?.Dispose();

        Local?.Dispose();

        RedisPubSub = null;
        Local = null;
    }
}