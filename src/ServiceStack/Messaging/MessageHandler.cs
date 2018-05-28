using System;
using System.Linq;
using ServiceStack.Logging;

namespace ServiceStack.Messaging
{
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
            if (messageService == null)
                throw new ArgumentNullException(nameof(messageService));

            if (processMessageFn == null)
                throw new ArgumentNullException(nameof(processMessageFn));

            this.messageService = messageService;
            this.processMessageFn = processMessageFn;
            this.processInExceptionFn = processInExceptionFn ?? DefaultInExceptionHandler;
            this.retryCount = retryCount;
            this.ReplyClientFactory = ClientFactory.Create;
            this.ProcessQueueNames = new[] { QueueNames<T>.Priority, QueueNames<T>.In };
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
            var msgsProcessed = 0;
            try
            {
                IMessage<T> message;
                while ((message = mqClient.GetAsync<T>(queueName)) != null)
                {
                    ProcessMessage(mqClient, message);

                    msgsProcessed++;

                    if (doNext != null && !doNext()) 
                        return msgsProcessed;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error serializing message from mq server: " + ex.Message, ex);
            }

            return msgsProcessed;
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

            bool requeue = !(ex is UnRetryableMessagingException)
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
                        responseEx = new MessagingException(responseStatus, response);
                    }
                }
                
                if (responseEx != null)
                { 
                    TotalMessagesFailed++;

                    if (message.ReplyTo != null)
                    {
                        var replyClient = ReplyClientFactory(message.ReplyTo);
                        if (replyClient != null)
                        {
                            replyClient.SendOneWay(message.ReplyTo, response);
                        }
                        else
                        {
                            var responseDto = response.GetResponseDto();
                            mqClient.Publish(message.ReplyTo, MessageFactory.Create(responseDto));
                        }
                        return;
                    }

                    msgHandled = true;
                    processInExceptionFn(this, message, responseEx);
                    return;
                }

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
                        var messageOptions = (MessageOption) message.Options;
                        if (messageOptions.Has(MessageOption.NotifyOneWay))
                        {
                            mqClient.Notify(QueueNames<T>.Out, message);
                        }
                    }
                }
                
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
                                publishResponse => responseType.GetOperationName() == publishResponse);
                            if (!inWhitelist) return;
                        }

                        // Leave as-is to work around a Mono 2.6.7 compiler bug
                        if (!responseType.IsUserType()) return;
                        mqReplyTo = new QueueNames(responseType).In;
                    }

                    var replyClient = ReplyClientFactory(mqReplyTo);
                    if (replyClient != null)
                    {
                        try
                        {
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
                    if (responseMessage == null)
                        responseMessage = MessageFactory.Create(response);

                    responseMessage.ReplyId = message.Id;
                    mqClient.Publish(mqReplyTo, responseMessage);
                }
            }
            catch (Exception ex)
            {
                try
                {
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
            }
        }

        public void Dispose()
        {
            var shouldDispose = messageService as IMessageHandlerDisposer;
            shouldDispose?.DisposeMessageHandler(this);
        }

    }
}