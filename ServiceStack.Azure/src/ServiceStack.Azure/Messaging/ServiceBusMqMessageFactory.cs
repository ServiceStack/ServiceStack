using ServiceStack.Messaging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using ServiceStack.Text;

#if NETCORE
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
#else
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
#endif


namespace ServiceStack.Azure.Messaging;

public class ServiceBusMqMessageFactory : IMessageFactory
{
    private long timesStarted;
    private long doOperation = WorkerOperation.NoOp;
    private long noOfErrors = 0;
    private int noOfContinuousErrors = 0;
    private string lastExMsg = null;

#if NETCORE
    public Action<Microsoft.Azure.ServiceBus.Message,IMessage> PublishMessageFilter { get; set; }
#else
    public Action<BrokeredMessage,IMessage> PublishMessageFilter { get; set; }
#endif

        
    protected internal readonly string address;
#if !NETCORE
    protected internal readonly NamespaceManager namespaceManager;
#else
    protected internal readonly ManagementClient managementClient;
#endif

    internal Dictionary<Type, IMessageHandlerFactory> handlerMap;
    Dictionary<string, Type> queueMap;

    // A list of all Service Bus QueueClients - one per type & queue (priorityq, inq, outq, and dlq)
    private static readonly ConcurrentDictionary<string, QueueClient> sbClients = new();

    public ServiceBusMqServer MqServer { get; }

    public ServiceBusMqMessageFactory(ServiceBusMqServer mqServer, string address)
    {
        this.MqServer = mqServer;
        this.address = address;
#if !NETCORE
        this.namespaceManager = NamespaceManager.CreateFromConnectionString(address);
#else
        this.managementClient = new ManagementClient(address);
#endif
    }

    public IMessageProducer CreateMessageProducer()
    {
        return new ServiceBusMqMessageProducer(this) {
            PublishMessageFilter = PublishMessageFilter,
        };
    }

    public IMessageQueueClient CreateMessageQueueClient()
    {
        return new ServiceBusMqClient(this);
    }

    public IMessageHandler GetMessageHandler(Type msgType)
    {
        var messageHandlerFactory = handlerMap[msgType];
        var messageHandler = messageHandlerFactory.CreateMessageHandler();
        return messageHandler;
    }
    
    public void Dispose()
    {
        this.status = WorkerStatus.Disposed;
    }

    protected internal void StartQueues(Dictionary<Type, IMessageHandlerFactory> handlerMap, Dictionary<Type, int> handlerThreadCountMap)
    {
        this.status = WorkerStatus.Starting;
        // Create queues for each registered type
        this.handlerMap = handlerMap;

        queueMap = new Dictionary<string, Type>();

        var mqSuffixes = new[] {".inq", ".outq", ".priorityq", ".dlq"};
        foreach (var type in this.handlerMap.Keys)
        {
            foreach (var mqSuffix in mqSuffixes)
            {
                var queueName = QueueNames.ResolveQueueNameFn(type.Name, mqSuffix);
                queueName = queueName.SafeQueueName()!;

                if (!queueMap.ContainsKey(queueName))
                    queueMap.Add(queueName, type);

                var mqDesc = new QueueDescription(queueName);
#if !NETCORE
                if (!namespaceManager.QueueExists(queueName))
                    namespaceManager.CreateQueue(mqDesc);
#else
                // Prefer GetAwaiter().GetResult() so that the StackTrace
                // is easier to use, see:
                // https://stackoverflow.com/a/36427080
                if (!managementClient.QueueExistsAsync(mqDesc.Path).GetAwaiter().GetResult())
                    managementClient.CreateQueueAsync(mqDesc).GetAwaiter().GetResult();
#endif
            }

            var mqNames = new QueueNames(type);
            AddQueueHandler(type, mqNames.In, handlerThreadCountMap[type]);
            AddQueueHandler(type, mqNames.Priority, handlerThreadCountMap[type]);
        }
        this.status = WorkerStatus.Started;
    }

    List<ServiceBusMqWorker> workers = new();
    private void AddQueueHandler(Type queueType, string queueName, int threadCount=1)
    {
        queueName = queueName.SafeQueueName()!;

#if NETCORE
        var sbClient = new QueueClient(address, queueName, ReceiveMode.PeekLock);
        var messageHandler = this.GetMessageHandler(queueType);
        var sbWorker = new ServiceBusMqWorker(messageHandler, CreateMessageQueueClient(), queueName, sbClient);
        workers.Add(sbWorker);
        sbClient.RegisterMessageHandler(sbWorker.HandleMessageAsync,
            new MessageHandlerOptions(
                (eventArgs) => Task.CompletedTask)
            {
                MaxConcurrentCalls = threadCount,
                AutoComplete = false
            });
#else
        var sbClient = QueueClient.CreateFromConnectionString(address, queueName, ReceiveMode.PeekLock);
        var options = new OnMessageOptions
        {
            // Cannot use AutoComplete because our HandleMessage throws errors into SS's handlers; this would
            // normally release the BrokeredMessage back to the Azure Service Bus queue, which we don't actually want

            AutoComplete = false,
            //AutoRenewTimeout = new TimeSpan()
            MaxConcurrentCalls = threadCount
        };

        var messageHandler = this.GetMessageHandler(queueType);
        var sbWorker = new ServiceBusMqWorker(messageHandler, CreateMessageQueueClient(), queueName, sbClient);
        workers.Add(sbWorker);
        sbClient.OnMessage(sbWorker.HandleMessage, options);
#endif
        sbClients.GetOrAdd(queueName, sbClient);
    }

    protected internal void StopQueues()
    {
        this.status = WorkerStatus.Stopping;
#if NETCORE
        sbClients.Each(async kvp => await kvp.Value.CloseAsync());
#else
        sbClients.Each(kvp => kvp.Value.Close());
#endif
        sbClients.Clear();
        this.status = WorkerStatus.Stopped;
    }

    protected internal QueueClient GetOrCreateClient(string queueName)
    {
        queueName = queueName.SafeQueueName()!;

        if (sbClients.ContainsKey(queueName))
            return sbClients[queueName];

        var qd = new QueueDescription(queueName);

#if !NETCORE
        // Create queue on ServiceBus namespace if it doesn't exist
        if (!namespaceManager.QueueExists(queueName))
        {
            try
            {
                namespaceManager.CreateQueue(qd);
            }
            catch (MessagingEntityAlreadyExistsException) { /* ignore */ }
        }
        var sbClient = QueueClient.CreateFromConnectionString(address, qd.Path);
#else
        if (!managementClient.QueueExistsAsync(queueName).GetAwaiter().GetResult())
        {
            try
            {
                managementClient.CreateQueueAsync(qd).GetAwaiter().GetResult();
            }
            catch (MessagingEntityAlreadyExistsException) { /* ignore */ }
        }
        var sbClient = new QueueClient(address, queueName);

#endif
        sbClient = sbClients.GetOrAdd(queueName, sbClient);
        return sbClient;
    }

    public IMessageHandlerStats GetStats()
    {
        lock (workers)
        {
            var total = new MessageHandlerStats("All Handlers");
            workers.ForEach(x => total.Add(x.GetStats()));
            return total;
        }
    }

    int status = 0;
    public string GetStatus() => WorkerStatus.ToString(status);
    
    public string GetStatsDescription()
    {
        lock (workers)
        {
            var sb = StringBuilderCache.Allocate()
                .Append("#MQ SERVER STATS:\n");
            sb.AppendLine("===============");
            sb.AppendLine("Current Status: " + GetStatus());
            sb.AppendLine("Listening On: " + string.Join(", ", workers.Select(x => x.QueueName).ToArray()));
            sb.AppendLine("Times Started: " + Interlocked.CompareExchange(ref timesStarted, 0, 0));
            sb.AppendLine("Num of Errors: " + Interlocked.CompareExchange(ref noOfErrors, 0, 0));
            sb.AppendLine("Num of Continuous Errors: " + Interlocked.CompareExchange(ref noOfContinuousErrors, 0, 0));
            sb.AppendLine("Last ErrorMsg: " + lastExMsg);
            sb.AppendLine("===============");

            foreach (var worker in workers)
            {
                sb.AppendLine(worker.MessageHandler.GetStats().ToString());
                sb.AppendLine("---------------\n");
            }

            return StringBuilderCache.ReturnAndFree(sb);
        }
    }
}