using System;
using System.Linq;
using Amazon.SQS;
using Amazon.SQS.Model;
using ServiceStack.Aws.Support;

namespace ServiceStack.Aws.Sqs
{
    public class SqsMqBufferNonBuffered : ISqsMqBuffer
    {
        private readonly SqsQueueDefinition queueDefinition;
        private readonly SqsConnectionFactory sqsConnectionFactory;
        private IAmazonSQS sqsClient;

        public SqsMqBufferNonBuffered(SqsQueueDefinition queueDefinition,
                                      SqsConnectionFactory sqsConnectionFactory)
        {
            Guard.AgainstNullArgument(queueDefinition, "queueDefinition");
            Guard.AgainstNullArgument(sqsConnectionFactory, "sqsConnectionFactory");

            this.queueDefinition = queueDefinition;
            this.sqsConnectionFactory = sqsConnectionFactory;
        }

        private IAmazonSQS SqsClient => sqsClient ?? (sqsClient = sqsConnectionFactory.GetClient());

        public SqsQueueDefinition QueueDefinition => queueDefinition;

        public Action<Exception> ErrorHandler { get; set; }
        
        public bool Delete(DeleteMessageRequest request)
        {
            if (request == null)
            {
                return false;
            }

            var response = SqsClient.DeleteMessage(request);
            return response != null;
        }
        
        public bool ChangeVisibility(ChangeMessageVisibilityRequest request)
        {
            if (request == null)
                return false;

            var response = SqsClient.ChangeMessageVisibility(request);
            return response != null;
        }
        
        public bool Send(SendMessageRequest request)
        {
            if (request == null)
                return false;

            var response = SqsClient.SendMessage(request);
            return response != null;
        }
        
        public Message Receive(ReceiveMessageRequest request)
        {
            if (request == null)
                return null;

            request.MaxNumberOfMessages = 1;

            var response = SqsClient.ReceiveMessage(request);
            return response?.Messages.SingleOrDefault();
        }

        public int DeleteBufferCount => 0;

        public int SendBufferCount => 0;

        public int ChangeVisibilityBufferCount => 0;

        public int ReceiveBufferCount => 0;

        public void Drain(bool fullDrain, bool nakReceived = false)
        {
        }

        public void Dispose()
        {
            if (sqsClient == null)
            {
                return;
            }

            try
            {
                sqsClient.Dispose();
                sqsClient = null;
            }
            catch { }
        }
    }
}