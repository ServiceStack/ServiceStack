using System;
using Amazon.SQS.Model;
using ServiceStack.Messaging;

namespace ServiceStack.Aws.Sqs
{
    public interface ISqsMqMessageFactory : IMessageFactory
    {
        ISqsQueueManager QueueManager { get; }
        SqsConnectionFactory ConnectionFactory { get; }
        int RetryCount { get; set; }
        int BufferFlushIntervalSeconds { get; set; }
        Action<Exception> ErrorHandler { get; set; }
        
        Action<SendMessageRequest,IMessage> SendMessageRequestFilter { get; set; }
        Action<ReceiveMessageRequest> ReceiveMessageRequestFilter { get; set; }
        Action<Amazon.SQS.Model.Message, IMessage> ReceiveMessageResponseFilter { get; set; }
        Action<DeleteMessageRequest> DeleteMessageRequestFilter { get; set; }
        Action<ChangeMessageVisibilityRequest> ChangeMessageVisibilityRequestFilter { get; set; }
    }
}