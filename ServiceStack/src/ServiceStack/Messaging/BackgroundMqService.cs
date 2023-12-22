using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.Messaging;

public class BackgroundMqService : IMessageService
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(BackgroundMqService));

    /// <summary>
    /// How many times to retry processing messages before moving them to the DLQ 
    /// </summary>
    public int RetryCount { get; set; }

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
    /// If you only want to enable priority queue handlers (and threads) for specific msg types
    /// </summary>
    public string[] PriorityQueuesWhitelist { get; set; }

    /// <summary>
    /// Create workers for priority queues
    /// </summary>
    public bool EnablePriorityQueues
    {
        set => PriorityQueuesWhitelist = value ? null : TypeConstants.EmptyStringArray;
    }
        
    /// <summary>
    /// Disable Priority MQ's for Background MQ 
    /// </summary>
    public bool DisablePriorityQueues
    {
        set => EnablePriorityQueues = !value;
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
        set => PublishResponsesWhitelist = value ? TypeConstants.EmptyStringArray : null;
    }
        
    /// <summary>
    /// Opt-in to only publish .outq messages on this white list. 
    /// Publishes all responses by default.
    /// </summary>
    public string[] PublishToOutqWhitelist { get; set; }

    /// <summary>
    /// Don't publish any messages to .outq
    /// </summary>
    public bool DisablePublishingToOutq
    {
        set => PublishToOutqWhitelist = value ? TypeConstants.EmptyStringArray : null;
    }

    /// <summary>
    /// Subscribe to messages sent to .outq
    /// </summary>
    public List<Action<string, IMessage>> OutHandlers { get; } = new List<Action<string, IMessage>>();

    /// <summary>
    /// The max size of the Out MQ Collection in each Type (default 100)
    /// </summary>
    public int OutQMaxSize { get; set; } = 100;

    private readonly BackgroundMqClient mqClient;

    public IMessageFactory MessageFactory { get; }
        
    public List<Type> RegisteredTypes { get; }

    public BackgroundMqService()
    {
        EnablePriorityQueues = false;
        mqClient = new BackgroundMqClient(this);
        MessageFactory = new BackgroundMqMessageFactory(mqClient);
    }

    private readonly Dictionary<Type, IMqCollection> collectionsMap
        = new Dictionary<Type, IMqCollection>();

    private IMqWorker[] workers;
    private BlockingCollection<IMessage> unknownQueues;

    public void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn)
    {
        RegisterHandler(processMessageFn, null, noOfThreads:1);
    }

    public void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn, int noOfThreads)
    {
        RegisterHandler(processMessageFn, null, noOfThreads: noOfThreads);
    }

    public void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn,
        Action<IMessageHandler, IMessage<T>, Exception> processExceptionEx)
    {
        RegisterHandler(processMessageFn, processExceptionEx, noOfThreads: 1);
    }

    public void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn, 
        Action<IMessageHandler, IMessage<T>, Exception> processExceptionEx, int noOfThreads)
    {
        if (collectionsMap.ContainsKey(typeof(T)))
            throw new ArgumentException("Message handler has already been registered for type: " + typeof(T).Name);

        var handlerFactory = CreateMessageHandlerFactory(processMessageFn, processExceptionEx);
        collectionsMap[typeof(T)] = new BackgroundMqCollection<T>(mqClient, handlerFactory, noOfThreads, OutQMaxSize);
    }

    protected IMessageHandlerFactory CreateMessageHandlerFactory<T>(
        Func<IMessage<T>, object> processMessageFn,
        Action<IMessageHandler, IMessage<T>, Exception> processExceptionEx)
    {
        return new MessageHandlerFactory<T>(this, processMessageFn, processExceptionEx) {
            RequestFilter = this.RequestFilter,
            ResponseFilter = this.ResponseFilter,
            PublishResponsesWhitelist = PublishResponsesWhitelist,
            PublishToOutqWhitelist = PublishToOutqWhitelist,
            RetryCount = RetryCount,
        };
    }

    public IMessageHandlerStats GetStats()
    {
        AssertNotDisposed();
            
        lock (workers)
        {
            var total = new MessageHandlerStats("ALL WORKERS");
            workers.Each(x => total.Add(x.GetStats()));
            return total;
        }
    }

    public IMqWorker[] GetWorkers(string queueName)
    {
        var ret = new List<IMqWorker>();

        if (workers != null)
        {
            lock (workers)
            {
                foreach (var worker in workers)
                {
                    if (worker.QueueName != queueName)
                        continue;

                    ret.Add(worker);
                }
            }
        }

        return ret.ToArray();
    }

    public IMqWorker[] GetAllWorkers()
    {
        if (workers != null)
        {
            lock (workers)
            {
                return workers.ToArray();
            }
        }
        return TypeConstants<IMqWorker>.EmptyArray;
    }

    public string GetStatus() => workers != null ? nameof(WorkerStatus.Started) : nameof(WorkerStatus.Stopped);

    public string GetStatsDescription()
    {
        AssertNotDisposed();

        var sb = StringBuilderCache.Allocate()
            .AppendLine("# MQ SERVER STATS:")
            .AppendLine()
            .AppendLine("STATUS: " + GetStatus())
            .AppendLine();

        if (workers != null)
        {
            lock (workers)
            {
                sb.AppendLine("LISTENING ON: ");
                workers.Each(x => sb.AppendLine($"  {x.QueueName}"));
    
                sb.AppendLine();
                sb.AppendLine("------------------------------");
                sb.AppendLine().AppendLine("# COLLECTIONS:").AppendLine();
                sb.AppendLine("------------------------------");
                foreach (var x in collectionsMap.Values.ToList())
                {
                    sb.Append(x.GetDescription());
                    sb.AppendLine("------------------------------");
                }
    
                sb.AppendLine().AppendLine("# WORKERS:").AppendLine();
                sb.AppendLine("------------------------------");
                for (var i = 0; i < workers.Length; i++)
                {
                    var worker = workers[i];
                    sb.AppendLine($"WORKER {i+1} on {worker.QueueName} ");
                    sb.Append(worker.GetStats());
                    sb.AppendLine("------------------------------");
                }
            }
        }
                
        return StringBuilderCache.ReturnAndFree(sb);
    }

    public IMqCollection GetCollection(Type type)
    {
        return collectionsMap.TryGetValue(type, out var collection)
            ? collection
            : null;
    }

    private const string MessageQueueKey = "mq";
        
    private static Type GetMessageType(IMessage message)
    {
        if (message.Body != null)
            return message.Body.GetType();

        return message.GetType().GetGenericArguments()[0];
    }

    public void Publish(string queueName, IMessage message)
    {
        AssertNotDisposed();
            
        var msgType = GetMessageType(message);
        if (collectionsMap.TryGetValue(msgType, out var collection))
        {
            collection.Add(queueName, message);
        }
        else
        {
            if (Log.IsDebugEnabled)
                Log.Debug($"Publish message for '{queueName}' to unknownQueues");

            if (message is Message msg)
            {
                (msg.Meta ?? (msg.Meta = new Dictionary<string, string>()))[MessageQueueKey] = queueName;
                unknownQueues.Add(msg);
            }
            else
            {
                Log.Warn($"Could not queue message for '{queueName}' of unknown Message type '{message.GetType().Name}'");
            }
        }
    }

    public void Notify(string queueName, IMessage message)
    {
        if (Log.IsDebugEnabled)
            Log.Debug($"Publish message for '{queueName}' to outQueues");

        var msgType = GetMessageType(message);
        if (collectionsMap.TryGetValue(msgType, out var collection))
        {
            collection.Add(queueName, message);
        }
        else
        {
            Log.Warn($"Could not queue message for .outq '{queueName}' of unknown Message type '{message.GetType().Name}'");
        }

        if (Log.IsDebugEnabled)
            Log.Debug($"Sending '{queueName}' notification to {OutHandlers.Count} handler(s)");
            
        OutHandlers.Each(x => x(queueName, message));
    }

    public IMessage<T> Get<T>(string queueName, TimeSpan? timeout = null)
    {
        AssertNotDisposed();

        if (string.IsNullOrEmpty(queueName))
            throw new ArgumentNullException(nameof(queueName));
            
        var waitFor = timeout.GetValueOrDefault(Timeout.InfiniteTimeSpan);
            
        if (collectionsMap.TryGetValue(typeof(T), out var collection))
        {
            if (collection.TryTake(queueName, out var msg, waitFor))
            {
                return (IMessage<T>)msg;
            }
        }
        else
        {
            var started = DateTime.UtcNow;
    
            if (Log.IsDebugEnabled)
                Log.Debug($"Checking messages in unknownQueues for '{queueName}'");

            while (unknownQueues.TryTake(out var imsg, waitFor))
            {
                if (imsg is Message msg && msg.Meta != null && msg.Meta.TryGetValue(MessageQueueKey, out var msgQ) && msgQ == queueName)
                {
                    return (IMessage<T>)msg;
                }
                unknownQueues.Add(imsg); //re-add non matches back to queue
    
                if (DateTime.UtcNow - started > waitFor)
                    return null;
            }
        }
        return null;
    }

    public IMessage<T> TryGet<T>(string queueName)
    {
        AssertNotDisposed();
            
        if (collectionsMap.TryGetValue(typeof(T), out var collection))
        {
            if (collection.TryTake(queueName, out var msg))
            {
                return (IMessage<T>)msg;
            }
        }
        else
        {
            if (Log.IsDebugEnabled)
                Log.Debug($"Could not TryGet message from unknown '{queueName}'");
        }
        return null;
    }

    public void Start()
    {
        AssertNotDisposed();
            
        if (workers == null)
        {
            var workerBuilder = new List<IMqWorker>();

            foreach (var entry in collectionsMap)
            {
                var msgType = entry.Key;
                var collection = entry.Value;
                var queueNames = new QueueNames(msgType);

                if (PriorityQueuesWhitelist == null
                    || PriorityQueuesWhitelist.Any(x => x == msgType.Name))
                {
                    collection.ThreadCount.Times(i => 
                        workerBuilder.Add(collection.CreateWorker(queueNames.Priority)));
                }

                collection.ThreadCount.Times(i => 
                    workerBuilder.Add(collection.CreateWorker(queueNames.In)));
            }

            unknownQueues = new BlockingCollection<IMessage>();
            workers = workerBuilder.ToArray();
        }
    }

    public void Stop()
    {
        AssertNotDisposed();
            
        IMqWorker[] captureWorkers = null;
        if (workers != null)
        {
            lock (workers)
            {
                captureWorkers = workers;
                unknownQueues = null;
                workers = null;
            }
        }

        if (captureWorkers != null)
        {
            foreach (var worker in captureWorkers)
            {
                if (Log.IsDebugEnabled)
                    Log.Debug($"Stopping worker {worker.QueueName}...");
                    
                worker.Stop();
            }
        }
    }

    void AssertNotDisposed()
    {
        if (isDisposed)
            throw new ObjectDisposedException(GetType().Name);
    }

    private bool isDisposed = false;

    public void Dispose()
    {
        if (isDisposed)
            return;

        if (Log.IsDebugEnabled)
            Log.Debug($"Disposing {GetType().Name}...");
                    
        Stop();

        isDisposed = true;

        foreach (var entry in collectionsMap)
        {
            entry.Value.Dispose();
        }
            
        collectionsMap.Clear();
    }
}

public interface IMqCollection : IDisposable
{
    int ThreadCount { get; }
        
    Type QueueType { get; }

    IMqWorker CreateWorker(string mqName);

    void Add(string queueName, IMessage message);

    bool TryTake(string queueName, out IMessage message);

    bool TryTake(string queueName, out IMessage message, TimeSpan timeout);

    void Clear(string queueName);

    string GetDescription();

    Dictionary<string,long> GetDescriptionMap();
}
 
public interface IMqWorker : IDisposable
{
    string QueueName { get; }
        
    void Stop();

    IMessageHandlerStats GetStats();
}
 
public class BackgroundMqCollection<T> : IMqCollection
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(BackgroundMqWorker));

    public int ThreadCount { get; }
    public int OutQMaxSize { get; set; }
    public Type QueueType { get; } = typeof(T);

    public BackgroundMqClient MqClient { get; }

    public IMessageHandlerFactory HandlerFactory { get; }

    private readonly Dictionary<string, BlockingCollection<IMessage>> queueMap;

    private long totalMessagesAdded = 0;
    private long totalMessagesTaken = 0;

    private long totalOutQMessagesAdded = 0;
    private long totalDlQMessagesAdded = 0;
    public string[] QueueNames { get; }
        
    public BackgroundMqCollection(BackgroundMqClient mqClient, IMessageHandlerFactory handlerFactory, int threadCount, int outQMaxSize)
    {
        MqClient = mqClient;
        HandlerFactory = handlerFactory;
        ThreadCount = threadCount;
        OutQMaxSize = outQMaxSize;

        queueMap = new Dictionary<string, BlockingCollection<IMessage>> {
            { QueueNames<T>.In, new BlockingCollection<IMessage>() },
            { QueueNames<T>.Priority, new BlockingCollection<IMessage>() },
            { QueueNames<T>.Dlq, new BlockingCollection<IMessage>() },
            { QueueNames<T>.Out, new BlockingCollection<IMessage>() },
        };
        QueueNames = new[]
        {
            QueueNames<T>.In,
            QueueNames<T>.Priority,
            QueueNames<T>.Dlq,
            QueueNames<T>.Out,
        };
    }

    public void Add(string queueName, IMessage message)
    {
        if (queueMap.TryGetValue(queueName, out var mq))
        {
            mq.Add(message);

            if (queueName == QueueNames<T>.Out)
            {
                while (mq.Count > OutQMaxSize)
                {
                    if (mq.TryTake(out var overflowMsg) && Log.IsDebugEnabled)
                        Log.Debug($"Discarding .outq message {overflowMsg.Id} from overflowed outQueues, {mq.Count} remaining...");
                }
                Interlocked.Increment(ref totalOutQMessagesAdded);
            }
            else if (queueName == QueueNames<T>.Dlq)
            {
                Interlocked.Increment(ref totalDlQMessagesAdded);
            }
            else
            {
                Interlocked.Increment(ref totalMessagesAdded);
            }
                
            if (Log.IsDebugEnabled)
                Log.Debug($"Added new message to '{queueName}', total: {mq.Count}");
        }
        else
        {
            if (Log.IsDebugEnabled)
                Log.Debug("Ignoring message sent to unknown queue: " + queueName);
        }
    }

    public bool TryTake(string queueName, out IMessage message)
    {
        if (queueMap.TryGetValue(queueName, out var mq))
        {
            var ret = mq.TryTake(out message);
                
            if (ret && queueName != QueueNames<T>.Out && queueName != QueueNames<T>.Dlq)
                Interlocked.Increment(ref totalMessagesTaken);
                
            if (Log.IsDebugEnabled)
                Log.Debug($"Checking for next message in '{queueName}', found: {ret}, remaining: {mq.Count}");
                
            return ret;
        }

        message = null;
        return false;
    }
        
    public bool TryTake(string queueName, out IMessage message, TimeSpan timeout)
    {
        if (queueMap.TryGetValue(queueName, out var mq))
        {
            var ret = mq.TryTake(out message, timeout);
                
            if (ret && queueName != QueueNames<T>.Out && queueName != QueueNames<T>.Dlq)
                Interlocked.Increment(ref totalMessagesTaken);
                
            if (Log.IsDebugEnabled)
                Log.Debug($"Waiting for next message in '{queueName}', found: {ret}, remaining: {mq.Count}");

            return ret;
        }

        message = null;
        return false;
    }

    public void Clear(string queueName)
    {
        if (queueMap.TryGetValue(queueName, out var mq))
        {
            if (Log.IsDebugEnabled)
                Log.Debug($"Clearing '{queueName}' of {mq.Count} item(s)");

            while (mq.TryTake(out _)){}
        }
    }

    public IMqWorker CreateWorker(string mqName)
    {
        if (Log.IsDebugEnabled)
            Log.Debug("Creating BackgroundMqWorker for: " + mqName);
            
        var mq = queueMap[mqName];
        return new BackgroundMqWorker(mqName, mq, MqClient, HandlerFactory.CreateMessageHandler());
    }
        
    public string GetDescription()
    {
        var sb = StringBuilderCache.Allocate().AppendLine($"INFO {QueueType.Name}:")
            .AppendLine()
            .AppendLine($"STATS:")
            .AppendLine($"  Thread Count:         {ThreadCount}")
            .AppendLine($"  Total Messages Added: {Interlocked.Read(ref totalMessagesAdded)}")
            .AppendLine($"  Total Messages Taken: {Interlocked.Read(ref totalMessagesTaken)}")
            .AppendLine($"  Total .outq Messages: {Interlocked.Read(ref totalOutQMessagesAdded)}")
            .AppendLine($"  Total .dlq Messages:  {Interlocked.Read(ref totalDlQMessagesAdded)}")
            .AppendLine("QUEUES:");

        var longestKey = queueMap.Keys.Map(x => x.Length).OrderByDescending(x => x).FirstOrDefault();
            
        foreach (var entry in queueMap)
        {
            var keyWithPadding = $"{entry.Key}:".PadRight(Math.Max(longestKey + 1, 31), ' ');
            sb.AppendLine($"  {keyWithPadding} {entry.Value.Count} message(s)");
        }
            
        return StringBuilderCache.ReturnAndFree(sb);
    }

    public Dictionary<string, long> GetDescriptionMap()
    {
        var to = new Dictionary<string,long> {
            [nameof(ThreadCount)] = ThreadCount,
            ["TotalMessagesAdded"] = Interlocked.Read(ref totalMessagesAdded),
            ["TotalMessagesTaken"] = Interlocked.Read(ref totalMessagesTaken),
            ["TotalOutQMessagesAdded"] = Interlocked.Read(ref totalOutQMessagesAdded),
            ["TotalDlQMessagesAdded"] = Interlocked.Read(ref totalDlQMessagesAdded),
        };

        foreach (var entry in queueMap)
        {
            to[entry.Key] = entry.Value.Count;
        }

        return to;
    }

    public List<IMessage> GetQueueMessages(string queueName)
    {
        var to = new List<IMessage>();
        if (queueMap.TryGetValue(queueName, out var mq))
        {
            foreach (var msg in mq)
            {
                to.Add(msg);
            }
        }
        return to;
    }

    //Called when AppHost is disposing
    public void Dispose()
    {
        MqClient?.Dispose();

        foreach (var entry in queueMap)
        {
            entry.Value.Dispose();
        }            
        queueMap.Clear();
    }
}

public class BackgroundMqWorker : IMqWorker
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(BackgroundMqWorker));
    private Task bgTask;
    private CancellationTokenSource cts;

    private readonly BlockingCollection<IMessage> queue;
    private readonly BackgroundMqClient mqClient;
    private readonly IMessageHandler handler;
        
    public string QueueName { get; }

    public BackgroundMqWorker(string queueName, BlockingCollection<IMessage> queue, BackgroundMqClient mqClient, 
        IMessageHandler handler)
    {
        QueueName = queueName;
        this.queue = queue;
        this.mqClient = mqClient;
        this.handler = handler;

        cts = new CancellationTokenSource();
        bgTask = Task.Factory.StartNew(Run, null, TaskCreationOptions.LongRunning);
    }

    private Task Run(object state)
    {
        while (!cts.IsCancellationRequested)
        {
            foreach (var item in queue.GetConsumingEnumerable(cts.Token))
            {
                try
                {
                    if (Log.IsDebugEnabled)
                        Log.Debug($"[{QueueName}] ProcessMessage(): {item.Id}");
                        
                    handler.ProcessMessage(mqClient, item);
                }
                catch (Exception ex)
                {
                    Log.Error($"MQ {QueueName} failed to ProcessMessage with id {item.Id}: {ex.Message}", ex);
                }
            }
        }
            
        return TypeConstants.EmptyTask;
    }

    public IMessageHandlerStats GetStats()
    {
        return handler.GetStats();
    }

    public void Stop()
    {
        cts.Cancel();
    }

    public void Dispose()
    {
        new IDisposable[]{ cts, bgTask }.Dispose();
        cts = null;
        bgTask = null;
    }
}
    
public class BackgroundMqMessageFactory : IMessageFactory
{
    private readonly BackgroundMqClient mqClient;
    public BackgroundMqMessageFactory(BackgroundMqClient mqClient) => this.mqClient = mqClient;
    public IMessageQueueClient CreateMessageQueueClient() => mqClient;
    public IMessageProducer CreateMessageProducer() => mqClient;
    public void Dispose() {}
}

public class BackgroundMqClient : IMessageProducer, IMessageQueueClient, IOneWayClient
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(BackgroundMqClient));
        
    private readonly BackgroundMqService mqService;
        
    public BackgroundMqClient(BackgroundMqService mqService)
    {
        this.mqService = mqService;
    }

    public void Publish<T>(T messageBody)
    {
        if (messageBody is IMessage message)
        {
            Diagnostics.ServiceStack.Init(message);
            Publish(message.ToInQueueName(), message);
        }
        else
        {
            Publish(MessageFactory.Create(messageBody));
        }
    }

    public void Publish<T>(IMessage<T> message)
    {
        Publish(message.ToInQueueName(), message);
    }

    public void Publish(string queueName, IMessage message)
    {
        mqService.Publish(queueName, message);
    }

    public void Notify(string queueName, IMessage message)
    {
        mqService.Notify(queueName, message);
    }

    public IMessage<T> Get<T>(string queueName, TimeSpan? timeout = null)
    {
        return mqService.Get<T>(queueName, timeout);
    }

    public IMessage<T> GetAsync<T>(string queueName)
    {
        return mqService.TryGet<T>(queueName);
    }

    public void Ack(IMessage message)
    {
        //NOOP: message is removed at time of Get()
    }

    public void Nak(IMessage message, bool requeue, Exception exception = null)
    {
        var queueName = requeue
            ? message.ToInQueueName()
            : message.ToDlqQueueName();

        Publish(queueName, message);
    }

    public IMessage<T> CreateMessage<T>(object mqResponse)
    {
        return (IMessage<T>)mqResponse;
    }

    public string GetTempQueueName()
    {
        return QueueNames.GetTempQueueName();
    }
        
    public void SendOneWay(object requestDto)
    {
        Publish(MessageFactory.Create(requestDto));
    }

    public void SendOneWay(string queueName, object requestDto)
    {
        Publish(queueName, MessageFactory.Create(requestDto));
    }

    public void SendAllOneWay(IEnumerable<object> requests)
    {
        if (requests == null) return;
        foreach (var request in requests)
        {
            SendOneWay(request);
        }
    }

    public void Dispose() {}
}