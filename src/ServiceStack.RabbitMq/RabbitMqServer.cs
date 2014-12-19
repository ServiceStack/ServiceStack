using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using ServiceStack.Logging;
using ServiceStack.Messaging;

namespace ServiceStack.RabbitMq
{
    public class RabbitMqServer : IMessageService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RabbitMqServer));

        public const int DefaultRetryCount = 1; //Will be a total of 2 attempts

        /// <summary>
        /// The RabbitMQ.Client Connection factory to introspect connection properties and create a low-level connection
        /// </summary>
        public ConnectionFactory ConnectionFactory
        {
            get { return messageFactory.ConnectionFactory; }
        }

        /// <summary>
        /// Whether Rabbit MQ should auto-retry connecting when a connection to Rabbit MQ Server instance is dropped
        /// </summary>
        public bool AutoReconnect { get; set; }

        /// <summary>
        /// How many times a message should be retried before sending to the DLQ (Max of 1).
        /// </summary>
        public int RetryCount
        {
            get { return messageFactory.RetryCount; }
            set { messageFactory.RetryCount = value; }
        }

        /// <summary>
        /// Whether to use polling for consuming messages instead of a long-term subscription
        /// </summary>
        public bool UsePolling
        {
            get { return messageFactory.UsePolling; }
            set { messageFactory.UsePolling = value; }
        }

        /// <summary>
        /// Wait before Starting the MQ Server after a restart 
        /// </summary>
        public int? KeepAliveRetryAfterMs { get; set; }

        /// <summary>
        /// The Message Factory used by this MQ Server
        /// </summary>
        private RabbitMqMessageFactory messageFactory;
        public IMessageFactory MessageFactory
        {
            get { return messageFactory; }
        }

        public Action<string, IBasicProperties, IMessage> PublishMessageFilter
        {
            get { return messageFactory.PublishMessageFilter; }
            set { messageFactory.PublishMessageFilter = value; }
        }

        public Action<string, BasicGetResult> GetMessageFilter
        {
            get { return messageFactory.GetMessageFilter; }
            set { messageFactory.GetMessageFilter = value; }
        }

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

        /// <summary>
        /// Opt-in to only publish responses on this white list. 
        /// Publishes all responses by default.
        /// </summary>
        public string[] PublishResponsesWhitelist { get; set; }

        /// <summary>
        /// Don't publish any response messages
        /// </summary>
        public bool DisablePublishingResponses
        {
            set { PublishResponsesWhitelist = value ? new string[0] : null; }
        }

        private IConnection connection;
        private IConnection Connection
        {
            get { return connection ?? (connection = ConnectionFactory.CreateConnection()); }
        }

        private readonly Dictionary<Type, IMessageHandlerFactory> handlerMap
            = new Dictionary<Type, IMessageHandlerFactory>();

        private readonly Dictionary<Type, int> handlerThreadCountMap
            = new Dictionary<Type, int>();

        private RabbitMqWorker[] workers;
        private Dictionary<string, int[]> queueWorkerIndexMap;

        public List<Type> RegisteredTypes
        {
            get { return handlerMap.Keys.ToList(); }
        }

        public RabbitMqServer(string connectionString="localhost",
            string username = null, string password = null)
        {
            Init(new RabbitMqMessageFactory(connectionString, username, password));
        }

        public RabbitMqServer(RabbitMqMessageFactory messageFactory)
        {
            Init(messageFactory);
        }

        private void Init(RabbitMqMessageFactory messageFactory)
        {
            this.messageFactory = messageFactory;
            this.ErrorHandler = ex => Log.Error("Exception in Rabbit MQ Server: " + ex.Message, ex);
            RetryCount = DefaultRetryCount;
            AutoReconnect = true;
        }


        public void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn)
        {
            RegisterHandler(processMessageFn, null, noOfThreads: 1);
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
            return new MessageHandlerFactory<T>(this, processMessageFn, processExceptionEx)
            {
                RequestFilter = this.RequestFilter,
                ResponseFilter = this.ResponseFilter,
                PublishResponsesWhitelist = PublishResponsesWhitelist,               
                RetryCount = RetryCount,
            };
        }

        //Stats
        private long timesStarted = 0;
        private long noOfErrors = 0;
        private int noOfContinuousErrors = 0;
        private string lastExMsg = null;
        private int status;
        readonly object msgLock = new object();
        private long doOperation = WorkerOperation.NoOp;

        private Thread bgThread; //Subscription controller thread
        private long bgThreadCount = 0;
        public long BgThreadCount
        {
            get { return Interlocked.CompareExchange(ref bgThreadCount, 0, 0); }
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

        public void Init()
        {
            if (workers != null) return;

            var workerBuilder = new List<RabbitMqWorker>();

            using (var channel = Connection.OpenChannel())
            {
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
                            workerBuilder.Add(new RabbitMqWorker(
                                messageFactory,
                                handlerFactory.CreateMessageHandler(),
                                queueNames.Priority,
                                WorkerErrorHandler,
                                AutoReconnect)));

                    }

                    noOfThreads.Times(i =>
                        workerBuilder.Add(new RabbitMqWorker(
                                messageFactory,
                                handlerFactory.CreateMessageHandler(),
                                queueNames.In,
                                WorkerErrorHandler,
                                AutoReconnect)));

                    channel.RegisterQueues(queueNames);
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
            if (Interlocked.CompareExchange(ref status, WorkerStatus.Starting, WorkerStatus.Stopped) == WorkerStatus.Stopped)
            {
                //Should only be 1 thread past this point
                try
                {
                    Init();

                    if (workers == null || workers.Length == 0)
                    {
                        Log.Warn("Cannot start a MQ Server with no Message Handlers registered, ignoring.");
                        Interlocked.CompareExchange(ref status, WorkerStatus.Stopped, WorkerStatus.Starting);
                        return;
                    }

                    StartWorkerThreads();

                    //Don't kill us if we're the thread that's retrying to Start() after a failure.
                    if (bgThread != Thread.CurrentThread)
                    {
                        KillBgThreadIfExists();

                        bgThread = new Thread(RunLoop)
                        {
                            IsBackground = true,
                            Name = "Rabbit MQ Server " + Interlocked.Increment(ref bgThreadCount)
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
                    if (this.ErrorHandler != null)
                        this.ErrorHandler(ex);
                    else
                        throw;
                }
            }
        }

        private void RunLoop()
        {
            if (Interlocked.CompareExchange(ref status, WorkerStatus.Started, WorkerStatus.Starting) != WorkerStatus.Starting) return;
            Interlocked.Increment(ref timesStarted);

            try
            {
                lock (msgLock)
                {
                    //RESET
                    while (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Started)
                    {
                        Monitor.Wait(msgLock);
                        Log.Debug("msgLock received...");

                        var op = Interlocked.CompareExchange(ref doOperation, WorkerOperation.NoOp, doOperation);
                        switch (op)
                        {
                            case WorkerOperation.Stop:
                                Log.Debug("Stop Command Issued");

                                if (Interlocked.CompareExchange(ref status, WorkerStatus.Stopped, WorkerStatus.Started) != WorkerStatus.Started)
                                    Interlocked.CompareExchange(ref status, WorkerStatus.Stopped, WorkerStatus.Stopping);

                                StopWorkerThreads();
                                return; //exits

                            case WorkerOperation.Restart:
                                Log.Debug("Restart Command Issued");

                                if (Interlocked.CompareExchange(ref status, WorkerStatus.Stopped, WorkerStatus.Started) != WorkerStatus.Started)
                                    Interlocked.CompareExchange(ref status, WorkerStatus.Stopped, WorkerStatus.Stopping);

                                StopWorkerThreads();
                                StartWorkerThreads();

                                Interlocked.CompareExchange(ref status, WorkerStatus.Started, WorkerStatus.Stopped);
                                break; //continues
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

                StopWorkerThreads();

                if (this.ErrorHandler != null)
                    this.ErrorHandler(ex);

                if (KeepAliveRetryAfterMs != null)
                {
                    Thread.Sleep(KeepAliveRetryAfterMs.Value);
                    Start();
                }
            }
            Log.Debug("Exiting RunLoop()...");
        }

        public void Stop()
        {
            if (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Disposed)
                throw new ObjectDisposedException("MQ Host has been disposed");

            if (Interlocked.CompareExchange(ref status, WorkerStatus.Stopping, WorkerStatus.Started) == WorkerStatus.Started)
            {
                lock (msgLock)
                {
                    Interlocked.CompareExchange(ref doOperation, WorkerOperation.Stop, doOperation);
                    Monitor.Pulse(msgLock);
                }
            }
        }

        public void Restart()
        {
            if (Interlocked.CompareExchange(ref status, 0, 0) == WorkerStatus.Disposed)
                throw new ObjectDisposedException("MQ Host has been disposed");

            if (Interlocked.CompareExchange(ref status, WorkerStatus.Stopping, WorkerStatus.Started) == WorkerStatus.Started)
            {
                lock (msgLock)
                {
                    Interlocked.CompareExchange(ref doOperation, WorkerOperation.Restart, doOperation);
                    Monitor.Pulse(msgLock);
                }
            }
        }

        public void StartWorkerThreads()
        {
            Log.Debug("Starting all Rabbit MQ Server worker threads...");
            foreach (var worker in workers)
            {
                try
                {
                    worker.Start();
                }
                catch (Exception ex)
                {
                    if (this.ErrorHandler != null) this.ErrorHandler(ex);
                    Log.Warn("Could not START Rabbit MQ worker thread: " + ex.Message);
                }
            }
        }

        public void StopWorkerThreads()
        {
            Log.Debug("Stopping all Rabbit MQ Server worker threads...");
            foreach (var worker in workers)
            {
                try
                {
                    worker.Stop();
                }
                catch (Exception ex)
                {
                    if (this.ErrorHandler != null) this.ErrorHandler(ex);
                    Log.Warn("Could not STOP Rabbit MQ worker thread: " + ex.Message);
                }
            }
        }

        void DisposeWorkerThreads()
        {
            Log.Debug("Disposing all Rabbit MQ Server worker threads...");
            if (workers != null) Array.ForEach(workers, x => x.Dispose());
        }

        void WorkerErrorHandler(RabbitMqWorker source, Exception ex)
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

        public void Dispose()
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

            try
            {
                if (connection != null)
                {
                    connection.Dispose();
                    connection = null;
                }
            }
            catch (Exception ex)
            {
                if (this.ErrorHandler != null) this.ErrorHandler(ex);
            }

        }
    }
}
