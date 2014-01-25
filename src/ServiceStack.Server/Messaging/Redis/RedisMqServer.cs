//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ServiceStack.Logging;
using ServiceStack.Redis;
using ServiceStack.Text;

namespace ServiceStack.Messaging.Redis
{
    /// <summary>
    /// Creates a Redis MQ Server that processes each message on its own background thread.
    /// i.e. if you register 3 handlers it will create 7 background threads:
    ///   - 1 listening to the Redis MQ Subscription, getting notified of each new message
    ///   - 3x1 Normal InQ for each message handler
    ///   - 3x1 PriorityQ for each message handler (Turn off with DisablePriorityQueues)
    /// 
    /// When RedisMqServer Starts it creates a background thread subscribed to the Redis MQ Topic that
    /// listens for new incoming messages. It also starts 2 background threads for each message type:
    ///  - 1 for processing the services Priority Queue and 1 processing the services normal Inbox Queue.
    /// 
    /// Priority Queue's can be enabled on a message-per-message basis by specifying types in the 
    /// OnlyEnablePriortyQueuesForTypes property. The DisableAllPriorityQueues property disables all Queues.
    /// 
    /// The Start/Stop methods are idempotent i.e. It's safe to call them repeatedly on multiple threads 
    /// and the Redis MQ Server will only have Started or Stopped once.
    /// </summary>
    public class RedisMqServer : IMessageService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RedisMqServer));
        public const int DefaultRetryCount = 1; //Will be a total of 2 attempts

        public int RetryCount { get; protected set; }

        public int? KeepAliveRetryAfterMs { get; set; }

        public IMessageFactory MessageFactory { get; private set; }

        public Func<string, IOneWayClient> ReplyClientFactory { get; set; }

        /// <summary>
        /// Execute global transformation or custom logic before a request is processed.
        /// Must be thread-safe.
        /// </summary>
        public Func<IMessage, IMessage> RequestFilter { get; set; }

        /// <summary>
        /// Execute global transformation or custom logic on the response.
        /// Must be thread-safe.
        /// </summary>
        public Func<object, object> ResponseFilter { get; set; }

        /// <summary>
        /// Execute global error handler logic. Must be thread-safe.
        /// </summary>
        public Action<Exception> ErrorHandler { get; set; }

        /// <summary>
        /// If you only want to enable priority queue handlers (and threads) for specific msg types
        /// </summary>
        public string[] PriortyQueuesWhitelist { get; set; }

        /// <summary>
        /// Don't listen on any Priority Queues
        /// </summary>
        public bool DisablePriorityQueues
        {
            set
            {
                PriortyQueuesWhitelist = new string[0];
            }
        }

        private readonly IRedisClientsManager clientsManager; //Thread safe redis client/conn factory

        public IMessageQueueClient CreateMessageQueueClient()
        {
            return new RedisMessageQueueClient(this.clientsManager, null);
        }

        /// <summary>
        /// Opt-in to only publish responses on this white list. 
        /// Publishes all responses by default.
        /// </summary>
        public string[] PublishResponsesWhitelist { get; set; }

        public bool DisablePublishingResponses
        {
            set { PublishResponsesWhitelist = value ? new string[0] : null; }
        }

        //Stats
        private long timesStarted = 0;
        private long noOfErrors = 0;
        private int noOfContinuousErrors = 0;
        private string lastExMsg = null;
        private int status;
        private int doOperation = WorkerOperation.NoOp;

        private Thread bgThread; //Subscription controller thread
        private long bgThreadCount = 0;
        public long BgThreadCount
        {
            get { return Interlocked.CompareExchange(ref bgThreadCount, 0, 0); }
        }

        private readonly Dictionary<Type, IMessageHandlerFactory> handlerMap
            = new Dictionary<Type, IMessageHandlerFactory>();

        private readonly Dictionary<Type, int> handlerThreadCountMap
            = new Dictionary<Type, int>();

        private MessageHandlerWorker[] workers;
        private Dictionary<string, int[]> queueWorkerIndexMap;

        public List<Type> RegisteredTypes
        {
            get { return handlerMap.Keys.ToList(); }
        }

        public RedisMqServer(IRedisClientsManager clientsManager,
            int retryCount = DefaultRetryCount, TimeSpan? requestTimeOut = null)
        {
            this.clientsManager = clientsManager;
            this.RetryCount = retryCount;
            //this.RequestTimeOut = requestTimeOut;
            this.MessageFactory = new RedisMessageFactory(clientsManager);
            this.ErrorHandler = ex => Log.Error("Exception in Redis MQ Server: " + ex.Message, ex);
            this.KeepAliveRetryAfterMs = 2000;

            var failoverHost = clientsManager as IRedisFailover;
            if (failoverHost != null)
            {
                failoverHost.OnFailover.Add(OnFailover);
            }
        }

        public void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn)
        {
            RegisterHandler(processMessageFn, null, noOfThreads:1);
        }

        public void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn, int noOfThreads)
        {
            RegisterHandler(processMessageFn, null, noOfThreads);
        }

        public void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn, Action<IMessage<T>, Exception> processExceptionEx)
        {
            RegisterHandler(processMessageFn, processExceptionEx, noOfThreads: 1);
        }

        public void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn, Action<IMessage<T>, Exception> processExceptionEx, int noOfThreads)
        {
            if (handlerMap.ContainsKey(typeof(T)))
            {
                throw new ArgumentException("Message handler has already been registered for type: " + typeof(T).Name);
            }

            handlerMap[typeof(T)] = CreateMessageHandlerFactory(processMessageFn, processExceptionEx);
            handlerThreadCountMap[typeof(T)] = noOfThreads;

            LicenseUtils.AssertValidUsage(LicenseFeature.ServiceStack, QuotaType.Operations, handlerMap.Count);
        }

        protected IMessageHandlerFactory CreateMessageHandlerFactory<T>(Func<IMessage<T>, object> processMessageFn, Action<IMessage<T>, Exception> processExceptionEx)
        {
            return new MessageHandlerFactory<T>(this, processMessageFn, processExceptionEx) {
                RequestFilter = this.RequestFilter,
                ResponseFilter = this.ResponseFilter,
                PublishResponsesWhitelist = PublishResponsesWhitelist,
                RetryCount = RetryCount,
            };
        }

        public void Init()
        {
            if (workers == null)
            {
                var workerBuilder = new List<MessageHandlerWorker>();

                foreach (var entry in handlerMap)
                {
                    var msgType = entry.Key;
                    var handlerFactory = entry.Value;
                    
                    var queueNames = new QueueNames(msgType);
                    var noOfThreads = handlerThreadCountMap[msgType];

                    if (PriortyQueuesWhitelist == null
                        || PriortyQueuesWhitelist.Any(x => x == msgType.Name))
                    {
                        noOfThreads.Times(i =>
                            workerBuilder.Add(new MessageHandlerWorker(
                                clientsManager,
                                handlerFactory.CreateMessageHandler(),
                                queueNames.Priority,
                                WorkerErrorHandler)));
                    }

                    noOfThreads.Times(i =>
                        workerBuilder.Add(new MessageHandlerWorker(
                            clientsManager,
                            handlerFactory.CreateMessageHandler(),
                            queueNames.In,
                            WorkerErrorHandler)));
                }

                workers = workerBuilder.ToArray();

                queueWorkerIndexMap = new Dictionary<string, int[]>();
                for (var i = 0; i < workers.Length; i++)
                {
                    var worker = workers[i];

                    int[] workerIds;
                    if (!queueWorkerIndexMap.TryGetValue(worker.QueueName, out workerIds))
                    {
                        queueWorkerIndexMap[worker.QueueName] = new[] { i };
                    }
                    else
                    {
                        workerIds = new List<int>(workerIds) { i }.ToArray();
                        queueWorkerIndexMap[worker.QueueName] = workerIds;
                    }
                }
            }
        }

        public void Start()
        {
            if (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Started)
            {
                //Start any stopped worker threads
                StartWorkerThreads();
                return;
            }
            if (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Disposed)
                throw new ObjectDisposedException("MQ Host has been disposed");

            //Only 1 thread allowed past
            if (Interlocked.CompareExchange(ref status, WorkerStatus.Starting, WorkerStatus.Stopped) == WorkerStatus.Stopped) //Should only be 1 thread past this point
            {
                try
                {
                    Init();

                    if (workers == null || workers.Length == 0)
                    {
                        Log.Warn("Cannot start a MQ Server with no Message Handlers registered, ignoring.");
                        Interlocked.CompareExchange(ref status, WorkerStatus.Stopped, WorkerStatus.Starting);
                        return;
                    }

                    SleepBackOffMultiplier(Interlocked.CompareExchange(ref noOfContinuousErrors, 0, 0));

                    StartWorkerThreads();

                    //Don't kill us if we're the thread that's retrying to Start() after a failure.
                    if (bgThread != Thread.CurrentThread)
                    {
                        KillBgThreadIfExists();

                        bgThread = new Thread(RunLoop)
                        {
                            IsBackground = true,
                            Name = "Redis MQ Server " + Interlocked.Increment(ref bgThreadCount)
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
                                    int[] workerIndexes;
                                    if (queueWorkerIndexMap.TryGetValue(msg, out workerIndexes))
                                    {
                                        foreach (var workerIndex in workerIndexes)
                                        {
                                            workers[workerIndex].NotifyNewMessage();
                                        }
                                    }
                                }
                            };

                            subscription.SubscribeToChannels(QueueNames.TopicIn); //blocks thread
                            masterClient = null;
                        }
                    }
                }

                StopWorkerThreads();
            }
            catch (Exception ex)
            {
                lastExMsg = ex.Message;
                Interlocked.Increment(ref noOfErrors);
                Interlocked.Increment(ref noOfContinuousErrors);

                if (Interlocked.CompareExchange(ref status, WorkerStatus.Stopped, WorkerStatus.Started) != WorkerStatus.Started)
                    Interlocked.CompareExchange(ref status, WorkerStatus.Stopped, WorkerStatus.Stopping);

                StopWorkerThreads();

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
                throw new ObjectDisposedException("MQ Host has been disposed");

            if (Interlocked.CompareExchange(ref status, WorkerStatus.Stopping, WorkerStatus.Started) == WorkerStatus.Started)
            {
                Log.Debug("Stopping MQ Host...");

                //Unblock current bgthread by issuing StopCommand
                try
                {
                    using (var redis = clientsManager.GetClient())
                    {
                        Interlocked.CompareExchange(ref doOperation, WorkerOperation.Stop, doOperation);
                        redis.PublishMessage(QueueNames.TopicIn, WorkerOperation.ControlCommand);
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
                        currentlySubscribedClient.PublishMessage(QueueNames.TopicIn, WorkerOperation.ControlCommand);
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

        public void NotifyAll()
        {
            Log.Debug("Notifying all worker threads to check for new messages...");
            foreach (var worker in workers)
            {
                worker.NotifyNewMessage();
            }
        }

        public void StartWorkerThreads()
        {
            Log.Debug("Starting all Redis MQ Server worker threads...");
            Array.ForEach(workers, x => x.Start());
        }

        public void ForceRestartWorkerThreads()
        {
            Log.Debug("ForceRestart all Redis MQ Server worker threads...");
            Array.ForEach(workers, x => x.ForceRestart());
        }

        public void StopWorkerThreads()
        {
            Log.Debug("Stopping all Redis MQ Server worker threads...");
            Array.ForEach(workers, x => x.Stop());
        }

        void DisposeWorkerThreads()
        {
            Log.Debug("Disposing all Redis MQ Server worker threads...");
            if (workers != null) Array.ForEach(workers, x => x.Dispose());
        }

        void WorkerErrorHandler(MessageHandlerWorker source, Exception ex)
        {
            Log.Error("Received exception in Worker: " + source.QueueName, ex);
            for (int i = 0; i < workers.Length; i++)
            {
                var worker = workers[i];
                if (worker == source)
                {
                    Log.Debug("Starting new {0} Worker at index {1}...".Fmt(source.QueueName, i));
                    workers[i] = source.Clone();
                    workers[i].Start();
                    worker.Dispose();
                    return;
                }
            }
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
                DisposeWorkerThreads();
            }
            catch (Exception ex)
            {
                Log.Error("Error DisposeWorkerThreads(): ", ex);
            }

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

        public string GetStatus()
        {
            switch (Interlocked.CompareExchange(ref status, 0, 0))
            {
                case WorkerStatus.Disposed:
                    return "Disposed";
                case WorkerStatus.Stopped:
                    return "Stopped";
                case WorkerStatus.Stopping:
                    return "Stopping";
                case WorkerStatus.Starting:
                    return "Starting";
                case WorkerStatus.Started:
                    return "Started";
            }
            return null;
        }

        public IMessageHandlerStats GetStats()
        {
            lock (workers)
            {
                var total = new MessageHandlerStats("All Handlers");
                workers.ToList().ForEach(x => total.Add(x.GetStats()));
                return total;
            }
        }

        public string GetStatsDescription()
        {
            lock (workers)
            {
                var sb = new StringBuilder("#MQ SERVER STATS:\n");
                sb.AppendLine("===============");
                sb.AppendLine("Current Status: " + GetStatus());
                sb.AppendLine("Listening On: " + string.Join(", ", workers.ToList().ConvertAll(x => x.QueueName).ToArray()));
                sb.AppendLine("Times Started: " + Interlocked.CompareExchange(ref timesStarted, 0, 0));
                sb.AppendLine("Num of Errors: " + Interlocked.CompareExchange(ref noOfErrors, 0, 0));
                sb.AppendLine("Num of Continuous Errors: " + Interlocked.CompareExchange(ref noOfContinuousErrors, 0, 0));
                sb.AppendLine("Last ErrorMsg: " + lastExMsg);
                sb.AppendLine("===============");
                foreach (var worker in workers)
                {
                    sb.AppendLine(worker.GetStats().ToString());
                    sb.AppendLine("---------------\n");
                }
                return sb.ToString();
            }
        }

        public List<string> WorkerThreadsStatus()
        {
            return workers.ToList().ConvertAll(x => x.GetStatus());
        }
    }
}