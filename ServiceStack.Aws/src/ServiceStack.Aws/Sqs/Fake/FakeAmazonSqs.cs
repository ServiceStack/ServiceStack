using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using ServiceStack;
using ServiceStack.Text;
using ServiceStack.Aws.Support;

namespace ServiceStack.Aws.Sqs.Fake
{
    public class FakeAmazonSqs : IAmazonSQS
    {
        private static readonly ConcurrentDictionary<string, FakeSqsQueue> queues = new ConcurrentDictionary<string, FakeSqsQueue>();

        static FakeAmazonSqs() { }
        private FakeAmazonSqs() { }

        private FakeSqsQueue GetQueue(string queueUrl)
        {
            if (!queues.TryGetValue(queueUrl, out var q))
            {
                throw new QueueDoesNotExistException($"Queue does not exist for url [{queueUrl}]");
            }

            return q;
        }

        public static FakeAmazonSqs Instance { get; } = new FakeAmazonSqs();

        public void Dispose()
        {
            // Nothing to do here, this object is basically a singleton that mimics an SQS client with in-memory data backing,
            // so no disposal required (actually shouldn't do so even if you want to, as each time you dispose you'll wind up
            // clearning the "server" data)
        }

        public AddPermissionResponse AddPermission(string queueUrl, string label, List<string> awsAccountIds, List<string> actions)
        {
            throw new NotImplementedException();
        }

        public AddPermissionResponse AddPermission(AddPermissionRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<AddPermissionResponse> AddPermissionAsync(string queueUrl, string label, List<string> awsAccountIds, List<string> actions,
            CancellationToken token = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<AddPermissionResponse> AddPermissionAsync(AddPermissionRequest request, CancellationToken token = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public ChangeMessageVisibilityResponse ChangeMessageVisibility(string queueUrl, string receiptHandle, int visibilityTimeout)
        {
            return ChangeMessageVisibility(new ChangeMessageVisibilityRequest
            {
                QueueUrl = queueUrl,
                ReceiptHandle = receiptHandle,
                VisibilityTimeout = visibilityTimeout
            });
        }

        public ChangeMessageVisibilityResponse ChangeMessageVisibility(ChangeMessageVisibilityRequest request)
        {
            var q = GetQueue(request.QueueUrl);

            var success = q.ChangeVisibility(request);

            return new ChangeMessageVisibilityResponse();
        }

        public Task<ChangeMessageVisibilityResponse> ChangeMessageVisibilityAsync(string queueUrl, string receiptHandle, int visibilityTimeout, CancellationToken token = default(CancellationToken))
        {
            return ChangeMessageVisibilityAsync(new ChangeMessageVisibilityRequest(queueUrl, receiptHandle, visibilityTimeout), token);
        }

        public Task<ChangeMessageVisibilityResponse> ChangeMessageVisibilityAsync(ChangeMessageVisibilityRequest request, CancellationToken token = default(CancellationToken))
        {
            return ChangeMessageVisibility(request).AsTaskResult();
        }

        public ChangeMessageVisibilityBatchResponse ChangeMessageVisibilityBatch(string queueUrl, List<ChangeMessageVisibilityBatchRequestEntry> entries)
        {
            return ChangeMessageVisibilityBatch(new ChangeMessageVisibilityBatchRequest
            {
                QueueUrl = queueUrl,
                Entries = entries
            });
        }

        public ChangeMessageVisibilityBatchResponse ChangeMessageVisibilityBatch(ChangeMessageVisibilityBatchRequest request)
        {
            if (request.Entries == null || request.Entries.Count <= 0)
            {
                throw new EmptyBatchRequestException("No entires in request");
            }
            if (request.Entries.Count > SqsQueueDefinition.MaxBatchCvItems)
            {
                throw new TooManyEntriesInBatchRequestException(
                    $"Count of [{request.Entries.Count}] exceeds limit of [{SqsQueueDefinition.MaxBatchCvItems}]");
            }

            var q = GetQueue(request.QueueUrl);

            var response = new ChangeMessageVisibilityBatchResponse
            {
                Failed = new List<BatchResultErrorEntry>(),
                Successful = new List<ChangeMessageVisibilityBatchResultEntry>()
            };

            var entryIds = new HashSet<string>();

            foreach (var entry in request.Entries)
            {
                if (entryIds.Contains(entry.Id))
                    throw new BatchEntryIdsNotDistinctException($"Duplicate Id of [{entry.Id}]");

                entryIds.Add(entry.Id);

                var success = false;
                BatchResultErrorEntry batchError = null;

                try
                {
                    success = q.ChangeVisibility(new ChangeMessageVisibilityRequest
                    {
                        QueueUrl = request.QueueUrl,
                        ReceiptHandle = entry.ReceiptHandle,
                        VisibilityTimeout = entry.VisibilityTimeout
                    });
                }
                catch (ReceiptHandleIsInvalidException rhex)
                {
                    batchError = new BatchResultErrorEntry
                    {
                        Id = entry.Id,
                        Message = rhex.Message,
                        Code = rhex.ErrorCode
                    };
                }
                catch (MessageNotInflightException mfex)
                {
                    batchError = new BatchResultErrorEntry
                    {
                        Id = entry.Id,
                        Message = mfex.Message,
                        Code = mfex.ErrorCode
                    };
                }

                if (success)
                {
                    response.Successful.Add(new ChangeMessageVisibilityBatchResultEntry
                    {
                        Id = entry.Id
                    });
                }
                else
                {
                    var entryToQueue = batchError ?? new BatchResultErrorEntry
                    {
                        Id = entry.Id,
                        Message = "FakeCvError",
                        Code = "123"
                    };

                    response.Failed.Add(entryToQueue);
                }
            }

            return response;
        }

        public Task<ChangeMessageVisibilityBatchResponse> ChangeMessageVisibilityBatchAsync(string queueUrl, List<ChangeMessageVisibilityBatchRequestEntry> entries, CancellationToken token = default(CancellationToken))
        {
            return ChangeMessageVisibilityBatchAsync(new ChangeMessageVisibilityBatchRequest(queueUrl, entries), token);
        }

        public Task<ChangeMessageVisibilityBatchResponse> ChangeMessageVisibilityBatchAsync(ChangeMessageVisibilityBatchRequest request, CancellationToken token = default(CancellationToken))
        {
            return ChangeMessageVisibilityBatch(request).AsTaskResult();
        }

        public CreateQueueResponse CreateQueue(string queueName)
        {
            return CreateQueue(new CreateQueueRequest
            {
                QueueName = queueName
            });
        }

        public CreateQueueResponse CreateQueue(CreateQueueRequest request)
        {
            if (request.QueueName.Length > 80 ||
                request.QueueName.Any(c => !char.IsLetterOrDigit(c) && !SqsQueueDefinition.ValidNonAlphaNumericChars.Contains(c)))
            {
                throw new AmazonSQSException("Can only include alphanumeric characters, hyphens, or underscores. 1 to 80 in length");
            }

            var qUrl = Guid.NewGuid().ToString("N");

            var qd = request.Attributes.ToQueueDefinition(SqsQueueNames.GetSqsQueueName(request.QueueName), qUrl, disableBuffering: true);

            var existingQueue = queues.SingleOrDefault(kvp => kvp.Value.QueueDefinition.QueueName.Equals(qd.QueueName, StringComparison.OrdinalIgnoreCase)).Value;

            if (existingQueue != null)
            {   // If an existing q with same name, attributes must match, which we only check 2 of currently in Fake
                if (existingQueue.QueueDefinition.VisibilityTimeout == qd.VisibilityTimeout &&
                    existingQueue.QueueDefinition.ReceiveWaitTime == qd.ReceiveWaitTime)
                {   // Same, so return the existing q
                    return new CreateQueueResponse
                    {
                        QueueUrl = existingQueue.QueueDefinition.QueueUrl
                    };
                }

                throw new QueueNameExistsException($"Queue with name [{qd.QueueName}] already exists");
            }

            qd.QueueArn = Guid.NewGuid().ToString("N");

            var q = new FakeSqsQueue
            {
                QueueDefinition = qd
            };

            if (!queues.TryAdd(qUrl, q))
                throw new Exception("This should not happen, somehow the QueueUrl already exists");

            return new CreateQueueResponse
            {
                QueueUrl = q.QueueDefinition.QueueUrl
            };
        }

        public Task<CreateQueueResponse> CreateQueueAsync(string queueName, CancellationToken token = default(CancellationToken))
        {
            return CreateQueueAsync(new CreateQueueRequest(queueName), token);
        }

        public Task<CreateQueueResponse> CreateQueueAsync(CreateQueueRequest request, CancellationToken token = default(CancellationToken))
        {
            return CreateQueue(request).AsTaskResult();
        }

        public DeleteMessageResponse DeleteMessage(string queueUrl, string receiptHandle)
        {
            return DeleteMessage(new DeleteMessageRequest
            {
                QueueUrl = queueUrl,
                ReceiptHandle = receiptHandle
            });
        }

        public DeleteMessageResponse DeleteMessage(DeleteMessageRequest request)
        {
            var q = GetQueue(request.QueueUrl);

            var removed = q.DeleteMessage(request);

            return new DeleteMessageResponse();
        }

        public Task<DeleteMessageResponse> DeleteMessageAsync(string queueUrl, string receiptHandle, CancellationToken token = default(CancellationToken))
        {
            return DeleteMessageAsync(new DeleteMessageRequest(queueUrl, receiptHandle), token);
        }

        public Task<DeleteMessageResponse> DeleteMessageAsync(DeleteMessageRequest request, CancellationToken token = default(CancellationToken))
        {
            return DeleteMessage(request).AsTaskResult();
        }

        public DeleteMessageBatchResponse DeleteMessageBatch(string queueUrl, List<DeleteMessageBatchRequestEntry> entries)
        {
            return DeleteMessageBatch(new DeleteMessageBatchRequest
            {
                QueueUrl = queueUrl,
                Entries = entries
            });
        }

        public DeleteMessageBatchResponse DeleteMessageBatch(DeleteMessageBatchRequest request)
        {
            if (request.Entries == null || request.Entries.Count <= 0)
                throw new EmptyBatchRequestException("No entires in request");

            if (request.Entries.Count > SqsQueueDefinition.MaxBatchDeleteItems)
                throw new TooManyEntriesInBatchRequestException(
                    $"Count of [{request.Entries.Count}] exceeds limit of [{SqsQueueDefinition.MaxBatchDeleteItems}]");

            var q = GetQueue(request.QueueUrl);

            var response = new DeleteMessageBatchResponse
            {
                Failed = new List<BatchResultErrorEntry>(),
                Successful = new List<DeleteMessageBatchResultEntry>()
            };

            var entryIds = new HashSet<string>();

            foreach (var entry in request.Entries)
            {
                var success = false;
                BatchResultErrorEntry batchError = null;

                try
                {
                    if (entryIds.Contains(entry.Id))
                        throw new BatchEntryIdsNotDistinctException($"Duplicate Id of [{entry.Id}]");

                    entryIds.Add(entry.Id);

                    success = q.DeleteMessage(new DeleteMessageRequest
                    {
                        QueueUrl = request.QueueUrl,
                        ReceiptHandle = entry.ReceiptHandle
                    });
                }
                catch (ReceiptHandleIsInvalidException rhex)
                {
                    batchError = new BatchResultErrorEntry
                    {
                        Id = entry.Id,
                        Message = rhex.Message,
                        Code = rhex.ErrorCode
                    };
                }
                catch (MessageNotInflightException mfex)
                {
                    batchError = new BatchResultErrorEntry
                    {
                        Id = entry.Id,
                        Message = mfex.Message,
                        Code = mfex.ErrorCode
                    };
                }

                if (success)
                {
                    response.Successful.Add(new DeleteMessageBatchResultEntry
                    {
                        Id = entry.Id
                    });
                }
                else
                {
                    var entryToQueue = batchError ?? new BatchResultErrorEntry
                    {
                        Id = entry.Id,
                        Message = "FakeDeleteError",
                        Code = "456"
                    };

                    response.Failed.Add(entryToQueue);
                }
            }

            return response;
        }

        public Task<DeleteMessageBatchResponse> DeleteMessageBatchAsync(string queueUrl, List<DeleteMessageBatchRequestEntry> entries, CancellationToken token = default(CancellationToken))
        {
            return DeleteMessageBatchAsync(new DeleteMessageBatchRequest(queueUrl, entries), token);
        }

        public Task<DeleteMessageBatchResponse> DeleteMessageBatchAsync(DeleteMessageBatchRequest request, CancellationToken token = default(CancellationToken))
        {
            return DeleteMessageBatch(request).AsTaskResult();
        }

        public DeleteQueueResponse DeleteQueue(string queueUrl)
        {
            return DeleteQueue(new DeleteQueueRequest
            {
                QueueUrl = queueUrl
            });
        }

        public DeleteQueueResponse DeleteQueue(DeleteQueueRequest request)
        {
            queues.TryRemove(request.QueueUrl, out _);

            return new DeleteQueueResponse();
        }

        public Task<DeleteQueueResponse> DeleteQueueAsync(string queueUrl, CancellationToken token = default(CancellationToken))
        {
            return DeleteQueueAsync(new DeleteQueueRequest(queueUrl), token);
        }

        public Task<DeleteQueueResponse> DeleteQueueAsync(DeleteQueueRequest request, CancellationToken token = default(CancellationToken))
        {
            return DeleteQueue(request).AsTaskResult();
        }

        public GetQueueAttributesResponse GetQueueAttributes(string queueUrl, List<string> attributeNames)
        {
            return GetQueueAttributes(new GetQueueAttributesRequest
            {
                QueueUrl = queueUrl,
                AttributeNames = attributeNames
            });
        }

        public GetQueueAttributesResponse GetQueueAttributes(GetQueueAttributesRequest request)
        {
            var q = GetQueue(request.QueueUrl);

            return new GetQueueAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    { QueueAttributeName.VisibilityTimeout, q.QueueDefinition.VisibilityTimeout.ToString() },
                    { QueueAttributeName.ReceiveMessageWaitTimeSeconds, q.QueueDefinition.ReceiveWaitTime.ToString() },
                    { QueueAttributeName.CreatedTimestamp, q.QueueDefinition.CreatedTimestamp.ToString() },
                    { QueueAttributeName.ApproximateNumberOfMessages, q.Count.ToString() },
                    { QueueAttributeName.QueueArn, q.QueueDefinition.QueueArn },
                    { QueueAttributeName.RedrivePolicy, q.QueueDefinition.RedrivePolicy?.ToJson() }
                }
            };
        }

        public Task<GetQueueAttributesResponse> GetQueueAttributesAsync(string queueUrl, List<string> attributeNames,
            CancellationToken token = default(CancellationToken))
        {
            return GetQueueAttributesAsync(new GetQueueAttributesRequest(queueUrl, attributeNames), token);
        }

        public Task<GetQueueAttributesResponse> GetQueueAttributesAsync(GetQueueAttributesRequest request, CancellationToken token = default(CancellationToken))
        {
            return GetQueueAttributes(request).AsTaskResult();
        }

        public GetQueueUrlResponse GetQueueUrl(string queueName)
        {
            return GetQueueUrl(new GetQueueUrlRequest
            {
                QueueName = queueName
            });
        }

        public GetQueueUrlResponse GetQueueUrl(GetQueueUrlRequest request)
        {
            var q = queues.Where(kvp => kvp.Value.QueueDefinition.QueueName.Equals(request.QueueName, StringComparison.OrdinalIgnoreCase))
                .Select(kvp => kvp.Value)
                .SingleOrDefault();

            if (q == null)
                throw new QueueDoesNotExistException($"Queue does not exist with namel [{request.QueueName}]");

            return new GetQueueUrlResponse
            {
                QueueUrl = q.QueueDefinition.QueueUrl
            };
        }

        public Task<GetQueueUrlResponse> GetQueueUrlAsync(string queueName, CancellationToken token = default(CancellationToken))
        {
            return GetQueueUrlAsync(new GetQueueUrlRequest(queueName), token);
        }

        public Task<GetQueueUrlResponse> GetQueueUrlAsync(GetQueueUrlRequest request, CancellationToken token = default(CancellationToken))
        {
            return GetQueueUrl(request).AsTaskResult();
        }

        public ListDeadLetterSourceQueuesResponse ListDeadLetterSourceQueues(ListDeadLetterSourceQueuesRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<ListDeadLetterSourceQueuesResponse> ListDeadLetterSourceQueuesAsync(ListDeadLetterSourceQueuesRequest request, CancellationToken token = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public ListQueuesResponse ListQueues(string queueNamePrefix)
        {
            return ListQueues(new ListQueuesRequest
            {
                QueueNamePrefix = queueNamePrefix
            });
        }

        public ListQueuesResponse ListQueues(ListQueuesRequest request)
        {
            var urls = string.IsNullOrEmpty(request.QueueNamePrefix)
                ? queues.Keys
                : queues.Values
                    .Where(q => q.QueueDefinition
                        .QueueName.StartsWith(request.QueueNamePrefix, StringComparison.OrdinalIgnoreCase))
                    .Select(q => q.QueueDefinition.QueueUrl);

            var response = new ListQueuesResponse
            {
                QueueUrls = urls.ToList()
            };

            return response;
        }

        public Task<ListQueuesResponse> ListQueuesAsync(string queueNamePrefix, CancellationToken token = default(CancellationToken))
        {
            return ListQueuesAsync(new ListQueuesRequest(queueNamePrefix), token);
        }

        public Task<ListQueuesResponse> ListQueuesAsync(ListQueuesRequest request, CancellationToken token = default(CancellationToken))
        {
            return ListQueues(request).AsTaskResult();
        }

        public ListQueueTagsResponse ListQueueTags(ListQueueTagsRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<ListQueueTagsResponse> ListQueueTagsAsync(ListQueueTagsRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public PurgeQueueResponse PurgeQueue(string queueUrl)
        {
            return PurgeQueue(new PurgeQueueRequest
            {
                QueueUrl = queueUrl
            });
        }

        public PurgeQueueResponse PurgeQueue(PurgeQueueRequest request)
        {
            var q = GetQueue(request.QueueUrl);
            q.Clear();
            return new PurgeQueueResponse();
        }

        public Task<PurgeQueueResponse> PurgeQueueAsync(string queueUrl, CancellationToken token = default(CancellationToken))
        {
            return PurgeQueueAsync(new PurgeQueueRequest { QueueUrl = queueUrl }, token);
        }

        public Task<PurgeQueueResponse> PurgeQueueAsync(PurgeQueueRequest request, CancellationToken token = default(CancellationToken))
        {
            return PurgeQueue(request).AsTaskResult();
        }

        public ReceiveMessageResponse ReceiveMessage(string queueUrl)
        {
            return ReceiveMessage(new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = 1,
                VisibilityTimeout = 30,
                WaitTimeSeconds = 0
            });
        }

        public ReceiveMessageResponse ReceiveMessage(ReceiveMessageRequest request)
        {
            if (request.MaxNumberOfMessages > SqsQueueDefinition.MaxBatchReceiveItems)
                throw new TooManyEntriesInBatchRequestException(
                    $"Count of [{request.MaxNumberOfMessages}] exceeds limit of [{SqsQueueDefinition.MaxBatchReceiveItems}]");

            var q = GetQueue(request.QueueUrl);

            var response = new ReceiveMessageResponse
            {
                Messages = q.Receive(request)
                .Select(qi => new Message {
                    Body = qi.Body,
                    MessageAttributes = qi.MessageAttributes,
                    Attributes = qi.Attributes,
                    MessageId = qi.MessageId,
                    ReceiptHandle = qi.ReceiptHandle
                })
                .ToList()
            };

            return response;
        }

        public Task<ReceiveMessageResponse> ReceiveMessageAsync(string queueUrl, CancellationToken token = default(CancellationToken))
        {
            return ReceiveMessageAsync(new ReceiveMessageRequest(queueUrl), token);
        }

        public Task<ReceiveMessageResponse> ReceiveMessageAsync(ReceiveMessageRequest request, CancellationToken token = default(CancellationToken))
        {
            return ReceiveMessage(request).AsTaskResult();
        }

        public RemovePermissionResponse RemovePermission(string queueUrl, string label)
        {
            throw new NotImplementedException();
        }

        public RemovePermissionResponse RemovePermission(RemovePermissionRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<RemovePermissionResponse> RemovePermissionAsync(string queueUrl, string label, CancellationToken token = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<RemovePermissionResponse> RemovePermissionAsync(RemovePermissionRequest request, CancellationToken token = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public SendMessageResponse SendMessage(string queueUrl, string messageBody)
        {
            return SendMessage(new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = messageBody,
                DelaySeconds = 0
            });
        }

        public SendMessageResponse SendMessage(SendMessageRequest request)
        {
            var q = GetQueue(request.QueueUrl);

            var id = q.Send(request);

            return new SendMessageResponse
            {
                MessageId = id
            };
        }

        public Task<SendMessageResponse> SendMessageAsync(string queueUrl, string messageBody, CancellationToken token = default(CancellationToken))
        {
            return SendMessageAsync(new SendMessageRequest(queueUrl, messageBody), token);
        }

        public Task<SendMessageResponse> SendMessageAsync(SendMessageRequest request, CancellationToken token = default(CancellationToken))
        {
            return SendMessage(request).AsTaskResult();
        }

        public SendMessageBatchResponse SendMessageBatch(string queueUrl, List<SendMessageBatchRequestEntry> entries)
        {
            return SendMessageBatch(new SendMessageBatchRequest
            {
                QueueUrl = queueUrl,
                Entries = entries
            });
        }

        public SendMessageBatchResponse SendMessageBatch(SendMessageBatchRequest request)
        {
            if (request.Entries == null || request.Entries.Count <= 0)
                throw new EmptyBatchRequestException("No entires in request");

            if (request.Entries.Count > SqsQueueDefinition.MaxBatchSendItems)
                throw new TooManyEntriesInBatchRequestException($"Count of [{request.Entries.Count}] exceeds limit of [{SqsQueueDefinition.MaxBatchSendItems}]");

            var q = GetQueue(request.QueueUrl);

            var response = new SendMessageBatchResponse
            {
                Failed = new List<BatchResultErrorEntry>(),
                Successful = new List<SendMessageBatchResultEntry>()
            };

            var entryIds = new HashSet<string>();

            foreach (var entry in request.Entries)
            {
                string id = null;
                BatchResultErrorEntry batchError = null;

                try
                {
                    if (entryIds.Contains(entry.Id))
                        throw new BatchEntryIdsNotDistinctException($"Duplicate Id of [{entry.Id}]");

                    entryIds.Add(entry.Id);

                    id = q.Send(new SendMessageRequest
                    {
                        QueueUrl = q.QueueDefinition.QueueUrl,
                        MessageAttributes = entry.MessageAttributes,
                        MessageBody = entry.MessageBody,
                    });
                }
                catch (ReceiptHandleIsInvalidException rhex)
                {
                    batchError = new BatchResultErrorEntry
                    {
                        Id = entry.Id,
                        Message = rhex.Message,
                        Code = rhex.ErrorCode
                    };
                }

                if (id == null)
                {
                    var entryToQueue = batchError ?? new BatchResultErrorEntry
                    {
                        Id = entry.Id,
                        Message = "FakeSendError",
                        Code = "789"
                    };

                    response.Failed.Add(entryToQueue);
                }
                else
                {
                    response.Successful.Add(new SendMessageBatchResultEntry
                    {
                        Id = entry.Id,
                        MessageId = id
                    });
                }
            }

            return response;
        }

        public Task<SendMessageBatchResponse> SendMessageBatchAsync(string queueUrl, List<SendMessageBatchRequestEntry> entries, CancellationToken token = default(CancellationToken))
        {
            return SendMessageBatchAsync(new SendMessageBatchRequest(queueUrl, entries), token);
        }

        public Task<SendMessageBatchResponse> SendMessageBatchAsync(SendMessageBatchRequest request, CancellationToken token = default(CancellationToken))
        {
            return SendMessageBatch(request).AsTaskResult();
        }

        public SetQueueAttributesResponse SetQueueAttributes(string queueUrl, Dictionary<string, string> attributes)
        {
            return SetQueueAttributes(new SetQueueAttributesRequest
            {
                QueueUrl = queueUrl,
                Attributes = attributes
            });
        }

        public SetQueueAttributesResponse SetQueueAttributes(SetQueueAttributesRequest request)
        {
            var q = GetQueue(request.QueueUrl);

            var qd = request.Attributes.ToQueueDefinition(SqsQueueNames.GetSqsQueueName(q.QueueDefinition.QueueName), q.QueueDefinition.QueueUrl, disableBuffering: true);

            if (qd.VisibilityTimeout > 0)
            {
                q.QueueDefinition.VisibilityTimeout = qd.VisibilityTimeout;
            }
            if (qd.ReceiveWaitTime > 0)
            {
                q.QueueDefinition.ReceiveWaitTime = qd.ReceiveWaitTime;
            }

            q.QueueDefinition.DisableBuffering = qd.DisableBuffering;
            q.QueueDefinition.RedrivePolicy = qd.RedrivePolicy;

            return new SetQueueAttributesResponse();
        }

        public Task<SetQueueAttributesResponse> SetQueueAttributesAsync(string queueUrl, Dictionary<string, string> attributes, CancellationToken token = default(CancellationToken))
        {
            return SetQueueAttributesAsync(new SetQueueAttributesRequest(queueUrl, attributes), token);
        }

        public Task<SetQueueAttributesResponse> SetQueueAttributesAsync(SetQueueAttributesRequest request, CancellationToken token = default(CancellationToken))
        {
            return SetQueueAttributes(request).AsTaskResult();
        }

        public TagQueueResponse TagQueue(TagQueueRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<TagQueueResponse> TagQueueAsync(TagQueueRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public UntagQueueResponse UntagQueue(UntagQueueRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<UntagQueueResponse> UntagQueueAsync(UntagQueueRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public ISQSPaginatorFactory Paginators { get; }

        public string AuthorizeS3ToSendMessage(string queueUrl, string bucket)
        {
            throw new NotImplementedException();
        }

        public Task<string> AuthorizeS3ToSendMessageAsync(string queueUrl, string bucket)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, string> GetAttributes(string queueUrl)
        {
            throw new NotImplementedException();
        }

        public Task<Dictionary<string, string>> GetAttributesAsync(string queueUrl)
        {
            throw new NotImplementedException();
        }

        public void SetAttributes(string queueUrl, Dictionary<string, string> attributes)
        {
            throw new NotImplementedException();
        }

        public Task SetAttributesAsync(string queueUrl, Dictionary<string, string> attributes)
        {
            throw new NotImplementedException();
        }

        public IClientConfig Config { get; }
    }
}