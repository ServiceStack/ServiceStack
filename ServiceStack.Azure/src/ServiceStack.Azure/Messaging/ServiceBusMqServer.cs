using ServiceStack.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;
#if NETCORE
using Microsoft.Azure.ServiceBus.Management;
#else
using Microsoft.ServiceBus;
#endif

namespace ServiceStack.Azure.Messaging;

public class ServiceBusMqServer : IMessageService
{
    private int retryCount = 1;
    public int RetryCount
    {
        get => retryCount;
        set
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(retryCount));
            retryCount = value;
        }
    }

    public ServiceBusMqServer(string connectionString)
    {
        messageFactory = new ServiceBusMqMessageFactory(this, connectionString);
    }

    private readonly ServiceBusMqMessageFactory? messageFactory;

    public IMessageFactory MessageFactory => messageFactory ?? throw new ArgumentNullException(nameof(messageFactory));

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
        
        
#if NETCORE
    /// <summary>
    /// Exposes the <see cref="Microsoft.Azure.ServiceBus.Management.ManagementClient"/> which can be used to perform
    /// management operations on ServiceBus entities.
    /// </summary>
    public ManagementClient ManagementClient => messageFactory!.managementClient;
#else
    /// <summary>
    /// Exposes the <see cref="Microsoft.ServiceBus.NamespaceManager"/> which can be used in managing entities,
    /// such as queues, topics, subscriptions, and rules, in your service namespace.
    /// </summary>
    public NamespaceManager NamespaceManager => messageFactory.namespaceManager;
#endif

    private readonly Dictionary<Type, IMessageHandlerFactory> handlerMap = new();

    protected internal Dictionary<Type, IMessageHandlerFactory> HandlerMap => handlerMap;

    private readonly Dictionary<Type, int> handlerThreadCountMap = new();

    public List<Type> RegisteredTypes => handlerMap.Keys.ToList();

    /// <summary>
    /// Opt-in to only publish responses on this white list. 
    /// Publishes all responses by default.
    /// </summary>
    public string[]? PublishResponsesWhitelist { get; set; }

    public bool DisablePublishingResponses
    {
        set => PublishResponsesWhitelist = value ? TypeConstants.EmptyStringArray : null;
    }
        
    /// <summary>
    /// Opt-in to only publish .outq messages on this white list. 
    /// Publishes all responses by default.
    /// </summary>
    public string[]? PublishToOutqWhitelist { get; set; }

    /// <summary>
    /// Don't publish any messages to .outq
    /// </summary>
    public bool DisablePublishingToOutq
    {
        set => PublishToOutqWhitelist = value ? TypeConstants.EmptyStringArray : null;
    }
        
    /// <summary>
    /// Disable publishing .outq Messages for Responses with no return type
    /// </summary>
    public bool DisableNotifyMessages { get; set; }

        
#if NETCORE
    public Action<Microsoft.Azure.ServiceBus.Message,IMessage> PublishMessageFilter 
    {
        get => messageFactory!.PublishMessageFilter;
        set => messageFactory!.PublishMessageFilter = value;
    }
#else
        public Action<Microsoft.ServiceBus.Messaging.BrokeredMessage,IMessage> PublishMessageFilter 
        {
            get => messageFactory.PublishMessageFilter;
            set => messageFactory.PublishMessageFilter = value;
        }
#endif
        
    public void Dispose()
    {
        (MessageFactory as ServiceBusMqMessageFactory)?.StopQueues();
    }

    public IMessageHandlerStats GetStats()
    {
        return ServiceBusMessageFactory.GetStats();
    }

    public string GetStatus()
    {
        return ServiceBusMessageFactory.GetStatus();
    }

    public string GetStatsDescription()
    {
        return ServiceBusMessageFactory.GetStatsDescription();
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

    public void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn, Action<IMessageHandler, IMessage<T>, Exception>? processExceptionEx, int noOfThreads)
    {
        if (handlerMap.ContainsKey(typeof(T)))
        {
            throw new ArgumentException("Message handler has already been registered for type: " + typeof(T).Name);
        }

        handlerMap[typeof(T)] = CreateMessageHandlerFactory(processMessageFn, processExceptionEx);
        handlerThreadCountMap[typeof(T)] = noOfThreads;

        LicenseUtils.AssertValidUsage(LicenseFeature.ServiceStack, QuotaType.Operations, handlerMap.Count);
    }

    protected IMessageHandlerFactory CreateMessageHandlerFactory<T>(Func<IMessage<T>, object> processMessageFn, Action<IMessageHandler, IMessage<T>, Exception>? processExceptionEx)
    {
        return new MessageHandlerFactory<T>(this, processMessageFn, processExceptionEx)
        {
            RequestFilter = RequestFilter,
            ResponseFilter = ResponseFilter,
            RetryCount = RetryCount,
            PublishResponsesWhitelist = PublishResponsesWhitelist,
            PublishToOutqWhitelist = PublishToOutqWhitelist,
        };
    }

    private ServiceBusMqMessageFactory ServiceBusMessageFactory => (ServiceBusMqMessageFactory)MessageFactory;

    public void Start()
    {
        // Create the queues (if they don't exist) and start the listeners
        ServiceBusMessageFactory.StartQueues(this.handlerMap, this.handlerThreadCountMap);
    }

    public void Stop()
    {
        ServiceBusMessageFactory.StopQueues();
    }
}