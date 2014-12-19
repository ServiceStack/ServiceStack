//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
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

        public int RetryCount { get; set; }

        public TimeSpan? WaitBeforeNextRestart
        {
            get { return RedisPubSub.WaitBeforeNextRestart; }
            set { RedisPubSub.WaitBeforeNextRestart = value; }
        }

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

        public IRedisPubSubServer RedisPubSub { get; set; }

        private readonly IRedisClientsManager clientsManager; //Thread safe redis client/conn factory

        public IRedisClientsManager ClientsManager
        {
            get { return clientsManager; }
        }

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
            RedisPubSub = new RedisPubSubServer(clientsManager, QueueNames.TopicIn)
            {
                OnInit = OnInit,
                OnStart = OnStart,
                OnStop = OnStop,
                OnError = OnError,
                OnMessage = OnMessage,
            };

            this.RetryCount = retryCount;
            //this.RequestTimeOut = requestTimeOut;
            this.MessageFactory = new RedisMessageFactory(clientsManager);
            this.ErrorHandler = ex => Log.Error("Exception in Redis MQ Server: " + ex.Message, ex);
            this.WaitBeforeNextRestart = TimeSpan.FromMilliseconds(2000);
        }

        public void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn)
        {
            RegisterHandler(processMessageFn, null, noOfThreads:1);
        }

        public void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn, int noOfThreads)
        {
            RegisterHandler(processMessageFn, null, noOfThreads);
        }

        public void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn, Action<IMessageHandler, IMessage<T>, Exception> processExceptionEx)
        {
            RegisterHandler(processMessageFn, processExceptionEx, noOfThreads: 1);
        }

        public void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn, Action<IMessageHandler, IMessage<T>, Exception> processExceptionEx, int noOfThreads)
        {
            if (handlerMap.ContainsKey(typeof(T)))
            {
                throw new ArgumentException("Message handler has already been registered for type: " + typeof(T).Name);
            }

            handlerMap[typeof(T)] = CreateMessageHandlerFactory(processMessageFn, processExceptionEx);
            handlerThreadCountMap[typeof(T)] = noOfThreads;

            LicenseUtils.AssertValidUsage(LicenseFeature.ServiceStack, QuotaType.Operations, handlerMap.Count);
        }

        protected IMessageHandlerFactory CreateMessageHandlerFactory<T>(Func<IMessage<T>, object> processMessageFn, Action<IMessageHandler, IMessage<T>, Exception> processExceptionEx)
        {
            return new MessageHandlerFactory<T>(this, processMessageFn, processExceptionEx) {
                RequestFilter = this.RequestFilter,
                ResponseFilter = this.ResponseFilter,
                PublishResponsesWhitelist = PublishResponsesWhitelist,
                RetryCount = RetryCount,
            };
        }

        public void OnError(Exception ex)
        {
            if (ErrorHandler != null)
                ErrorHandler(ex);
        }

        public void OnStart()
        {
            StartWorkerThreads();
        }

        public void OnStop()
        {
            StopWorkerThreads();
        }

        public void OnInit()
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

        public void OnMessage(string channel, string msg)
        {
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
        }

        public void Start()
        {
            RedisPubSub.Start();
        }

        public void Stop()
        {
            RedisPubSub.Stop();
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

        public IMessageHandlerStats GetStats()
        {
            lock (workers)
            {
                var total = new MessageHandlerStats("All Handlers");
                workers.ToList().ForEach(x => total.Add(x.GetStats()));
                return total;
            }
        }

        public string GetStatus()
        {
            return RedisPubSub.GetStatus();
        }

        public string GetStatsDescription()
        {
            lock (workers)
            {
                var sb = new StringBuilder("#MQ SERVER STATS:\n");
                sb.AppendLine("Listening On: " + string.Join(", ", workers.ToList().ConvertAll(x => x.QueueName).ToArray()));
                sb.Append(RedisPubSub.GetStatsDescription());

                foreach (var worker in workers)
                {
                    sb.AppendLine(worker.GetStats().ToString());
                    sb.AppendLine("---------------\n");
                }
                return sb.ToString();
            }
        }

        public virtual void Dispose()
        {
            try
            {
                DisposeWorkerThreads();
            }
            catch (Exception ex)
            {
                Log.Error("Error DisposeWorkerThreads(): ", ex);
            }

            RedisPubSub.Dispose();
        }

        public List<string> WorkerThreadsStatus()
        {
            return workers.ToList().ConvertAll(x => x.GetStatus());
        }

        public long ExpireTemporaryQueues(int afterMs = 10*60*1000)
        {
            using (var redis = clientsManager.GetClient())
            {
                var tmpWildCard = QueueNames.TempMqPrefix + "*";
                var itemsExpired = redis.ExecLuaAsInt(@"
                        local count = 0
                        local pattern = KEYS[1]
                        local timeMs = tonumber(ARGV[1])
                        local keys = redis.call('KEYS',pattern)
                        for i,k in pairs(keys) do
                            count = count + 1
                            redis.call('PEXPIRE', k, timeMs)
                        end
                        return count
                    ",
                    keys: new[] { tmpWildCard },
                    args: new[] { afterMs.ToString() });

                return itemsExpired;
            }
        }
    }
}