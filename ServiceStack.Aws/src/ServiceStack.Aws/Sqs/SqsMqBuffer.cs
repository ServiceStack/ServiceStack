using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Amazon.SQS;
using Amazon.SQS.Model;
using ServiceStack.Aws.Support;

namespace ServiceStack.Aws.Sqs
{
    public class SqsMqBuffer : ISqsMqBuffer
    {
        private readonly SqsQueueDefinition queueDefinition;
        private readonly SqsConnectionFactory sqsConnectionFactory;
        private IAmazonSQS sqsClient;

        private readonly ConcurrentQueue<Message> receiveBuffer = new ConcurrentQueue<Message>();
        private readonly ConcurrentQueue<DeleteMessageRequest> deleteBuffer = new ConcurrentQueue<DeleteMessageRequest>();
        private readonly ConcurrentQueue<SendMessageRequest> sendBuffer = new ConcurrentQueue<SendMessageRequest>();
        private readonly ConcurrentQueue<ChangeMessageVisibilityRequest> cvBuffer = new ConcurrentQueue<ChangeMessageVisibilityRequest>();

        public SqsMqBuffer(SqsQueueDefinition queueDefinition,
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

        private void HandleError(Exception ex)
        {
            if (ErrorHandler != null && ex != null)
            {
                ErrorHandler(ex);
            }
        }

        public bool ChangeVisibility(ChangeMessageVisibilityRequest request)
        {
            if (request == null)
            {
                return false;
            }

            cvBuffer.Enqueue(request);
            return CvEnqueued(queueDefinition.ChangeVisibilityBufferSize);
        }

        private bool CvEnqueued(int minBufferCount, bool forceOne = false)
        {
            var cvAtAws = false;
            minBufferCount = Math.Min(SqsQueueDefinition.MaxBatchCvItems, Math.Max(minBufferCount, 1));

            try
            {
                while (forceOne || cvBuffer.Count >= minBufferCount)
                {
                    forceOne = false;

                    var entries = EntriesToCv(SqsQueueDefinition.MaxBatchCvItems).ToList();

                    if (entries.Count <= 0)
                        break;

                    cvAtAws = true;

                    var response = SqsClient.ChangeMessageVisibilityBatch(new ChangeMessageVisibilityBatchRequest
                    {
                        QueueUrl = queueDefinition.QueueUrl,
                        Entries = entries
                    });

                    if (response.Failed != null && response.Failed.Count > 0)
                    {
                        response.Failed.Each(f => HandleError(f.ToException()));
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }

            return cvAtAws;
        }

        private IEnumerable<ChangeMessageVisibilityBatchRequestEntry> EntriesToCv(int count)
        {
            var result = new Dictionary<string, ChangeMessageVisibilityBatchRequestEntry>(count);

            while (result.Count < count)
            {
                if (!cvBuffer.TryDequeue(out var request))
                    return result.Values;

                var hashId = request.ReceiptHandle.ToSha256HashString64();

                if (!result.ContainsKey(hashId))
                {
                    result.Add(hashId, new ChangeMessageVisibilityBatchRequestEntry
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        ReceiptHandle = request.ReceiptHandle,
                        VisibilityTimeout = request.VisibilityTimeout
                    });
                }
            }

            return result.Values;
        }

        public bool Send(SendMessageRequest request)
        {
            if (request == null)
                return false;

            sendBuffer.Enqueue(request);
            return SendEnqueued(queueDefinition.SendBufferSize);
        }

        private bool SendEnqueued(int minBufferCount, bool forceOne = false)
        {
            var sentToSqs = false;
            minBufferCount = Math.Min(SqsQueueDefinition.MaxBatchSendItems, Math.Max(minBufferCount, 1));

            try
            {
                while (forceOne || sendBuffer.Count >= minBufferCount)
                {
                    forceOne = false;

                    var entries = EntriesToSend(SqsQueueDefinition.MaxBatchSendItems).ToList();

                    if (entries.Count <= 0)
                        break;

                    sentToSqs = true;

                    var response = SqsClient.SendMessageBatch(new SendMessageBatchRequest
                    {
                        QueueUrl = queueDefinition.QueueUrl,
                        Entries = entries
                    });

                    if (response.Failed != null && response.Failed.Count > 0)
                    {
                        response.Failed.Each(f => HandleError(f.ToException()));
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }

            return sentToSqs;
        }

        private IEnumerable<SendMessageBatchRequestEntry> EntriesToSend(int count)
        {
            var results = 0;

            while (results < count)
            {
                if (!sendBuffer.TryDequeue(out var request))
                    yield break;

                yield return new SendMessageBatchRequestEntry
                {
                    Id = Guid.NewGuid().ToString("N"),
                    MessageBody = request.MessageBody,
                    DelaySeconds = request.DelaySeconds,
                    MessageAttributes = request.MessageAttributes
                };

                results++;
            }
        }

        public bool Delete(DeleteMessageRequest request)
        {
            if (request == null)
                return false;

            deleteBuffer.Enqueue(request);
            return DeleteEnqueued(queueDefinition.DeleteBufferSize);
        }

        private bool DeleteEnqueued(int minBufferCount, bool forceOne = false)
        {
            var deletedAtSqs = false;
            minBufferCount = Math.Min(SqsQueueDefinition.MaxBatchDeleteItems, Math.Max(minBufferCount, 1));

            try
            {
                while (forceOne || deleteBuffer.Count >= minBufferCount)
                {
                    forceOne = false;

                    var entries = EntriesToDelete(SqsQueueDefinition.MaxBatchDeleteItems).ToList();

                    if (entries.Count <= 0)
                        break;

                    deletedAtSqs = true;

                    var response = SqsClient.DeleteMessageBatch(new DeleteMessageBatchRequest
                    {
                        QueueUrl = queueDefinition.QueueUrl,
                        Entries = entries
                    });

                    if (response.Failed != null && response.Failed.Count > 0)
                    {
                        response.Failed.Each(f => HandleError(f.ToException()));
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }

            return deletedAtSqs;
        }

        private IEnumerable<DeleteMessageBatchRequestEntry> EntriesToDelete(int count)
        {
            var result = new Dictionary<string, DeleteMessageBatchRequestEntry>(count);

            while (result.Count < count)
            {
                if (!deleteBuffer.TryDequeue(out var request))
                    return result.Values;

                var hashId = request.ReceiptHandle.ToSha256HashString64();

                if (!result.ContainsKey(hashId))
                {
                    result.Add(hashId, new DeleteMessageBatchRequestEntry
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        ReceiptHandle = request.ReceiptHandle
                    });
                }
            }

            return result.Values;
        }

        private Message BufferResponse(ReceiveMessageResponse response)
        {
            if (response?.Messages == null)
                return null;

            Message toReturn = null;

            foreach (var message in response.Messages.Where(m => m != null))
            {
                if (toReturn == null)
                {
                    toReturn = message;
                    continue;
                }

                receiveBuffer.Enqueue(message);
            }

            return toReturn;
        }

        public Message Receive(ReceiveMessageRequest request)
        {
            if (request == null)
                return null;

            if (receiveBuffer.Count > 0 && receiveBuffer.TryDequeue(out var toReturn))
                return toReturn;

            request.MaxNumberOfMessages = Math.Min(SqsQueueDefinition.MaxBatchReceiveItems, Math.Max(request.MaxNumberOfMessages, 1));

            var response = SqsClient.ReceiveMessage(request);

            return BufferResponse(response);
        }

        private void NakBufferedReceived()
        {
            Message toNak = null;

            var stopAt = DateTime.UtcNow.AddSeconds(30);

            while (receiveBuffer.Count > 0 && DateTime.UtcNow <= stopAt && receiveBuffer.TryDequeue(out toNak))
            {
                ChangeVisibility(new ChangeMessageVisibilityRequest
                {
                    QueueUrl = queueDefinition.QueueUrl,
                    ReceiptHandle = toNak.ReceiptHandle,
                    VisibilityTimeout = 0
                });
            }
        }

        public int DeleteBufferCount => deleteBuffer.Count;

        public int SendBufferCount => sendBuffer.Count;

        public int ChangeVisibilityBufferCount => cvBuffer.Count;

        public int ReceiveBufferCount => receiveBuffer.Count;

        public void Drain(bool fullDrain, bool nakReceived = false)
        {
            SendEnqueued(fullDrain ? 1 : SqsQueueDefinition.MaxBatchSendItems, forceOne: true);

            if (nakReceived)
            {
                NakBufferedReceived();
            }

            CvEnqueued(fullDrain ? 1 : SqsQueueDefinition.MaxBatchCvItems, forceOne: true);
            DeleteEnqueued(fullDrain ? 1 : SqsQueueDefinition.MaxBatchDeleteItems, forceOne: true);
        }

        public void Dispose()
        {   // Do our best to drain all the buffers
            Drain(fullDrain: true, nakReceived: true);

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