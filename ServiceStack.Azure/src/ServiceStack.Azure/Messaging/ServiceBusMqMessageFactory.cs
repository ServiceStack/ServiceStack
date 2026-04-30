using ServiceStack.Logging;
using ServiceStack.Messaging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using ServiceStack.Text;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace ServiceStack.Azure.Messaging;

public class ServiceBusMqMessageFactory : IMessageFactory
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(ServiceBusMqMessageFactory));

    private long timesStarted;
    private long noOfErrors = 0;
    private int noOfContinuousErrors = 0;
    private readonly string? lastExMsg = null;

    public Action<ServiceBusMessage, IMessage> PublishMessageFilter { get; set; }

    protected internal readonly string address;
    internal readonly ServiceBusClient? sbClient;
    internal ServiceBusAdministrationClient? managementClient;
    private readonly ConcurrentDictionary<string, ServiceBusSender> senders = new();
    private readonly ConcurrentDictionary<string, ServiceBusReceiver> receivers = new();
    private readonly ConcurrentDictionary<string, ServiceBusProcessor> processors = new();
    internal readonly ConcurrentDictionary<string, Func<Task>> pendingAcks = new();

    internal Dictionary<Type, IMessageHandlerFactory> handlerMap;
    Dictionary<string, Type> queueMap;

    public ServiceBusMqServer MqServer { get; }

    public ServiceBusMqMessageFactory(ServiceBusMqServer mqServer, string address)
    {
        this.MqServer = mqServer;
        this.address = address;
        JsConfig.AllowRuntimeType = _ => true;
        this.sbClient = new ServiceBusClient(address);
        this.managementClient = new ServiceBusAdministrationClient(address);
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

                EnsureQueueExists(queueName);
            }

            var mqNames = new QueueNames(type);
            AddQueueHandler(type, mqNames.In, handlerThreadCountMap[type]);
            AddQueueHandler(type, mqNames.Priority, handlerThreadCountMap[type]);
        }
        this.status = WorkerStatus.Started;
    }

    private readonly List<ServiceBusMqWorker> workers = [];
    private void AddQueueHandler(Type queueType, string queueName, int threadCount = 1)
    {
        queueName = queueName.SafeQueueName()!;
        var processor = sbClient!.CreateProcessor(queueName, new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = threadCount,
            AutoCompleteMessages = false,
            ReceiveMode = ServiceBusReceiveMode.PeekLock
        });
        var messageHandler = this.GetMessageHandler(queueType);
        var sbWorker = new ServiceBusMqWorker(messageHandler, CreateMessageQueueClient(), queueName, this);
        lock (workers)
        {
            workers.Add(sbWorker);
        }
        processor.ProcessMessageAsync += sbWorker.HandleMessageAsync;
        processor.ProcessErrorAsync += args =>
        {
            if (args.Exception is ServiceBusException sbEx &&
                sbEx.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
            {
                // Queue was deleted while the processor was running (e.g. concurrent async cleanup).
                // Recreate and restart the processor in a background task so it can resume receiving.
                Log.Warn($"Queue {queueName} not found, recreating and restarting processor...");
                _ = Task.Run(async () =>
                {
                    try
                    {
                        EnsureQueueExists(queueName);
                        await Task.Delay(200);
                        await processor.StartProcessingAsync();
                    }
                    catch (Exception ex) when (ex.Message.Contains("already processing"))
                    {
                        // Processor already running – nothing to do
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Failed to restart processor for {queueName}: {ex.Message}");
                    }
                });
            }
            else
            {
                Log.Error($"ServiceBus processor error for {queueName}: {args.Exception}");
            }
            return Task.CompletedTask;
        };
        processor.StartProcessingAsync().GetAwaiter().GetResult();
        processors.GetOrAdd(queueName, processor);
    }

    protected internal void StopQueues()
    {
        this.status = WorkerStatus.Stopping;
        Task.WhenAll(processors.Values.Select(p => p.StopProcessingAsync())).GetAwaiter().GetResult();
        Task.WhenAll(processors.Values.Select(p => p.DisposeAsync().AsTask())).GetAwaiter().GetResult();
        Task.WhenAll(senders.Values.Select(s => s.DisposeAsync().AsTask())).GetAwaiter().GetResult();
        Task.WhenAll(receivers.Values.Select(r => r.DisposeAsync().AsTask())).GetAwaiter().GetResult();
        sbClient?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        processors.Clear();
        senders.Clear();
        receivers.Clear();
        pendingAcks.Clear();
        this.status = WorkerStatus.Stopped;
    }

    internal ServiceBusSender GetOrCreateSender(string queueName)
    {
        queueName = queueName.SafeQueueName()!;
        EnsureQueueExists(queueName);
        return senders.GetOrAdd(queueName, _ => sbClient!.CreateSender(queueName));
    }

    internal ServiceBusReceiver GetOrCreateReceiver(string queueName)
    {
        queueName = queueName.SafeQueueName()!;
        EnsureQueueExists(queueName);
        return receivers.GetOrAdd(queueName, _ => sbClient!.CreateReceiver(queueName,
            new ServiceBusReceiverOptions { ReceiveMode = ServiceBusReceiveMode.PeekLock }));
    }

    private void EnsureQueueExists(string queueName)
    {
        if (!managementClient!.QueueExistsAsync(queueName).GetAwaiter().GetResult())
        {
            try
            {
                managementClient.CreateQueueAsync(new CreateQueueOptions(queueName)).GetAwaiter().GetResult();
            }
            catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists) { }
        }
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
