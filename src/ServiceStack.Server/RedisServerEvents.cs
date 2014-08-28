using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ServiceStack.Logging;
using ServiceStack.Messaging;
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
            get { return local.Timeout; }
            set { local.Timeout = value; }
        }

        public Action<IEventSubscription> OnSubscribe
        {
            get { return local.OnSubscribe; }
            set { local.OnSubscribe = value; }
        }

        public Action<IEventSubscription> OnUnsubscribe
        {
            get { return local.OnUnsubscribe; }
            set { local.OnUnsubscribe = value; }
        }

        public bool NotifyChannelOfSubscriptions
        {
            get { return local.NotifyChannelOfSubscriptions; }
            set { local.NotifyChannelOfSubscriptions = value; }
        }

        public static string Topic = "sse:topic";

        public class RedisIndex
        {
            public const string Subscription = "sse:id:{0}";
            public const string ChannelSet = "sse:channel:{0}";
            public const string UserIdSet = "sse:userid:{0}";
            public const string UserNameSet = "sse:username:{0}";
            public const string SessionSet = "sse:session:{0}";
        }

        public IRedisClientsManager clientsManager;

        public RedisServerEvents(IRedisClientsManager clientsManager)
        {
            this.clientsManager = clientsManager;

            this.ErrorHandler = ex => Log.Error("Exception in RedisServerEvents: " + ex.Message, ex);
            this.KeepAliveRetryAfterMs = 2000;

            var failoverHost = clientsManager as IRedisFailover;
            if (failoverHost != null)
            {
                failoverHost.OnFailover.Add(OnFailover);
            }

            local = new MemoryServerEvents
            {
                NotifyJoin = s => NotifyChannel(s.Channel, "cmd.onJoin", s.Meta),
                NotifyLeave = s => NotifyChannel(s.Channel, "cmd.onLeave", s.Meta),
                Serialize = o => (string)o, //Already a seiralized JSON string
            };

            var appHost = HostContext.AppHost;
            var feature = appHost != null ? appHost.GetPlugin<ServerEventsFeature>() : null;
            if (feature != null)
            {
                Timeout = feature.Timeout;
                OnSubscribe = feature.OnSubscribe;
                OnUnsubscribe = feature.OnUnsubscribe;
                NotifyChannelOfSubscriptions = feature.NotifyChannelOfSubscriptions;
            }
        }

        public void NotifyAll(string selector, object message)
        {
            NotifyRedis("notify.all", selector, message);
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

        public void Register(IEventSubscription sub)
        {
            if (sub == null)
                throw new ArgumentNullException("subscription");

            local.Register(sub);

            var info = sub.GetInfo();
            using (var redis = clientsManager.GetClient())
            {
                StoreSubscriptionInfo(redis, info);
            }
        }

        private void StoreSubscriptionInfo(IRedisClient redis, SubscriptionInfo info)
        {
            var id = info.SubscriptionId;
            using (var trans = redis.CreateTransaction())
            {
                trans.QueueCommand(r => r.Set(RedisIndex.Subscription.Fmt(id), info));
                trans.QueueCommand(r => r.ExpireEntryIn(RedisIndex.Subscription.Fmt(id), Timeout));

                trans.QueueCommand(r => r.AddItemToSet(RedisIndex.ChannelSet.Fmt(info.Channel), id));
                trans.QueueCommand(r => r.ExpireEntryIn(RedisIndex.ChannelSet.Fmt(info.Channel), Timeout));

                trans.QueueCommand(r => r.AddItemToSet(RedisIndex.UserIdSet.Fmt(info.UserId), id));
                trans.QueueCommand(r => r.ExpireEntryIn(RedisIndex.UserIdSet.Fmt(info.UserId), Timeout));

                if (info.UserName != null)
                {
                    trans.QueueCommand(r => r.AddItemToSet(RedisIndex.UserNameSet.Fmt(info.UserName), id));
                    trans.QueueCommand(r => r.ExpireEntryIn(RedisIndex.UserNameSet.Fmt(info.UserName), Timeout));
                }
                if (info.SessionId != null)
                {
                    trans.QueueCommand(r => r.AddItemToSet(RedisIndex.SessionSet.Fmt(info.SessionId), id));
                    trans.QueueCommand(r => r.ExpireEntryIn(RedisIndex.SessionSet.Fmt(info.SessionId), Timeout));
                }

                trans.Commit();
            }
        }

        public void UnRegister(string subscriptionId)
        {
            var info = GetSubscriptionInfo(subscriptionId);
            if (info == null)
                return;

            var id = info.SubscriptionId;
            var keys = new List<string>(new[]
            {
                RedisIndex.Subscription.Fmt(id),
                RedisIndex.ChannelSet.Fmt(info.Channel),
                RedisIndex.UserIdSet.Fmt(info.UserId),
            });

            if (info.UserName != null)
                keys.Add(RedisIndex.UserNameSet.Fmt(info.UserName));

            if (info.SessionId != null)
                keys.Add(RedisIndex.SessionSet.Fmt(info.SessionId));

            using (var redis = clientsManager.GetClient())
            {
                redis.RemoveAll(keys);
            }

            NotifyRedis("unregister.id." + subscriptionId, null, null);
        }

        public long GetNextSequence(string sequenceId)
        {
            using (var redis = clientsManager.GetClient())
            {
                return redis.Increment("sse:seq:" + sequenceId, 1);
            }
        }

        public List<Dictionary<string, string>> GetSubscriptionsDetails(string channel = null)
        {
            using (var redis = clientsManager.GetClient())
            {
                var ids = redis.GetAllItemsFromSet(RedisIndex.ChannelSet.Fmt(channel));
                var keys = ids.Map(x => RedisIndex.Subscription.Fmt(x));
                var infos = redis.GetValues<SubscriptionInfo>(keys);

                var metas = infos.Map(x => x.Meta);
                return metas;
            }
        }

        public bool Pulse(string subscriptionId)
        {
            using (var redis = clientsManager.GetClient())
            {
                var info = redis.Get<SubscriptionInfo>(RedisIndex.Subscription.Fmt(subscriptionId));
                if (info == null)
                    return false;

                StoreSubscriptionInfo(redis, info);
                return true;
            }
        }

        public void Reset()
        {
            using (var redis = clientsManager.GetClient())
            {
                redis.FlushDb();
            }
        }

        protected void NotifyRedis(string key, string selector, object message, string channel = null)
        {
            using (var redis = clientsManager.GetClient())
            {
                var json = message != null ? message.ToJson() : null;
                var sb = new StringBuilder(key);

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

                var msg = sb.ToString();

                redis.PublishMessage(Topic, msg);
            }
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

                case "unregister":
                    var unregister = tokens[1];
                    if (unregister == "id")
                    {
                        var id = tokens.Length > 2 ? tokens[2] : null;
                        local.UnRegister(id);
                    }
                    break;
            }
        }

        public Action<Exception> ErrorHandler { get; set; }
        public int? KeepAliveRetryAfterMs { get; set; }

        private int doOperation = WorkerOperation.NoOp;

        private long timesStarted = 0;
        private long noOfErrors = 0;
        private int noOfContinuousErrors = 0;
        private string lastExMsg = null;
        private int status;
        private Thread bgThread; //Subscription controller thread
        private long bgThreadCount = 0;

        public long BgThreadCount
        {
            get { return Interlocked.CompareExchange(ref bgThreadCount, 0, 0); }
        }

        public void Start()
        {
            if (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Started)
            {
                //Start any stopped worker threads
                return;
            }
            if (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Disposed)
                throw new ObjectDisposedException("RedisServerEvents has been disposed");

            //Only 1 thread allowed past
            if (Interlocked.CompareExchange(ref status, WorkerStatus.Starting, WorkerStatus.Stopped) == WorkerStatus.Stopped) //Should only be 1 thread past this point
            {
                try
                {
                    Init();

                    SleepBackOffMultiplier(Interlocked.CompareExchange(ref noOfContinuousErrors, 0, 0));

                    //Don't kill us if we're the thread that's retrying to Start() after a failure.
                    if (bgThread != Thread.CurrentThread)
                    {
                        KillBgThreadIfExists();

                        bgThread = new Thread(RunLoop)
                        {
                            IsBackground = true,
                            Name = "RedisServerEvents " + Interlocked.Increment(ref bgThreadCount)
                        };
                        bgThread.Start();
                        Log.Debug("Started Background Thread: " + bgThread.Name);
                    }
                    else
                    {
                        Log.Debug("Retrying RunLoop() on Thread: " + bgThread.Name);
                        RunLoop();
                    }
                }
                catch (Exception ex)
                {
                    ex.Message.Print();
                    if (this.ErrorHandler != null) this.ErrorHandler(ex);
                }
            }
        }

        private void Init()
        {
        }

        private IRedisClient masterClient;
        private void RunLoop()
        {
            if (Interlocked.CompareExchange(ref status, WorkerStatus.Started, WorkerStatus.Starting) != WorkerStatus.Starting) return;
            Interlocked.Increment(ref timesStarted);

            try
            {
                //RESET
                while (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Started)
                {
                    using (var redisClient = clientsManager.GetReadOnlyClient())
                    {
                        masterClient = redisClient;

                        //Record that we had a good run...
                        Interlocked.CompareExchange(ref noOfContinuousErrors, 0, noOfContinuousErrors);

                        using (var subscription = redisClient.CreateSubscription())
                        {
                            subscription.OnUnSubscribe = channel => Log.Debug("OnUnSubscribe: " + channel);

                            subscription.OnMessage = (channel, msg) =>
                            {
                                if (msg == WorkerOperation.ControlCommand)
                                {
                                    var op = Interlocked.CompareExchange(ref doOperation, WorkerOperation.NoOp, doOperation);
                                    switch (op)
                                    {
                                        case WorkerOperation.Stop:
                                            Log.Debug("Stop Command Issued");

                                            if (Interlocked.CompareExchange(ref status, WorkerStatus.Stopped, WorkerStatus.Started) != WorkerStatus.Started)
                                                Interlocked.CompareExchange(ref status, WorkerStatus.Stopped, WorkerStatus.Stopping);

                                            Log.Debug("UnSubscribe From All Channels...");
                                            subscription.UnSubscribeFromAllChannels(); //Un block thread.
                                            return;

                                        case WorkerOperation.Reset:
                                            subscription.UnSubscribeFromAllChannels(); //Un block thread.
                                            return;
                                    }
                                }

                                if (!string.IsNullOrEmpty(msg))
                                {
                                    OnMessage(msg);
                                }
                            };

                            subscription.SubscribeToChannels(Topic); //blocks thread
                            masterClient = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lastExMsg = ex.Message;
                Interlocked.Increment(ref noOfErrors);
                Interlocked.Increment(ref noOfContinuousErrors);

                if (Interlocked.CompareExchange(ref status, WorkerStatus.Stopped, WorkerStatus.Started) != WorkerStatus.Started)
                    Interlocked.CompareExchange(ref status, WorkerStatus.Stopped, WorkerStatus.Stopping);

                if (this.ErrorHandler != null)
                    this.ErrorHandler(ex);


                if (KeepAliveRetryAfterMs != null)
                {
                    Thread.Sleep(KeepAliveRetryAfterMs.Value);
                    Start();
                }
            }
        }

        public void Stop()
        {
            if (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Disposed)
                throw new ObjectDisposedException("RedisServerEvents has been disposed");

            if (Interlocked.CompareExchange(ref status, WorkerStatus.Stopping, WorkerStatus.Started) == WorkerStatus.Started)
            {
                Log.Debug("Stopping RedisServerEvents...");

                //Unblock current bgthread by issuing StopCommand
                try
                {
                    using (var redis = clientsManager.GetClient())
                    {
                        Interlocked.CompareExchange(ref doOperation, WorkerOperation.Stop, doOperation);
                        redis.PublishMessage(Topic, WorkerOperation.ControlCommand);
                    }
                }
                catch (Exception ex)
                {
                    if (this.ErrorHandler != null) this.ErrorHandler(ex);
                    Log.Warn("Could not send STOP message to bg thread: " + ex.Message);
                }
            }
        }

        private void OnFailover(IRedisClientsManager clientsManager)
        {
            try
            {
                if (masterClient != null)
                {
                    //New thread-safe client with same connection info as connected master
                    using (var currentlySubscribedClient = ((RedisClient)masterClient).CloneClient())
                    {
                        Interlocked.CompareExchange(ref doOperation, WorkerOperation.Reset, doOperation);
                        currentlySubscribedClient.PublishMessage(Topic, WorkerOperation.ControlCommand);
                    }
                }
                else
                {
                    Restart();
                }
            }
            catch (Exception ex)
            {
                if (this.ErrorHandler != null) this.ErrorHandler(ex);
                Log.Warn("Error trying to UnSubscribeFromChannels in OnFailover. Restarting...", ex);
                Restart();
            }
        }

        public void Restart()
        {
            Stop();
            Start();
        }

        private void KillBgThreadIfExists()
        {
            if (bgThread != null && bgThread.IsAlive)
            {
                //give it a small chance to die gracefully
                if (!bgThread.Join(500))
                {
                    //Ideally we shouldn't get here, but lets try our hardest to clean it up
                    Log.Warn("Interrupting previous Background Thread: " + bgThread.Name);
                    bgThread.Interrupt();
                    if (!bgThread.Join(TimeSpan.FromSeconds(3)))
                    {
                        Log.Warn(bgThread.Name + " just wont die, so we're now aborting it...");
                        bgThread.Abort();
                    }
                }
                bgThread = null;
            }
        }

        readonly Random rand = new Random(Environment.TickCount);
        private void SleepBackOffMultiplier(int continuousErrorsCount)
        {
            if (continuousErrorsCount == 0) return;
            const int MaxSleepMs = 60 * 1000;

            //exponential/random retry back-off.
            var nextTry = Math.Min(
                rand.Next((int)Math.Pow(continuousErrorsCount, 3), (int)Math.Pow(continuousErrorsCount + 1, 3) + 1),
                MaxSleepMs);

            Log.Debug("Sleeping for {0}ms after {1} continuous errors".Fmt(nextTry, continuousErrorsCount));

            Thread.Sleep(nextTry);
        }

        public virtual void Dispose()
        {
            if (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Disposed)
                return;

            Stop();

            if (Interlocked.CompareExchange(ref status, WorkerStatus.Disposed, WorkerStatus.Stopped) != WorkerStatus.Stopped)
                Interlocked.CompareExchange(ref status, WorkerStatus.Disposed, WorkerStatus.Stopping);

            try
            {
                Thread.Sleep(100); //give it a small chance to die gracefully
                KillBgThreadIfExists();
            }
            catch (Exception ex)
            {
                if (this.ErrorHandler != null) this.ErrorHandler(ex);
            }
        }
    }
}