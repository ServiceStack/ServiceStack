using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack.Logging;

namespace ServiceStack.Messaging;

/// <summary>
/// Processes all messages in a Normal and Priority Queue.
/// Expects to be called in 1 thread. i.e. Non Thread-Safe.
/// </summary>
/// <typeparam name="T"></typeparam>
public class MessageHandler<T>
    : IMessageHandler, IDisposable
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(MessageHandler<T>));

    public const int DefaultRetryCount = 2; //Will be a total of 3 attempts
    private readonly IMessageService messageService;
    private readonly Func<IMessage<T>, object> processMessageFn;
    private readonly Action<IMessageHandler, IMessage<T>, Exception> processInExceptionFn;
    public Func<string, IOneWayClient> ReplyClientFactory { get; set; }
    public string[] PublishResponsesWhitelist { get; set; }
    public string[] PublishToOutqWhitelist { get; set; }
        
    private readonly int retryCount;

    public int TotalMessagesProcessed { get; private set; }
    public int TotalMessagesFailed { get; private set; }
    public int TotalRetries { get; private set; }
    public int TotalNormalMessagesReceived { get; private set; }
    public int TotalPriorityMessagesReceived { get; private set; }
    public int TotalOutMessagesReceived { get; private set; }
    public DateTime? LastMessageProcessed { get; private set; }

    public string[] ProcessQueueNames { get; set; }

    public MessageHandler(IMessageService messageService,
        Func<IMessage<T>, object> processMessageFn)
        : this(messageService, processMessageFn, null, DefaultRetryCount) { }

    public IMessageQueueClient MqClient { get; private set; }

    public MessageHandler(IMessageService messageService,
        Func<IMessage<T>, object> processMessageFn,
        Action<IMessageHandler, IMessage<T>, Exception> processInExceptionFn,
        int retryCount)
    {
        this.messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        this.processMessageFn = processMessageFn ?? throw new ArgumentNullException(nameof(processMessageFn));
        this.processInExceptionFn = processInExceptionFn ?? DefaultInExceptionHandler;
        this.retryCount = retryCount;
        this.ReplyClientFactory = ClientFactory.Create;
        this.ProcessQueueNames = [QueueNames<T>.Priority, QueueNames<T>.In];
    }

    public Type MessageType => typeof(T);

    public void Process(IMessageQueueClient mqClient)
    {
        foreach (var processQueueName in ProcessQueueNames)
        {
            ProcessQueue(mqClient, processQueueName);
        }
    }

    public int ProcessQueue(IMessageQueueClient mqClient, string queueName, Func<bool> doNext = null)
    {
        var messagesProcessed = 0;
        try
        {
            while (mqClient.GetAsync<T>(queueName) is { } message)
            {
                ProcessMessage(mqClient, message);

                messagesProcessed++;

                if (doNext != null && !doNext()) 
                    return messagesProcessed;
            }
        }
        catch (TaskCanceledException) {}
        catch (Exception ex)
        {
            Log.Error("Error serializing message from mq server: " + ex.Message, ex);
        }

        return messagesProcessed;
    }

    public IMessageHandlerStats GetStats()
    {
        return new MessageHandlerStats(typeof(T).GetOperationName(),
            TotalMessagesProcessed, TotalMessagesFailed, TotalRetries,
            TotalNormalMessagesReceived, TotalPriorityMessagesReceived, LastMessageProcessed);
    }

    private void DefaultInExceptionHandler(IMessageHandler mqHandler, IMessage<T> message, Exception ex)
    {
        Log.Error("Message exception handler threw an error", ex);

        bool requeue = ex is not UnRetryableMessagingException
                       && message.RetryAttempts < retryCount;

        if (requeue)
        {
            message.RetryAttempts++;
            this.TotalRetries++;
        }

        message.Error = ex.ToResponseStatus();
        mqHandler.MqClient.Nak(message, requeue: requeue, exception: ex);
    }

    public void ProcessMessage(IMessageQueueClient mqClient, object mqResponse)
    {
        var message = mqClient.CreateMessage<T>(mqResponse);
        ProcessMessage(mqClient, message);
    }

    public void ProcessMessage(IMessageQueueClient mqClient, IMessage<T> message)
    {
        this.MqClient = mqClient;
        bool msgHandled = false;


        Activity origActivity = null;
        Activity activity = null;
        if (Diagnostics.ServiceStack.IsEnabled(Diagnostics.Events.ServiceStack.WriteMqRequestBefore))
        {
            origActivity = Activity.Current;
            if (origActivity == null)
            {
                var traceId = message.TraceId;
                if (traceId != null)
                {
                    activity = new Activity(Diagnostics.Activity.MqBegin);
                    activity.SetParentId(traceId);
                    if (message.Tag != null)
                        activity.AddTag(Diagnostics.Activity.Tag, message.Tag);

                    Diagnostics.ServiceStack.StartActivity(activity, new ServiceStackMqActivityArgs { Message = message, Activity = activity });
                }
            }
        }
            
        var id = Diagnostics.ServiceStack.WriteMqRequestBefore(message);
        try
        {
            var response = processMessageFn(message);
            var responseEx = response as Exception;

            if (responseEx == null)
            {
                var responseStatus = response.GetResponseStatus();
                var isError = responseStatus?.ErrorCode != null;
                if (isError)
                {
                    Log.ErrorFormat("Failed to process MQ message {0}: {1} {2}", typeof(T).Name, responseStatus.ErrorCode, responseStatus.Message);
                    responseEx = new MessagingException(responseStatus, response);
                }
            }
                
            if (responseEx != null)
            {
                Diagnostics.ServiceStack.WriteMqRequestError(id, message, responseEx);
                TotalMessagesFailed++;

                if (message.ReplyTo != null)
                {
                    var replyClient = ReplyClientFactory(message.ReplyTo);
                    if (replyClient != null)
                    {
                        Diagnostics.ServiceStack.WriteMqRequestPublish(id, replyClient, message.ReplyTo, response);
                        replyClient.SendOneWay(message.ReplyTo, response);
                    }
                    else
                    {
                        Diagnostics.ServiceStack.WriteMqRequestPublish(id, mqClient, message.ReplyTo, response);
                        var responseDto = response.GetResponseDto();
                        mqClient.Publish(message.ReplyTo, MessageFactory.Create(responseDto));
                    }
                    return;
                }

                msgHandled = true;
                processInExceptionFn(this, message, responseEx);
                return;
            }
            Diagnostics.ServiceStack.WriteMqRequestAfter(id, message);

            this.TotalMessagesProcessed++;

            //If there's no response publish the request message to its OutQ
            if (response == null)
            {
                if (message.ReplyTo != null)
                {
                    response = message.GetBody();
                }
                else
                {
                    var publishOutqResponses = PublishToOutqWhitelist == null;
                    if (!publishOutqResponses)
                    {
                        var inWhitelist = PublishToOutqWhitelist.Contains(QueueNames<T>.Out);
                        if (!inWhitelist) 
                            return;
                    }
                        
                    var messageOptions = (MessageOption) message.Options;
                    if (messageOptions.Has(MessageOption.NotifyOneWay))
                    {
                        Diagnostics.ServiceStack.WriteMqRequestPublish(id, mqClient, QueueNames<T>.Out, response);
                        mqClient.Notify(QueueNames<T>.Out, message);
                    }
                }
            }

            response = response.GetResponseDto();
            if (response != null)
            {
                var responseMessage = response as IMessage;
                var responseType = responseMessage != null 
                    ? (responseMessage.Body != null ? responseMessage.Body.GetType() : typeof(object))
                    : response.GetType();

                //If there's no explicit ReplyTo, send it to the typed Response InQ by default
                var mqReplyTo = message.ReplyTo;
                if (mqReplyTo == null)
                {
                    //Disable default handling of MQ Responses if whitelist exists and Response not in whitelist
                    var publishAllResponses = PublishResponsesWhitelist == null;
                    if (!publishAllResponses)
                    {
                        var inWhitelist = PublishResponsesWhitelist.Any(
                            publishResponse => 
                                responseType.GetOperationName() == publishResponse || 
                                responseType.Name == publishResponse);
                        if (!inWhitelist) 
                            return;
                    }

                    mqReplyTo = new QueueNames(responseType).In;
                }

                var replyClient = ReplyClientFactory(mqReplyTo);
                if (replyClient != null)
                {
                    try
                    {
                        Diagnostics.ServiceStack.WriteMqRequestPublish(id, replyClient, mqReplyTo, response);
                        replyClient.SendOneWay(mqReplyTo, response);
                        return;
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Could not send response to '{mqReplyTo}' with client '{replyClient.GetType().GetOperationName()}'", ex);

                        // Leave as-is to work around a Mono 2.6.7 compiler bug
                        if (!responseType.IsUserType()) return;

                        mqReplyTo = new QueueNames(responseType).In;
                    }
                }

                //Otherwise send to our trusty response Queue (inc if replyClient fails)
                responseMessage ??= MessageFactory.Create(response);

                responseMessage.ReplyId = message.Id;
                Diagnostics.ServiceStack.WriteMqRequestPublish(id, mqClient, responseMessage.ReplyId.ToString(), response);
                mqClient.Publish(mqReplyTo, responseMessage);
            }
        }
        catch (Exception ex)
        {
            try
            {
                if (ex is AggregateException)
                    ex = ex.UnwrapIfSingleException();
                    
                TotalMessagesFailed++;
                msgHandled = true;
                processInExceptionFn(this, message, ex);
            }
            catch (Exception exHandlerEx)
            {
                Log.Error("Message exception handler threw an error", exHandlerEx);
            }
        }
        finally
        {
            if (!msgHandled)
                mqClient.Ack(message);

            this.TotalNormalMessagesReceived++;
            LastMessageProcessed = DateTime.UtcNow;

            if (activity != null)
            {
                Diagnostics.ServiceStack.StopActivity(activity, new ServiceStackMqActivityArgs { Message = message, Activity = activity });
            }
        }
    }

    public void Dispose()
    {
        var shouldDispose = messageService as IMessageHandlerDisposer;
        shouldDispose?.DisposeMessageHandler(this);
    }

}