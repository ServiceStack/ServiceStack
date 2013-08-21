using System;
using System.Linq;
using ServiceStack.Common;
using ServiceStack.Logging;
using ServiceStack.Service;
using ServiceStack.Text;
using StringExtensions = ServiceStack.Common.StringExtensions;

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
        private readonly Action<IMessage<T>, Exception> processInExceptionFn;
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
            : this(messageService, processMessageFn, null, DefaultRetryCount) {}

        private IMessageQueueClient MqClient { get; set; }

        public MessageHandler(IMessageService messageService,
            Func<IMessage<T>, object> processMessageFn,
            Action<IMessage<T>, Exception> processInExceptionFn,
            int retryCount)
        {
            if (messageService == null)
                throw new ArgumentNullException("messageService");

            if (processMessageFn == null)
                throw new ArgumentNullException("processMessageFn");

            this.messageService = messageService;
            this.processMessageFn = processMessageFn;
            this.processInExceptionFn = processInExceptionFn ?? DefaultInExceptionHandler;
            this.retryCount = retryCount;
            this.ReplyClientFactory = ClientFactory.Create;
            this.ProcessQueueNames = new[] { QueueNames<T>.Priority, QueueNames<T>.In };
        }

        public Type MessageType
        {
            get { return typeof(T); }
        }

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
                byte[] messageBytes;
                while ((messageBytes = mqClient.GetAsync(queueName)) != null)
                {
                    var message = messageBytes.ToMessage<T>();
                    ProcessMessage(mqClient, message);

                    this.TotalNormalMessagesReceived++;
                    msgsProcessed++;
                    LastMessageProcessed = DateTime.UtcNow;

                    if (doNext != null && !doNext()) return msgsProcessed;
                }
            }
            catch (Exception ex)
            {
                var lastEx = ex;
                Log.Error("Error serializing message from mq server: " + lastEx.Message, ex);
            }

            return msgsProcessed;
        }

        public IMessageHandlerStats GetStats()
        {
            return new MessageHandlerStats(typeof(T).Name,
                TotalMessagesProcessed, TotalMessagesFailed, TotalRetries, 
                TotalNormalMessagesReceived, TotalPriorityMessagesReceived, LastMessageProcessed);
        }

        private void DefaultInExceptionHandler(IMessage<T> message, Exception ex)
        {
            Log.Error("Message exception handler threw an error", ex);

            if (!(ex is UnRetryableMessagingException))
            {
                if (message.RetryAttempts < retryCount)
                {
                    message.RetryAttempts++;
                    this.TotalRetries++;

                    message.Error = new MessagingException(ex.Message, ex).ToMessageError();
                    MqClient.Publish(QueueNames<T>.In, message.ToBytes());
                    return;
                }
            }

            MqClient.Publish(QueueNames<T>.Dlq, message.ToBytes());
        }

        public void ProcessMessage(IMessageQueueClient mqClient, Message<T> message)
        {
            this.MqClient = mqClient;

            try
            {
                var response = processMessageFn(message);
                var responseEx = response as Exception;
                if (responseEx != null)
                    throw responseEx;

                this.TotalMessagesProcessed++;

                //If there's no response publish the request message to its OutQ
                if (response == null)
                {
                    var messageOptions = (MessageOption)message.Options;
                    if (messageOptions.Has(MessageOption.NotifyOneWay))
                    {
                        mqClient.Notify(QueueNames<T>.Out, message.ToBytes());
                    }
                }
                else
                {
                    var responseType = response.GetType();

                    //If there's no explicit ReplyTo, send it to the typed Response InQ by default
                    var mqReplyTo = message.ReplyTo;
                    if (mqReplyTo == null)
                    {
                        //Disable default handling of MQ Responses if whitelist exists and Response not in whitelist
                        var publishAllResponses = PublishResponsesWhitelist == null;
                        if (!publishAllResponses)
                        {
                            var inWhitelist = PublishResponsesWhitelist.Any(publishResponse => responseType.Name == publishResponse);
                            if (!inWhitelist) return;
                        }

                        // Leave as-is to work around a Mono 2.6.7 compiler bug
                        if (!StringExtensions.IsUserType(responseType)) return;
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
                            Log.Error("Could not send response to '{0}' with client '{1}'"
                                .Fmt(mqReplyTo, replyClient.GetType().Name), ex);

                            // Leave as-is to work around a Mono 2.6.7 compiler bug
                            if (!StringExtensions.IsUserType(responseType)) return;

                            mqReplyTo = new QueueNames(responseType).In;
                        }
                    }

                    //Otherwise send to our trusty response Queue (inc if replyClient fails)
                    var responseMessage = MessageFactory.Create(response);
                    responseMessage.ReplyId = message.Id;
                    mqClient.Publish(mqReplyTo, responseMessage.ToBytes());
                }
            }
            catch (Exception ex)
            {
                try
                {
                    TotalMessagesFailed++;
                    processInExceptionFn(message, ex);
                }
                catch (Exception exHandlerEx)
                {
                    Log.Error("Message exception handler threw an error", exHandlerEx);
                }
            }
        }

        public void Dispose()
        {
            var shouldDispose = messageService as IMessageHandlerDisposer;
            if (shouldDispose != null)
                shouldDispose.DisposeMessageHandler(this);
        }

    }
}