using System;
using System.Collections.Generic;
using System.Linq;
using Amazon;
using Amazon.SQS.Model;
using ServiceStack.Aws.Support;
using ServiceStack.Messaging;

namespace ServiceStack.Aws.Sqs
{
    public class SqsMqServer : BaseMqServer<SqsMqWorker>
    {
        private readonly Dictionary<Type, SqsMqWorkerInfo> handlerMap = new Dictionary<Type, SqsMqWorkerInfo>();

        private readonly ISqsMqMessageFactory sqsMqMessageFactory;

        public TimeSpan PollingDuration { get; set; } = TimeSpan.FromMilliseconds(1000);

        public SqsMqServer() : this(new SqsConnectionFactory()) { }

        public SqsMqServer(string awsAccessKey, string awsSecretKey, RegionEndpoint region)
            : this(new SqsConnectionFactory(awsAccessKey, awsSecretKey, region)) { }

        public SqsMqServer(SqsConnectionFactory sqsConnectionFactory) : this(new SqsMqMessageFactory(new SqsQueueManager(sqsConnectionFactory))) { }

        public SqsMqServer(ISqsMqMessageFactory sqsMqMessageFactory)
        {
            Guard.AgainstNullArgument(sqsMqMessageFactory, "sqsMqMessageFactory");

            this.sqsMqMessageFactory = sqsMqMessageFactory;
        }

        public override IMessageFactory MessageFactory => sqsMqMessageFactory;

        public SqsConnectionFactory ConnectionFactory => sqsMqMessageFactory.ConnectionFactory;

        /// <summary>
        /// How many times a message should be retried before sending to the DLQ (Max of 1000).
        /// </summary>
        public int RetryCount
        {
            get => sqsMqMessageFactory.RetryCount;
            set => sqsMqMessageFactory.RetryCount = value;
        }

        /// <summary>
        /// How often, in seconds, any buffered SQS data is forced to be processed by the client. Only valid if buffering
        /// is enabled for a given model/server. By default, this is off entirely, which means if you are using buffering,
        /// data will only be processed to the server when operations occur that push a given queue/type over the
        /// configured size of the buffer.
        /// </summary>
        public int BufferFlushIntervalSeconds
        {
            get => sqsMqMessageFactory.BufferFlushIntervalSeconds;
            set => sqsMqMessageFactory.BufferFlushIntervalSeconds = value;
        }

        /// <summary>
        /// Default time (in seconds) each in-flight message remains locked/unavailable on the queue before being returned to visibility
        /// Default of 30 seconds
        /// See http://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/AboutVT.html
        /// </summary>
        public int VisibilityTimeout
        {
            get => sqsMqMessageFactory.QueueManager.DefaultVisibilityTimeout;
            set
            {
                Guard.AgainstArgumentOutOfRange(value < 0 || value > SqsQueueDefinition.MaxVisibilityTimeoutSeconds,
                    "SQS MQ VisibilityTimeout must be 0-43200");

                sqsMqMessageFactory.QueueManager.DefaultVisibilityTimeout = value;
            }
        }

        /// <summary>
        /// Defaut time (in seconds) each request to receive from the queue waits for a message to arrive
        /// Default is 0 seconds
        /// See http://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/sqs-long-polling.html
        /// </summary>
        public int ReceiveWaitTime
        {
            get => sqsMqMessageFactory.QueueManager.DefaultReceiveWaitTime;
            set
            {
                Guard.AgainstArgumentOutOfRange(value < 0 || value > SqsQueueDefinition.MaxWaitTimeSeconds,
                    "SQS MQ ReceiveWaitTime must be 0-20");

                sqsMqMessageFactory.QueueManager.DefaultReceiveWaitTime = value;
            }
        }

        /// <summary>
        /// Disables buffering of send/delete/change/receive calls to SQS (call per request when disabled)
        /// </summary>
        public bool DisableBuffering
        {
            get => sqsMqMessageFactory.QueueManager.DisableBuffering;
            set => sqsMqMessageFactory.QueueManager.DisableBuffering = value;
        }
        
        /// <summary>
        /// Set the AWS Account Identifier of the AWS Account that owns the SQS queue.
        /// </summary>
        public string AwsQueueOwnerAccountId
        {
            get => sqsMqMessageFactory.QueueManager.AwsQueueOwnerAccountId;
            set => sqsMqMessageFactory.QueueManager.AwsQueueOwnerAccountId = value;
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

        public Action<SendMessageRequest,IMessage> SendMessageRequestFilter
        {
            get => sqsMqMessageFactory.SendMessageRequestFilter;
            set => sqsMqMessageFactory.SendMessageRequestFilter = value;
        }

        public Action<ReceiveMessageRequest> ReceiveMessageRequestFilter
        {
            get => sqsMqMessageFactory.ReceiveMessageRequestFilter;
            set => sqsMqMessageFactory.ReceiveMessageRequestFilter = value;
        }
        
        public Action<Amazon.SQS.Model.Message, IMessage> ReceiveMessageResponseFilter
        {
            get => sqsMqMessageFactory.ReceiveMessageResponseFilter;
            set => sqsMqMessageFactory.ReceiveMessageResponseFilter = value;
        }
        
        public Action<DeleteMessageRequest> DeleteMessageRequestFilter
        {
            get => sqsMqMessageFactory.DeleteMessageRequestFilter;
            set => sqsMqMessageFactory.DeleteMessageRequestFilter = value;
        }
        
        public Action<ChangeMessageVisibilityRequest> ChangeMessageVisibilityRequestFilter
        {
            get => sqsMqMessageFactory.ChangeMessageVisibilityRequestFilter;
            set => sqsMqMessageFactory.ChangeMessageVisibilityRequestFilter = value;
        }

        protected override void DoDispose()
        {
            try
            {
                sqsMqMessageFactory.Dispose();
            }
            catch (Exception ex)
            {
                ErrorHandler?.Invoke(ex);
            }
        }

        public override void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn)
        {
            RegisterHandler(processMessageFn, null, noOfThreads: 1);
        }

        public override void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn, int noOfThreads)
        {
            RegisterHandler(processMessageFn, null, noOfThreads: noOfThreads);
        }

        public override void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn,
                                                Action<IMessageHandler, IMessage<T>, Exception> processExceptionEx)
        {
            RegisterHandler(processMessageFn, processExceptionEx, noOfThreads: 1);
        }

        public override void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn, Action<IMessageHandler, IMessage<T>, Exception> processExceptionEx, int noOfThreads)
        {
            RegisterHandler(processMessageFn, processExceptionEx, noOfThreads: noOfThreads, 
                null, null, null, null);
        }

        public void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn,
                                       Action<IMessageHandler, IMessage<T>, Exception> processExceptionEx,
                                       int noOfThreads, int? retryCount = null,
                                       int? visibilityTimeoutSeconds = null, int? receiveWaitTimeSeconds = null,
                                       bool? disableBuffering = null, string? awsQueueOwnerAccountId = null)
        {
            var type = typeof(T);

            Guard.Against<ArgumentException>(handlerMap.ContainsKey(type), string.Concat("SQS Message handler has already been registered for type: ", type.Name));

            var retry = RetryCount = retryCount ?? RetryCount;

            handlerMap[type] = new SqsMqWorkerInfo
            {
                MessageHandlerFactory = new MessageHandlerFactory<T>(this, processMessageFn, processExceptionEx)
                {
                    RequestFilter = this.RequestFilter,
                    ResponseFilter = this.ResponseFilter,
                    PublishResponsesWhitelist = this.PublishResponsesWhitelist,
                    PublishToOutqWhitelist = this.PublishToOutqWhitelist,
                    RetryCount = retry
                },
                MessageType = type,
                RetryCount = retry,
                ThreadCount = noOfThreads,
                VisibilityTimeout = visibilityTimeoutSeconds ?? this.VisibilityTimeout,
                ReceiveWaitTime = receiveWaitTimeSeconds ?? this.ReceiveWaitTime,
                DisableBuffering = disableBuffering ?? this.DisableBuffering,
                AwsQueueOwnerAccountId = awsQueueOwnerAccountId ?? this.AwsQueueOwnerAccountId
            };

            LicenseUtils.AssertValidUsage(LicenseFeature.ServiceStack, QuotaType.Operations, handlerMap.Count);
        }
        
        protected override void Init()
        {
            if (workers != null)
            {
                return;
            }

            sqsMqMessageFactory.ErrorHandler = this.ErrorHandler;

            workers = new List<SqsMqWorker>();

            foreach (var handler in handlerMap)
            {
                var msgType = handler.Key;
                var info = handler.Value;

                // First build the DLQ that will become the redrive policy for other q's for this type
                var dlqDefinition = sqsMqMessageFactory.QueueManager.CreateQueue(info.QueueNames.Dlq, info);

                // Base in q and workers
                sqsMqMessageFactory.QueueManager.CreateQueue(info.QueueNames.In, info, dlqDefinition.QueueArn);

                info.ThreadCount.Times(i => workers.Add(
                    new SqsMqWorker(sqsMqMessageFactory, info, info.QueueNames.In, WorkerErrorHandler) { PollingDuration = PollingDuration }));
                
                // Need an outq?
                if (PublishResponsesWhitelist == null || PublishResponsesWhitelist.Any(x => x == msgType.Name))
                {
                    sqsMqMessageFactory.QueueManager.CreateQueue(info.QueueNames.Out, info, dlqDefinition.QueueArn);
                }
                
                // Priority q and workers
                if (PriorityQueuesWhitelist == null || PriorityQueuesWhitelist.Any(x => x == msgType.Name))
                {   // Need priority queue and workers
                    sqsMqMessageFactory.QueueManager.CreateQueue(info.QueueNames.Priority, info, dlqDefinition.QueueArn);

                    info.ThreadCount.Times(i => workers.Add(
                        new SqsMqWorker(sqsMqMessageFactory, info, info.QueueNames.Priority, WorkerErrorHandler) { PollingDuration = PollingDuration }));
                }
            }
        }

    }
}