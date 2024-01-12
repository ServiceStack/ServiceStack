using ServiceStack.Messaging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

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

    public void Dispose()
    {
    }

    protected internal void StartQueues(Dictionary<Type, IMessageHandlerFactory> handlerMap, Dictionary<Type, int> handlerThreadCountMap)
    {
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
            AddQueueHandler(mqNames.In, handlerThreadCountMap[type]);
            AddQueueHandler(mqNames.Priority, handlerThreadCountMap[type]);
        }
    }

    private void AddQueueHandler(string queueName, int threadCount=1)
    {
        queueName = queueName.SafeQueueName()!;

#if NETCORE
        var sbClient = new QueueClient(address, queueName, ReceiveMode.PeekLock);
        var sbWorker = new ServiceBusMqWorker(this, CreateMessageQueueClient(), queueName, sbClient);
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

        var sbWorker = new ServiceBusMqWorker(this, CreateMessageQueueClient(), queueName, sbClient);
        sbClient.OnMessage(sbWorker.HandleMessage, options);
#endif
        sbClients.GetOrAdd(queueName, sbClient);
    }

    protected internal void StopQueues()
    {
#if NETCORE
        sbClients.Each(async kvp => await kvp.Value.CloseAsync());
#else
        sbClients.Each(kvp => kvp.Value.Close());
#endif
        sbClients.Clear();
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
}