using System;
using System.Collections.Generic;
using Amazon.SQS.Model;
using NUnit.Framework;
using ServiceStack.Aws.Sqs;
using ServiceStack.Messaging;
using ServiceStack.Text;

namespace ServiceStack.Aws.Tests.Sqs
{
    [TestFixture]
    public class SqsQueueManagerTests
    {
        private SqsQueueManager sqsQueueManager;

        [OneTimeSetUp]
        public void FixtureSetup()
        {
            sqsQueueManager = new SqsQueueManager(SqsTestClientFactory.GetConnectionFactory());
        }

        [OneTimeTearDown]
        public void FixtureTeardown()
        {
            if (SqsTestAssert.IsFakeClient)
            {
                return;
            }
            
            // Cleanup anything left cached that we tested with
            var queueNamesToDelete = new List<string>(sqsQueueManager.QueueNameMap.Keys);

            foreach (var queueName in queueNamesToDelete)
            {
                try
                {
                    sqsQueueManager.DeleteQueue(queueName);
                }
                catch { }
            }
        }

        private string GetNewId()
        {
            return Guid.NewGuid().ToString("N");
        }

        [Test]
        public void Queue_exists_returns_false_for_non_existent_queue()
        {
            Assert.IsFalse(sqsQueueManager.QueueExists(GetNewId()));
        }

        [Test]
        public void Queue_exists_returns_false_for_non_existent_queue_that_is_already_cached_when_forced()
        {
            var qd = sqsQueueManager.CreateQueue(GetNewId());

            Assert.IsTrue(sqsQueueManager.QueueExists(qd.QueueName));

            // delete q directly at the client
            Assert.IsNotNull(sqsQueueManager.SqsClient.DeleteQueue(new DeleteQueueRequest(qd.QueueUrl)));

            // should still be in the cache
            Assert.IsTrue(sqsQueueManager.QueueNameMap.ContainsKey(qd.QueueName));

            // should still return true when not forced, false when forced
            Assert.IsTrue(sqsQueueManager.QueueExists(qd.QueueName));
            Assert.IsFalse(sqsQueueManager.QueueExists(qd.QueueName, forceRecheck: true));
        }

        [Test]
        public void Queue_exists_returns_true_for_existent_queue()
        {
            var qd = sqsQueueManager.CreateQueue(GetNewId());

            Assert.IsTrue(sqsQueueManager.QueueExists(qd.QueueName));
        }

        [Test]
        public void Queue_exists_returns_true_for_existent_queue_that_is_not_cached()
        {
            var qd = sqsQueueManager.CreateQueue(GetNewId());

            Assert.IsTrue(sqsQueueManager.QueueExists(qd.QueueName));

            // Remove from the cache in the manager
            Assert.IsTrue(sqsQueueManager.QueueNameMap.TryRemove(qd.QueueName, out var removedQd));
            Assert.IsTrue(ReferenceEquals(qd, removedQd));

            // Should still show exists without being forced
            Assert.IsTrue(sqsQueueManager.QueueExists(qd.QueueName));
        }

        [Test]
        public void Queue_url_returns_for_existent_queue()
        {
            var qd = sqsQueueManager.CreateQueue(GetNewId());

            Assert.That(sqsQueueManager.GetQueueUrl(qd.QueueName), Is.Not.Null.Or.Empty);
        }

        [Test]
        public void Queue_url_returns_for_existent_queue_that_is_not_cached()
        {
            var qd = sqsQueueManager.CreateQueue(GetNewId());

            var url = sqsQueueManager.GetQueueUrl(qd.QueueName);

            Assert.AreEqual(qd.QueueUrl, url, "QueueUrl not equal");

            // Remove from the cache in the manager
            Assert.IsTrue(sqsQueueManager.QueueNameMap.TryRemove(qd.QueueName, out var removedQd));
            Assert.IsTrue(ReferenceEquals(qd, removedQd));

            Assert.That(sqsQueueManager.GetQueueUrl(qd.QueueName), Is.Not.Null.Or.Empty);
        }

        [Test]
        public void Queue_url_does_not_return_for_non_existent_queue()
        {
            Assert.Throws<QueueDoesNotExistException>(() => sqsQueueManager.GetQueueUrl(GetNewId()));
        }

        [Test]
        public void Queue_url_does_not_return_for_non_existent_queue_that_is_already_cached_when_forced()
        {
            var qd = sqsQueueManager.CreateQueue(GetNewId());

            var url = sqsQueueManager.GetQueueUrl(qd.QueueName);

            Assert.AreEqual(qd.QueueUrl, url, "QueueUrl not equal");

            // delete q directly at the client
            Assert.IsNotNull(sqsQueueManager.SqsClient.DeleteQueue(new DeleteQueueRequest(qd.QueueUrl)));

            // should still be in the cache
            Assert.IsTrue(sqsQueueManager.QueueNameMap.ContainsKey(qd.QueueName));

            // should still return true when not forced, false when forced
            Assert.That(sqsQueueManager.GetQueueUrl(qd.QueueName), Is.Not.Null.Or.Empty);
            Assert.Throws<QueueDoesNotExistException>(() => sqsQueueManager.GetQueueUrl(qd.QueueName, forceRecheck: true));
        }

        [Test]
        public void Qd_returns_for_existent_queue()
        {
            var qd = sqsQueueManager.CreateQueue(GetNewId());

            var returnedQd = sqsQueueManager.GetQueueDefinition(qd.QueueName);

            Assert.IsTrue(ReferenceEquals(qd, returnedQd));
        }

        [Test]
        public void Qd_returns_for_existent_queue_that_is_not_cached()
        {
            var qd = sqsQueueManager.CreateQueue(GetNewId());

            var returnedQd = sqsQueueManager.GetQueueDefinition(qd.QueueName);

            Assert.IsTrue(ReferenceEquals(qd, returnedQd));

            // Remove from the cache in the manager
            Assert.IsTrue(sqsQueueManager.QueueNameMap.TryRemove(qd.QueueName, out var removedQd));
            Assert.IsTrue(ReferenceEquals(qd, removedQd));

            var newQd = sqsQueueManager.GetQueueDefinition(qd.QueueName);

            Assert.IsNotNull(newQd);
            Assert.AreEqual(qd.QueueUrl, newQd.QueueUrl, "QueueUrl");
            Assert.AreEqual(qd.QueueName, newQd.QueueName, "QueueName");
            Assert.AreEqual(qd.QueueArn, newQd.QueueArn, "QueueArn");
        }

        [Test]
        public void Qd_does_not_return_for_non_existent_queue()
        {
            Assert.Throws<QueueDoesNotExistException>(() => sqsQueueManager.GetQueueDefinition(GetNewId()));
        }

        [Test]
        public void Qd_does_not_return_for_non_existent_queue_that_is_already_cached_when_forced()
        {
            var qd = sqsQueueManager.CreateQueue(GetNewId());

            var returnedQd = sqsQueueManager.GetQueueDefinition(qd.QueueName);

            Assert.IsTrue(ReferenceEquals(qd, returnedQd));

            // delete q directly at the client
            Assert.IsNotNull(sqsQueueManager.SqsClient.DeleteQueue(new DeleteQueueRequest(qd.QueueUrl)));

            // should still be in the cache
            Assert.IsTrue(sqsQueueManager.QueueNameMap.ContainsKey(qd.QueueName));

            // should still return true when not forced, false when forced
            Assert.IsNotNull(sqsQueueManager.GetQueueDefinition(qd.QueueName));

            SqsTestAssert.Throws<QueueDoesNotExistException>(() => sqsQueueManager.GetQueueDefinition(qd.QueueName, forceRecheck: true), "specified queue does not exist");
        }
        
        [Test]
        public void GetOrCreate_creates_when_does_not_exist()
        {
            var name = GetNewId();
            var qd = sqsQueueManager.GetOrCreate(name);

            Assert.IsNotNull(qd);
            Assert.AreEqual(qd.QueueName, name);
        }

        [Test]
        public void GetOrCreate_gets_when_exists()
        {
            var name = GetNewId();
            var qd = sqsQueueManager.GetOrCreate(name);

            Assert.IsNotNull(qd);
            Assert.AreEqual(qd.QueueName, name);

            var getQd = sqsQueueManager.GetOrCreate(name);

            Assert.IsTrue(ReferenceEquals(qd, getQd));
        }

        [Test]
        public void Delete_fails_quietly_when_queue_does_not_exist()
        {
            Assert.DoesNotThrow(() => sqsQueueManager.DeleteQueue(GetNewId()));
        }

        [Test]
        public void Delete_succeeds_on_existing_queue()
        {
            var name = GetNewId();
            var qd = sqsQueueManager.CreateQueue(name);

            Assert.IsNotNull(qd);
            Assert.AreEqual(name, qd.QueueName);

            Assert.IsTrue(sqsQueueManager.QueueExists(qd.QueueName));

            Assert.DoesNotThrow(() => sqsQueueManager.DeleteQueue(qd.QueueName));
            Assert.IsFalse(sqsQueueManager.QueueExists(qd.QueueName));
        }

        [Test]
        public void Create_includes_correct_info_when_created_from_worker()
        {
            var info = new SqsMqWorkerInfo
            {
                VisibilityTimeout = 11,
                ReceiveWaitTime = 9,
                DisableBuffering = true,
                RetryCount = 6
            };

            var redriveQd = sqsQueueManager.CreateQueue(GetNewId(), info);

            var qd = sqsQueueManager.CreateQueue(GetNewId(), info, redriveArn: redriveQd.QueueArn);

            Assert.IsNotNull(qd, "Queue Definition");
            Assert.AreEqual(qd.VisibilityTimeout, info.VisibilityTimeout, "VisibilityTimeout");
            Assert.AreEqual(qd.ReceiveWaitTime, info.ReceiveWaitTime, "ReceiveWaitTime");
            Assert.AreEqual(qd.DisableBuffering, info.DisableBuffering, "DisableBuffering");

            Assert.IsNotNull(qd.RedrivePolicy, "RedrivePolicy");
            Assert.AreEqual(qd.RedrivePolicy.MaxReceiveCount, info.RetryCount, "RetryCount");
            Assert.AreEqual(qd.RedrivePolicy.DeadLetterTargetArn, redriveQd.QueueArn, "Redrive TargetArn");

        }

        [Test]
        public void Create_updates_existing_queue_when_created_with_different_attributes()
        {
            var info = new SqsMqWorkerInfo
            {
                VisibilityTimeout = 11,
                ReceiveWaitTime = 9,
                DisableBuffering = true,
                RetryCount = 6
            };

            var redriveQd = sqsQueueManager.CreateQueue(GetNewId(), info);

            var qd = sqsQueueManager.CreateQueue(GetNewId(), info, redriveArn: redriveQd.QueueArn);

            Assert.IsNotNull(qd, "First Queue Definition");
            Assert.IsTrue(sqsQueueManager.QueueExists(qd.QueueName), "First Queue");

            var newRedriveQd = sqsQueueManager.CreateQueue(GetNewId(), info);
            
            var newQd = sqsQueueManager.CreateQueue(qd.QueueName, 
                visibilityTimeoutSeconds: 12,
                receiveWaitTimeSeconds: 10, disasbleBuffering: false,
                redrivePolicy: new SqsRedrivePolicy {
                    DeadLetterTargetArn = newRedriveQd.QueueArn,
                    MaxReceiveCount = 7
                });

            Assert.IsNotNull(newQd, "New Queue Definition");
            Assert.AreEqual(newQd.VisibilityTimeout, 12, "VisibilityTimeout");
            Assert.AreEqual(newQd.ReceiveWaitTime, 10, "ReceiveWaitTime");
            Assert.AreEqual(newQd.DisableBuffering, false, "DisableBuffering");
            Assert.IsNotNull(newQd.RedrivePolicy, "RedrivePolicy");
            Assert.AreEqual(newQd.RedrivePolicy.MaxReceiveCount, 7, "RetryCount");
            Assert.AreEqual(newQd.RedrivePolicy.DeadLetterTargetArn, newRedriveQd.QueueArn, "Redrive TargetArn");

        }

        [Test]
        public void Purge_fails_quietly_when_queue_does_not_exist()
        {
            Assert.DoesNotThrow(() => sqsQueueManager.PurgeQueue(GetNewId()));
        }

        [Test]
        public void Can_remove_empty_temp_queues()
        {
            //Clean up
            sqsQueueManager.RemoveEmptyTemporaryQueues(DateTime.UtcNow.AddDays(5).ToUnixTime());

            var nonEmptyTempQueue = sqsQueueManager.CreateQueue(QueueNames.GetTempQueueName());

            sqsQueueManager.SqsClient.SendMessage(new SendMessageRequest(nonEmptyTempQueue.QueueUrl, "Just some text"));
            sqsQueueManager.SqsClient.SendMessage(new SendMessageRequest(nonEmptyTempQueue.QueueUrl, "Just some more text"));

            var emptyTempQueue1 = sqsQueueManager.CreateQueue(QueueNames.GetTempQueueName());
            var emptyTempQueue2 = sqsQueueManager.CreateQueue(QueueNames.GetTempQueueName());
            var emptyTempQueueNotCached = sqsQueueManager.CreateQueue(QueueNames.GetTempQueueName());

            if (!SqsTestAssert.IsFakeClient)
            {   // List queue doesn't return newly created queues for a bit, so if this a "real", we skip this part
                sqsQueueManager.QueueNameMap.TryRemove(emptyTempQueueNotCached.QueueName, out _);
            }

            var countOfQueuesRemoved = sqsQueueManager.RemoveEmptyTemporaryQueues(DateTime.UtcNow.AddDays(5).ToUnixTime());

            try
            {
                SqsTestAssert.FakeEqualRealGreater(3, 2, countOfQueuesRemoved);
            }
            finally
            {
                // Cleanup
                sqsQueueManager.DeleteQueue(nonEmptyTempQueue.QueueName);
                sqsQueueManager.DeleteQueue(emptyTempQueue1.QueueName);
                sqsQueueManager.DeleteQueue(emptyTempQueue2.QueueName);
                sqsQueueManager.DeleteQueue(emptyTempQueueNotCached.QueueName);
            }
        }

    }
}