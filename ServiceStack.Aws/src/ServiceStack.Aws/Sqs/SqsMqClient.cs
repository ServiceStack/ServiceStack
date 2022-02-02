using System;
using System.Collections.Generic;
using Amazon.SQS.Model;
using ServiceStack.Messaging;
using Message = Amazon.SQS.Model.Message;

namespace ServiceStack.Aws.Sqs
{
    public class SqsMqClient : SqsMqMessageProducer, IMessageQueueClient
    {
        public Action<ReceiveMessageRequest> ReceiveMessageRequestFilter { get; set; }
        public Action<Message, IMessage> ReceiveMessageResponseFilter { get; set; }
        public Action<DeleteMessageRequest> DeleteMessageRequestFilter { get; set; }
        public Action<ChangeMessageVisibilityRequest> ChangeMessageVisibilityRequestFilter { get; set; }
        
        public SqsMqClient(ISqsMqBufferFactory sqsMqBufferFactory, ISqsQueueManager sqsQueueManager)
            : base(sqsMqBufferFactory, sqsQueueManager)
        { }

        public void Notify(string queueName, IMessage message)
        {   // Just a publish
            Publish(queueName, message);
        }

        public IMessage<T> Get<T>(string queueName, TimeSpan? timeOut = null)
        {
            return GetMessage<T>(queueName, timeOut.HasValue
                ? (int)Math.Round(timeOut.Value.TotalSeconds, MidpointRounding.AwayFromZero)
                : -1);
        }

        public IMessage<T> GetAsync<T>(string queueName)
        {
            return GetMessage<T>(queueName, waitSeconds: 0);
        }

        private static readonly List<string> AllMessageProperties = new List<string> { "All" };

        private IMessage<T> GetMessage<T>(string queueName, int waitSeconds)
        {
            var receiveWaitTime = waitSeconds < 0
                ? SqsQueueDefinition.MaxWaitTimeSeconds
                : SqsQueueDefinition.GetValidQueueWaitTime(waitSeconds);

            var queueDefinition = sqsQueueManager.GetOrCreate(queueName, receiveWaitTimeSeconds: receiveWaitTime);

            var timeoutAt = waitSeconds >= 0
                ? DateTime.UtcNow.AddSeconds(waitSeconds)
                : DateTime.MaxValue;

            var sqsBuffer = sqsMqBufferFactory.GetOrCreate(queueDefinition);

            do
            {
                var sqsMessage = sqsBuffer.Receive(ApplyFilter(new ReceiveMessageRequest
                {
                    MaxNumberOfMessages = queueDefinition.ReceiveBufferSize,
                    QueueUrl = queueDefinition.QueueUrl,
                    VisibilityTimeout = queueDefinition.VisibilityTimeout,
                    WaitTimeSeconds = receiveWaitTime,
                    MessageAttributeNames = AllMessageProperties
                }));

                var message = ApplyFilter(sqsMessage, sqsMessage.FromSqsMessage<T>(queueDefinition.QueueName));
                if (message != null)
                    return message;

            } while (DateTime.UtcNow <= timeoutAt);

            return null;
        }

        private ReceiveMessageRequest ApplyFilter(ReceiveMessageRequest msg)
        {
            ReceiveMessageRequestFilter?.Invoke(msg);
            return msg;
        }

        private Message<T> ApplyFilter<T>(Message sqsMessage, Message<T> mqMessage)
        {
            ReceiveMessageResponseFilter?.Invoke(sqsMessage, mqMessage);
            return mqMessage;
        }

        public void DeleteMessage(IMessage message)
        {
            if (string.IsNullOrEmpty(message?.Tag))
                return;

            var sqsTag = message.Tag.FromJson<SqsMessageTag>();

            var queueDefinition = sqsQueueManager.GetOrCreate(sqsTag.QName);

            var sqsBuffer = sqsMqBufferFactory.GetOrCreate(queueDefinition);

            sqsBuffer.Delete(ApplyFilter(new DeleteMessageRequest
            {
                QueueUrl = queueDefinition.QueueUrl,
                ReceiptHandle = sqsTag.RHandle
            }));
        }

        private DeleteMessageRequest ApplyFilter(DeleteMessageRequest msg)
        {
            DeleteMessageRequestFilter?.Invoke(msg);
            return msg;
        }

        public void Ack(IMessage message)
        {
            DeleteMessage(message);
        }

        public void ChangeVisibility(IMessage message, int visibilityTimeoutSeconds)
        {
            if (string.IsNullOrEmpty(message?.Tag))
                return;

            var sqsTag = message.Tag.FromJson<SqsMessageTag>();

            var queueDefinition = sqsQueueManager.GetOrCreate(sqsTag.QName);

            var sqsBuffer = sqsMqBufferFactory.GetOrCreate(queueDefinition);

            sqsBuffer.ChangeVisibility(ApplyFilter(new ChangeMessageVisibilityRequest
            {
                QueueUrl = queueDefinition.QueueUrl,
                ReceiptHandle = sqsTag.RHandle,
                VisibilityTimeout = visibilityTimeoutSeconds
            }));
        }

        private ChangeMessageVisibilityRequest ApplyFilter(ChangeMessageVisibilityRequest msg)
        {
            ChangeMessageVisibilityRequestFilter?.Invoke(msg);
            return msg;
        }

        public void Nak(IMessage message, bool requeue, Exception exception = null)
        {
            if (requeue)
            {   // NOTE: Cannot simply cv at SQS, as that simply puts the same message with the same state back on the q at
                // SQS, and we need the state on the message object coming in to this Nak to remain with it (i.e. retryCount, etc.)
                //ChangeVisibility(message, 0);

                DeleteMessage(message);
                Publish(message);
            }
            else
            {
                try
                {
                    Publish(message.ToDlqQueueName(), message);
                    DeleteMessage(message);
                }
                catch (Exception ex)
                {
                    log.Debug("Error trying to Nak message to Dlq", ex);
                    ChangeVisibility(message, 0);
                }
            }
        }

        public IMessage<T> CreateMessage<T>(object mqResponse)
        {
            return (IMessage<T>)mqResponse;
        }

        public string GetTempQueueName()
        {   // NOTE: Purposely not creating DLQ queues for all these temps if they get used, they'll get
            // created on the fly as needed if messages actually fail
            var queueDefinition = sqsQueueManager.GetOrCreate(QueueNames.GetTempQueueName());
            return queueDefinition.QueueName;
        }
    }
}