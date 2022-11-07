using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Amazon.SQS;
using Amazon.SQS.Model;
using ServiceStack.Aws.Support;
using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.Text;

namespace ServiceStack.Aws.Sqs
{
    public class SqsQueueManager : ISqsQueueManager
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SqsQueueManager));
        private readonly ConcurrentDictionary<string, SqsQueueDefinition> queueNameMap = new ConcurrentDictionary<string, SqsQueueDefinition>();

        private readonly SqsConnectionFactory sqsConnectionFactory;
        private IAmazonSQS sqsClient;

        public SqsQueueManager(SqsConnectionFactory sqsConnectionFactory)
        {
            Guard.AgainstNullArgument(sqsConnectionFactory, "sqsConnectionFactory");

            DefaultVisibilityTimeout = SqsQueueDefinition.DefaultVisibilityTimeoutSeconds;
            DefaultReceiveWaitTime = SqsQueueDefinition.DefaultWaitTimeSeconds;

            this.sqsConnectionFactory = sqsConnectionFactory;
        }

        public int DefaultVisibilityTimeout { get; set; }
        public int DefaultReceiveWaitTime { get; set; }
        public bool DisableBuffering { get; set; }
        public string AwsQueueOwnerAccountId { get; set; }

        public SqsConnectionFactory ConnectionFactory => sqsConnectionFactory;

        public ConcurrentDictionary<string, SqsQueueDefinition> QueueNameMap => queueNameMap;

        private SqsQueueName GetSqsQueueName(string queueName)
        {
            return queueNameMap.TryGetValue(queueName, out var qd)
                ? qd.SqsQueueName
                : SqsQueueNames.GetSqsQueueName(queueName);
        }

        public IAmazonSQS SqsClient => sqsClient ?? (sqsClient = sqsConnectionFactory.GetClient());

        public bool QueueExists(string queueName, bool forceRecheck = false)
        {
            return QueueExists(GetSqsQueueName(queueName), forceRecheck);
        }

        private bool QueueExists(SqsQueueName queueName, bool forceRecheck = false)
        {
            if (!forceRecheck && queueNameMap.ContainsKey(queueName.QueueName))
                return true;

            try
            {
                var definition = GetQueueDefinition(queueName, forceRecheck);
                return definition != null;
            }
            catch (QueueDoesNotExistException)
            {
                log.DebugFormat("SQS Queue named [{0}] does not exist", queueName);
                return false;
            }
            catch (AmazonSQSException sqsex)
            {
                if (!sqsex.Message.Contains("specified queue does not exist"))
                    throw;

                log.DebugFormat("SQS Queue named [{0}] does not exist", queueName);
                return false;
            }
        }

        public string GetQueueUrl(string queueName, bool forceRecheck = false)
        {
            return GetQueueUrl(GetSqsQueueName(queueName), forceRecheck);
        }

        private string GetQueueUrl(SqsQueueName queueName, bool forceRecheck = false)
        {
            if (!forceRecheck && queueNameMap.TryGetValue(queueName.QueueName, out var qd))
            {
                if (!string.IsNullOrEmpty(qd.QueueUrl))
                    return qd.QueueUrl;
            }

            if (AwsQueueOwnerAccountId.IsNullOrEmpty())
            {
                log.InfoFormat("Calling GetQueueUrl() for a Queue named [{0}]", queueName);
                var response = SqsClient.GetQueueUrl(new GetQueueUrlRequest { 
                    QueueName = queueName.AwsQueueName,
                });
                return response.QueueUrl;
            }
            else
            {
                log.InfoFormat("Calling GetQueueUrl() for a Queue named [{0}] on account [{1}]", queueName, AwsQueueOwnerAccountId);
                var response = SqsClient.GetQueueUrl(new GetQueueUrlRequest { 
                    QueueName = queueName.AwsQueueName,
                    QueueOwnerAWSAccountId = AwsQueueOwnerAccountId
                });
                return response.QueueUrl;
            }
        }

        public SqsQueueDefinition GetQueueDefinition(string queueName, bool forceRecheck = false)
        {
            return GetQueueDefinition(GetSqsQueueName(queueName), forceRecheck);
        }

        private SqsQueueDefinition GetQueueDefinition(SqsQueueName queueName, bool forceRecheck = false)
        {
            if (!forceRecheck && queueNameMap.TryGetValue(queueName.QueueName, out var qd))
                return qd;

            var queueUrl = GetQueueUrl(queueName);
            return GetQueueDefinition(queueName, queueUrl);
        }

        private SqsQueueDefinition GetQueueDefinition(string queueName, string queueUrl)
        {
            return GetQueueDefinition(GetSqsQueueName(queueName), queueUrl);
        }

        private SqsQueueDefinition GetQueueDefinition(SqsQueueName queueName, string queueUrl)
        {
            var response = SqsClient.GetQueueAttributes(new GetQueueAttributesRequest
            {
                QueueUrl = queueUrl,
                AttributeNames = new List<string> {
                    "All"
                }
            });

            var qd = response.Attributes.ToQueueDefinition(queueName, queueUrl, DisableBuffering);

            queueNameMap[queueName.QueueName] = qd;

            return qd;
        }

        public SqsQueueDefinition GetOrCreate(string queueName, int? visibilityTimeoutSeconds = null,
                                              int? receiveWaitTimeSeconds = null, bool? disasbleBuffering = null)
        {
            if (QueueExists(queueName) && queueNameMap.TryGetValue(queueName, out var qd))
                return qd;

            qd = CreateQueue(GetSqsQueueName(queueName), visibilityTimeoutSeconds,
                             receiveWaitTimeSeconds, disasbleBuffering);

            return qd;
        }

        public void DeleteQueue(string queueName)
        {
            try
            {
                var queueUrl = GetQueueUrl(queueName);
                DeleteQueue(queueName, queueUrl);
            }
            catch (QueueDoesNotExistException) { }
            catch (AmazonSQSException sqsex)
            {
                if (!sqsex.Message.Contains("specified queue does not exist"))
                {
                    throw;
                }
            }
        }

        private void DeleteQueue(string queueName, string queueUrl)
        {
            DeleteQueue(GetSqsQueueName(queueName), queueUrl);
        }

        private void DeleteQueue(SqsQueueName queueName, string queueUrl)
        {
            var request = new DeleteQueueRequest
            {
                QueueUrl = queueUrl
            };

            var response = SqsClient.DeleteQueue(request);

            queueNameMap.TryRemove(queueName.QueueName, out _);
        }

        public SqsQueueDefinition CreateQueue(string queueName, SqsMqWorkerInfo info, string redriveArn = null)
        {
            var redrivePolicy = string.IsNullOrEmpty(redriveArn)
                ? null
                : new SqsRedrivePolicy
                {
                    MaxReceiveCount = Math.Max(info.RetryCount, 1), //Valid Range 1-1000
                    DeadLetterTargetArn = redriveArn
                };

            return CreateQueue(GetSqsQueueName(queueName), info.VisibilityTimeout, info.ReceiveWaitTime,
                               info.DisableBuffering, redrivePolicy);
        }

        public SqsQueueDefinition CreateQueue(string queueName, int? visibilityTimeoutSeconds = null,
                                              int? receiveWaitTimeSeconds = null, bool? disasbleBuffering = null,
                                              SqsRedrivePolicy redrivePolicy = null)
        {
            return CreateQueue(GetSqsQueueName(queueName), visibilityTimeoutSeconds, receiveWaitTimeSeconds,
                               disasbleBuffering, redrivePolicy);
        }

        private SqsQueueDefinition CreateQueue(SqsQueueName queueName, int? visibilityTimeoutSeconds = null,
                                               int? receiveWaitTimeSeconds = null, bool? disasbleBuffering = null,
                                               SqsRedrivePolicy redrivePolicy = null)
        {
            SqsQueueDefinition queueDefinition = null;

            var request = new CreateQueueRequest
            {
                QueueName = queueName.AwsQueueName,
                Attributes = new Dictionary<string, string>
                {
                    {
                        QueueAttributeName.ReceiveMessageWaitTimeSeconds,
                        TimeSpan.FromSeconds(receiveWaitTimeSeconds ?? DefaultReceiveWaitTime)
                            .TotalSeconds
                            .ToString(CultureInfo.InvariantCulture)
                    },
                    {
                        QueueAttributeName.VisibilityTimeout,
                        TimeSpan.FromSeconds(visibilityTimeoutSeconds ?? DefaultVisibilityTimeout)
                            .TotalSeconds
                            .ToString(CultureInfo.InvariantCulture)
                    },
                    {
                        QueueAttributeName.MessageRetentionPeriod,
                        (QueueNames.IsTempQueue(queueName.QueueName)
                            ? SqsQueueDefinition.DefaultTempQueueRetentionSeconds
                            : SqsQueueDefinition.DefaultPermanentQueueRetentionSeconds).ToString(CultureInfo.InvariantCulture)
                    }
                }
            };

            if (redrivePolicy != null)
            {
                var json = redrivePolicy.ToJson();
                request.Attributes.Add(QueueAttributeName.RedrivePolicy, json);
            }

            try
            {
                var createResponse = SqsClient.CreateQueue(request);

                // Note - must go fetch the attributes from the server after creation, as the request attributes do not include
                // anything assigned by the server (i.e. the ARN, etc.).
                queueDefinition = GetQueueDefinition(queueName, createResponse.QueueUrl);

                queueDefinition.DisableBuffering = disasbleBuffering ?? DisableBuffering;

                queueNameMap[queueDefinition.QueueName] = queueDefinition;
            }
            catch (QueueNameExistsException)
            {   // Queue exists with different attributes, instead of creating, alter those attributes to match what was requested
                queueDefinition = UpdateQueue(queueName, request.ToSetAttributesRequest(null), disasbleBuffering);
            }

            return queueDefinition;
        }

        private SqsQueueDefinition UpdateQueue(SqsQueueName sqsQueueName, SetQueueAttributesRequest request,
                                               bool? disableBuffering = null)
        {
            if (string.IsNullOrEmpty(request.QueueUrl))
            {
                request.QueueUrl = GetQueueUrl(sqsQueueName);
            }

            var response = SqsClient.SetQueueAttributes(request);

            // Note - must go fetch the attributes from the server after creation, as the request attributes do not include
            // anything assigned by the server (i.e. the ARN, etc.).
            var queueDefinition = GetQueueDefinition(sqsQueueName, request.QueueUrl);

            queueDefinition.DisableBuffering = disableBuffering ?? DisableBuffering;

            queueNameMap[queueDefinition.QueueName] = queueDefinition;

            return queueDefinition;
        }

        public void PurgeQueue(string queueName)
        {
            try
            {
                PurgeQueueUrl(GetQueueUrl(queueName));
            }
            catch (QueueDoesNotExistException) { }
            catch (AmazonSQSException sqsex)
            {
                if (!sqsex.Message.Contains("specified queue does not exist"))
                    throw;
            }

        }

        public void PurgeQueues(IEnumerable<string> queueNames)
        {
            foreach (var queueName in queueNames)
            {
                try
                {
                    var url = GetQueueUrl(queueName);
                    PurgeQueueUrl(url);
                }
                catch (QueueDoesNotExistException) { }
                catch (AmazonSQSException sqsex)
                {
                    if (!sqsex.Message.Contains("specified queue does not exist"))
                        throw;
                }
            }
        }

        private void PurgeQueueUrl(string queueUrl)
        {
            try
            {
                SqsClient.PurgeQueue(new PurgeQueueRequest { QueueUrl = queueUrl });
            }
            catch (QueueDoesNotExistException) { }
            catch (PurgeQueueInProgressException) { }
            catch (AmazonSQSException sqsex)
            {
                if (!sqsex.Message.Contains("specified queue does not exist"))
                    throw;
            }
        }

        public int RemoveEmptyTemporaryQueues(long createdBefore)
        {
            var queuesRemoved = 0;

            var localTempQueueUrlMap = new Dictionary<string, QueueNameUrlMap>();

            // First, check any locally available
            queueNameMap.Where(kvp => QueueNames.IsTempQueue(kvp.Key))
                .Where(kvp => kvp.Value.CreatedTimestamp <= createdBefore)
                .Each(kvp => localTempQueueUrlMap.Add(kvp.Value.QueueUrl,
                    new QueueNameUrlMap {
                        QueueUrl = kvp.Value.QueueUrl,
                        QueueName = kvp.Value.QueueName
                    }));

            // Refresh the local info for each of the potentials, then if they are empty and expired, remove
            foreach (var qNameUrl in localTempQueueUrlMap.Values)
            {
                var qd = GetQueueDefinition(qNameUrl.QueueName, qNameUrl.QueueUrl);

                if (qd.CreatedTimestamp > createdBefore || qd.ApproximateNumberOfMessages > 0)
                    continue;

                DeleteQueue(qd.SqsQueueName, qd.QueueUrl);
                queuesRemoved++;
            }

            var queues = SqsClient.ListQueues(new ListQueuesRequest
            {
                QueueNamePrefix = QueueNames.TempMqPrefix.ToValidQueueName()
            });

            if (queues == null || queues.QueueUrls == null || queues.QueueUrls.Count <= 0)
                return queuesRemoved;

            foreach (var queueUrl in queues.QueueUrls)
            {
                // Already deleted above, or left purposely
                if (localTempQueueUrlMap.ContainsKey(queueUrl))
                    continue;

                var response = SqsClient.GetQueueAttributes(new GetQueueAttributesRequest
                {
                    QueueUrl = queueUrl,
                    AttributeNames = new List<string>
                    {
                        QueueAttributeName.CreatedTimestamp,
                        QueueAttributeName.ApproximateNumberOfMessages
                    }
                });

                if (response == null || response.CreatedTimestamp.ToUnixTime() > createdBefore ||
                    response.ApproximateNumberOfMessages > 0)
                {
                    continue;
                }

                SqsClient.DeleteQueue(new DeleteQueueRequest { QueueUrl = queueUrl });
                queuesRemoved++;
            }

            return queuesRemoved;
        }

        private class QueueNameUrlMap
        {
            public string QueueName { get; set; }
            public string QueueUrl { get; set; }
        }

        public void Dispose()
        {
            if (sqsClient == null)
                return;

            try
            {
                sqsClient.Dispose();
                sqsClient = null;
            }
            catch { }
        }
    }
}
