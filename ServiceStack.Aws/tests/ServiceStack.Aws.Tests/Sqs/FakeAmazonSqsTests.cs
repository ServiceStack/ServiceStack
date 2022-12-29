using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Amazon.SQS;
using Amazon.SQS.Model;
using NUnit.Framework;
using ServiceStack.Aws.Sqs;
using ServiceStack.Aws.Sqs.Fake;

namespace ServiceStack.Aws.Tests.Sqs
{
    [TestFixture]
    public class FakeAmazonSqsTests
    {
        private IAmazonSQS client;
        private FakeSqsClientHelper helper;

        [OneTimeSetUp]
        public void FixtureSetup()
        {
            //_client = SqsTestClientFactory.GetClient();
            // NOTE: This purposely gets an instance of the Fake client directly vs. using the factory, as these
            // tests create a lot of queues that are never really used, so we don't want to actually run it against
            // a "real" SQS instance typically. If we want to, just use the line above instead of the line below.
            client = FakeAmazonSqs.Instance;
            helper = new FakeSqsClientHelper(client);
        }
        
        [Test]
        public void Create_fails_with_non_alphanumeric_chars()
        {
            var invalidNames = new List<string>
            {
                "testing123:testing456",
                "testing123.testing456",
                "testing123=testing456",
                "testing123+testing456"
            };

            foreach (var invalidName in invalidNames)
            {
                Assert.Throws<AmazonSQSException>(() => 
                    client.CreateQueue(new CreateQueueRequest(invalidName)), invalidName);
            }
        }

        [Test]
        public void Create_succeeds_with_valid_non_alphanumeric_chars()
        {
            var validNames = new List<string>
            {
                "testing123-testing456",
                "testing123_testing456",
                "-_",
            };

            foreach (var validName in validNames)
            {
                var createResponse = client.CreateQueue(new CreateQueueRequest(validName));
                Assert.That(createResponse.QueueUrl, Is.Not.Null.Or.Empty);
            }
        }

        [Test]
        public void Can_create_and_get_attributes_and_url_correctly()
        {
            var createRequest = new CreateQueueRequest
            {
                QueueName = Guid.NewGuid().ToString("N"),
                Attributes = new Dictionary<string, string>
                {
                    { QueueAttributeName.VisibilityTimeout, "23" },
                    { QueueAttributeName.ReceiveMessageWaitTimeSeconds, "13" },
                }
            };
            
            var createResponse = client.CreateQueue(createRequest);

            Assert.That(createResponse.QueueUrl, Is.Not.Null.Or.Empty);

            var attrResponse = client.GetQueueAttributes(new GetQueueAttributesRequest(
                createResponse.QueueUrl, new List<string> { "All" }));
                
            Assert.AreEqual(attrResponse.Attributes[QueueAttributeName.VisibilityTimeout],
                            createRequest.Attributes[QueueAttributeName.VisibilityTimeout]);
            Assert.AreEqual(attrResponse.Attributes[QueueAttributeName.ReceiveMessageWaitTimeSeconds],
                            createRequest.Attributes[QueueAttributeName.ReceiveMessageWaitTimeSeconds]);

            var qUrlResponse = client.GetQueueUrl(new GetQueueUrlRequest(createRequest.QueueName));

            Assert.AreEqual(qUrlResponse.QueueUrl, createResponse.QueueUrl);
        }

        [Test]
        public void Creating_duplicate_queue_names_with_same_attribute_definition_does_not_throw_exception()
        {
            var name = Guid.NewGuid().ToString("N");

            var firstUrl = helper.CreateQueue(name);

            var secondUrl = helper.CreateQueue(name);

            Assert.AreEqual(firstUrl, secondUrl);
        }

        [Test]
        public void Creating_duplicate_queue_names_with_different_vis_timeout_throws_exception()
        {
            var name = Guid.NewGuid().ToString("N");

            var url = helper.CreateQueue(name);
            
            Assert.Throws<QueueNameExistsException>(() => helper.CreateQueue(name, visTimeout: 23));
        }

        [Test]
        public void Creating_duplicate_queue_names_with_different_waittime_throws_exception()
        {
            var name = Guid.NewGuid().ToString("N");

            var url = helper.CreateQueue(name);

            Assert.Throws<QueueNameExistsException>(() => helper.CreateQueue(name, waitTime: 13));
        }

        [Test]
        public void Can_send_single_message()
        {
            Assert.AreEqual(1, helper.SendMessages());
        }

        [Test]
        public void Can_send_batch_of_messages()
        {
            Assert.AreEqual(9, helper.SendMessages(count: 9));
        }

        [Test]
        public void Sending_too_many_messages_throws_exception()
        {
            Assert.Throws<TooManyEntriesInBatchRequestException>(() => helper.SendMessages(count: (SqsQueueDefinition.MaxBatchSendItems + 1)));
        }

        [Test]
        public void Sending_no_entries_throws_exception()
        {
            var newQueueUrl = helper.CreateQueue();

            Assert.Throws<EmptyBatchRequestException>(() => client.SendMessageBatch(new SendMessageBatchRequest
            {
                QueueUrl = newQueueUrl,
                Entries = new List<SendMessageBatchRequestEntry>()
            }));
        }

        [Test]
        public void Sending_to_non_existent_q_throws_exception()
        {
            var queueUrl = $"http://{Guid.NewGuid():N}.com";
            SqsTestAssert.Throws<QueueDoesNotExistException>(() => helper.SendMessages(queueUrl), "specified queue does not exist");
        }

        [Test]
        public void Sending_duplicate_entries_throws_exception()
        {
            var id = Guid.NewGuid().ToString("N");

            Assert.Throws<BatchEntryIdsNotDistinctException>(() => client.SendMessageBatch(new SendMessageBatchRequest
            {
                QueueUrl = helper.DefaultQueueUrl,
                Entries = new List<SendMessageBatchRequestEntry>
                {
                    new SendMessageBatchRequestEntry
                    {
                        Id = id,
                        MessageBody = id
                    },
                    new SendMessageBatchRequestEntry
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        MessageBody = Guid.NewGuid().ToString("N")
                    },
                    new SendMessageBatchRequestEntry
                    {
                        Id = id,
                        MessageBody = id
                    }
                }
            }));
        }

        [Test]
        public void Changing_visibility_on_valid_item_succeeds()
        {
            helper.SendMessages(count: 4);

            var message = helper.ReceiveSingle();

            var response = client.ChangeMessageVisibility(new ChangeMessageVisibilityRequest(
                helper.DefaultQueueUrl, message.ReceiptHandle, 28));

            Assert.IsNotNull(response);
        }
        
        [Test]
        public void Changing_visibility_to_zero_on_valid_item_requeues_it()
        {
            var newQueueUrl = helper.CreateQueue();

            // New q should be empty
            var response = client.ReceiveMessage(new ReceiveMessageRequest
            {
                QueueUrl = newQueueUrl,
                WaitTimeSeconds = 0
            });
            Assert.AreEqual(0, response.Messages.Count);

            // Send 1, pull it off (should only get 1)
            helper.SendMessages(newQueueUrl);

            var message = helper.ReceiveSingle(newQueueUrl);

            // Q should be empty again
            response = client.ReceiveMessage(new ReceiveMessageRequest(newQueueUrl));
            Assert.AreEqual(0, response.Messages.Count);

            // CV on message we have
            client.ChangeMessageVisibility(new ChangeMessageVisibilityRequest(
                newQueueUrl, message.ReceiptHandle, 0));

            // Should be a single item (same one) back on q again
            var messageRepeat = helper.ReceiveSingle(newQueueUrl);
            Assert.AreEqual(message.MessageId, messageRepeat.MessageId);
        }

        [Test]
        public void Changing_visibility_on_non_existent_item_throws_exception()
        {
            helper.SendMessages(count: 4);

            Assert.Throws<ReceiptHandleIsInvalidException>(
                () => client.ChangeMessageVisibility(new ChangeMessageVisibilityRequest(
                    helper.DefaultQueueUrl, Guid.NewGuid().ToString("N"), 11)));
        }
        
        [Test]
        public void Received_message_no_ack_gets_requeued()
        {
            var newQueueUrl = helper.CreateQueue();

            // New q should be empty
            var response = client.ReceiveMessage(new ReceiveMessageRequest
            {
                QueueUrl = newQueueUrl,
                WaitTimeSeconds = 0
            });
            Assert.AreEqual(0, response.Messages.Count);

            // Send 1, pull it off (should only get 1)
            helper.SendMessages(newQueueUrl);
            var message = helper.ReceiveSingle(newQueueUrl, visTimeout: 1);

            // Q should be empty again
            response = client.ReceiveMessage(new ReceiveMessageRequest
            {
                QueueUrl = newQueueUrl,
                WaitTimeSeconds = 0
            });
            Assert.AreEqual(0, response.Messages.Count);

            Thread.Sleep(1000);
            
            // Should be a single item (same one) back on q again
            var messageRepeat = helper.ReceiveSingle(newQueueUrl);
            Assert.AreEqual(message.MessageId, messageRepeat.MessageId);
        }

        [Test]
        public void Deleting_non_existent_item_throws_exception()
        {
            Assert.Throws<ReceiptHandleIsInvalidException>(() => 
                client.DeleteMessage(new DeleteMessageRequest(
                    helper.DefaultQueueUrl, Guid.NewGuid().ToString("N"))));
        }

        [Test]
        public void Deleting_valid_item_succeeds()
        {
            helper.SendMessages(count: 2);

            var message = helper.ReceiveSingle();

            var success = client.DeleteMessage(new DeleteMessageRequest(
                helper.DefaultQueueUrl, message.ReceiptHandle));

            Assert.IsNotNull(success);
        }

        [Test]
        public void Can_delete_batch_of_messages()
        {
            var newQueueUrl = helper.CreateQueue();

            helper.SendMessages(newQueueUrl, count: 6);

            var received = client.ReceiveMessage(new ReceiveMessageRequest
            {
                QueueUrl = newQueueUrl,
                MaxNumberOfMessages = 5,
                VisibilityTimeout = 30,
                WaitTimeSeconds = 0
            });

            SqsTestAssert.FakeEqualRealGreater(5, 1, received.Messages.Count);

            var response = client.DeleteMessageBatch(new DeleteMessageBatchRequest(
                newQueueUrl,
                received.Messages.Select(m => new DeleteMessageBatchRequestEntry {
                        Id = m.MessageId,
                        ReceiptHandle = m.ReceiptHandle
                    }).ToList()
                ));

            Assert.AreEqual(received.Messages.Count, response.Successful.Count);

            received = client.ReceiveMessage(new ReceiveMessageRequest
            {
                QueueUrl = newQueueUrl,
                MaxNumberOfMessages = 5,
                VisibilityTimeout = 30,
                WaitTimeSeconds = 0
            });

            SqsTestAssert.FakeEqualRealGreater(1, 0, received.Messages.Count);
        }

        [Test]
        public void Deleting_too_many_messages_throws_exception()
        {
            var entries = (SqsQueueDefinition.MaxBatchDeleteItems + 1).Times(() => 
                new DeleteMessageBatchRequestEntry
                {
                    Id = Guid.NewGuid().ToString("N"),
                    ReceiptHandle = Guid.NewGuid().ToString("N")
                });

            Assert.Throws<TooManyEntriesInBatchRequestException>(() => client.DeleteMessageBatch(
                new DeleteMessageBatchRequest
                {
                    QueueUrl = helper.DefaultQueueUrl,
                    Entries = entries
                }));
        }

        [Test]
        public void Deleting_no_entries_throws_exception()
        {
            var qUrl = helper.CreateQueue();
            Assert.Throws<EmptyBatchRequestException>(() => client.DeleteMessageBatch(
                new DeleteMessageBatchRequest
                {
                    QueueUrl = qUrl,
                    Entries = new List<DeleteMessageBatchRequestEntry>()
                }));
        }

        [Test]
        public void Deleting_from_non_existent_q_throws_exception()
        {
            var entries = 1.Times(() => new DeleteMessageBatchRequestEntry());
            var queueUrl = $"http://{Guid.NewGuid():N}.com";
            
            SqsTestAssert.Throws<QueueDoesNotExistException>(() => 
                client.DeleteMessageBatch(new DeleteMessageBatchRequest(queueUrl, entries)), 
                "specified queue does not exist");
        }

        [Test]
        public void Can_delete_existing_queue()
        {
            var qUrl = helper.CreateQueue();

            var response = client.DeleteQueue(new DeleteQueueRequest(qUrl));

            Assert.IsNotNull(response);
        }
        
        [Test]
        public void Can_get_url_for_existing_q()
        {
            var qName = Guid.NewGuid().ToString("N");

            var qUrl = helper.CreateQueue(qName);

            var response = client.GetQueueUrl(new GetQueueUrlRequest(qName));

            Assert.IsNotNull(response);
            Assert.AreEqual(qUrl, response.QueueUrl);
        }

        [Test]
        public void Getting_url_for_non_existent_queue_throws_exception()
        {
            Assert.Throws<QueueDoesNotExistException>(() => 
                client.GetQueueUrl(new GetQueueUrlRequest(Guid.NewGuid().ToString("N"))));
        }

        [Test]
        public void Receive_with_empty_queue_waits_time_specified()
        {
            var qUrl = helper.CreateQueue();

            var sw = Stopwatch.StartNew();

            var response = client.ReceiveMessage(new ReceiveMessageRequest
            {
                QueueUrl = qUrl,
                MaxNumberOfMessages = 1,
                VisibilityTimeout = 30,
                WaitTimeSeconds = 2
            });

            sw.Stop();

            Assert.AreEqual(0, response.Messages.Count);
            Assert.GreaterOrEqual(sw.ElapsedMilliseconds, 2000);
        }

        [Test]
        public void Receive_with_non_empty_queue_waits_time_specified_for_max_num_messages()
        {
            var qUrl = helper.CreateQueue();

            helper.SendMessages(qUrl, count: 3);

            var sw = Stopwatch.StartNew();

            var response = client.ReceiveMessage(new ReceiveMessageRequest
            {
                QueueUrl = qUrl,
                MaxNumberOfMessages = 4,
                VisibilityTimeout = 30,
                WaitTimeSeconds = 2
            });

            sw.Stop();

            SqsTestAssert.FakeEqualRealGreater(3, 1, response.Messages.Count);

            if (SqsTestAssert.IsFakeClient)
            {   // SQS support for long polling doesn't guarantee a specific wait time oddly
                Assert.GreaterOrEqual(sw.ElapsedMilliseconds, 2000);
            }
        }
        
    }
}