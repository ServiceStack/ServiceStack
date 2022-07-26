using System;
using System.Collections.Generic;
using Amazon.SQS.Model;
using ServiceStack.Aws.Support;
using ServiceStack.Logging;
using ServiceStack.Messaging;

namespace ServiceStack.Aws.Sqs
{
    public class SqsMqMessageProducer : IMessageProducer, IOneWayClient
    {
        protected static readonly ILog log = LogManager.GetLogger(typeof(SqsMqMessageProducer));

        public Action<SendMessageRequest,IMessage> SendMessageRequestFilter { get; set; }

        protected readonly ISqsMqBufferFactory sqsMqBufferFactory;
        protected readonly ISqsQueueManager sqsQueueManager;

        public ISqsMqBufferFactory SqsMqBufferFactory => sqsMqBufferFactory;
        public ISqsQueueManager SqsQueueManager => sqsQueueManager;
        
        public SqsMqMessageProducer(ISqsMqBufferFactory sqsMqBufferFactory, ISqsQueueManager sqsQueueManager)
        {
            this.sqsMqBufferFactory = sqsMqBufferFactory;
            this.sqsQueueManager = sqsQueueManager;
        }

        public Action OnPublishedCallback { get; set; }

        public void Publish<T>(T messageBody)
        {
            if (messageBody is IMessage message)
            {
#if NET472 || NET6_0_OR_GREATER
                Diagnostics.ServiceStack.Init(message);
#endif
                Publish(message.ToInQueueName(), message);
            }
            else
            {
                Publish(new Message<T>(messageBody));
            }
        }

        public void Publish<T>(IMessage<T> message)
        {
            Publish(message.ToInQueueName(), message);
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
            if (requests == null)
                return;

            foreach (var request in requests)
            {
                SendOneWay(request);
            }
        }

        public void Publish(string queueName, IMessage message)
        {
            var queueDefinition = sqsQueueManager.GetOrCreate(queueName);
            var sqsBuffer = sqsMqBufferFactory.GetOrCreate(queueDefinition);

            sqsBuffer.Send(ApplyFilter(message.ToSqsSendMessageRequest(queueDefinition), message));
            OnPublishedCallback?.Invoke();
        }

        public SendMessageRequest ApplyFilter(SendMessageRequest sqsMessage, IMessage mqMessage)
        {
            SendMessageRequestFilter?.Invoke(sqsMessage, mqMessage);
            return sqsMessage;
        }

        public void Publish(string queueName, SendMessageRequest msgRequest)
        {
            var queueDefinition = sqsQueueManager.GetOrCreate(queueName);
            var sqsBuffer = sqsMqBufferFactory.GetOrCreate(queueDefinition);

            sqsBuffer.Send(msgRequest);
            OnPublishedCallback?.Invoke();
        }

        public void Dispose()
        {
            // NOTE: Do not dispose the bufferFactory or queueManager here, this object didn't create them, it was given them
        }
    }
}