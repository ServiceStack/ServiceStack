using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Amazon.SQS.Model;
using NUnit.Framework;
using ServiceStack.Aws.Sqs;
using ServiceStack.Aws.Sqs.Fake;
using ServiceStack.Text;

namespace ServiceStack.Aws.Tests.Sqs
{
    [TestFixture]
    public class SqsMqBufferTests
    {
        private SqsQueueManager sqsQueueManager;
        private SqsMqBufferFactory sqsMqBufferFactory;

        [OneTimeSetUp]
        public void FixtureSetup()
        {
            sqsQueueManager = new SqsQueueManager(SqsTestClientFactory.GetConnectionFactory());
            sqsMqBufferFactory = new SqsMqBufferFactory(SqsTestClientFactory.GetConnectionFactory());
        }

        [OneTimeTearDown]
        public void FixtureTeardown()
        {
            if (SqsTestAssert.IsFakeClient)
                return;

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

        private ISqsMqBuffer GetNewMqBuffer(int? visibilityTimeoutSeconds = null,
                                            int? receiveWaitTimeSeconds = null,
                                            bool? disasbleBuffering = null)
        {
            var qd = sqsQueueManager.CreateQueue(GetNewId(), visibilityTimeoutSeconds, receiveWaitTimeSeconds, disasbleBuffering);
            var buffer = sqsMqBufferFactory.GetOrCreate(qd);
            return buffer;
        }

        private string GetNewId()
        {
            return Guid.NewGuid().ToString("N");
        }

        [Test]
        public void Can_send_and_receive_message_with_Attributes()
        {
            var buffer = GetNewMqBuffer(disasbleBuffering: true);

            var msgBody = "Test Body";
            buffer.Send(new SendMessageRequest
            {
                QueueUrl = buffer.QueueDefinition.QueueUrl,
                MessageBody = msgBody,
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    { "Custom", new MessageAttributeValue { DataType = "String", StringValue = "Header" } },
                }
            });

            var responseMsg = buffer.Receive(new ReceiveMessageRequest
            {
                QueueUrl = buffer.QueueDefinition.QueueUrl,
                MessageAttributeNames = new List<string> { "All" },
            });

            Assert.That(responseMsg.Body, Is.EqualTo(msgBody));

            Assert.That(responseMsg.MessageAttributes["Custom"].StringValue, Is.EqualTo("Header"));
        }

        [Test]
        public void Send_is_not_buffered_when_buffering_disabled()
        {
            var buffer = GetNewMqBuffer(disasbleBuffering: true);

            var sent = buffer.Send(new SendMessageRequest
            {
                QueueUrl = buffer.QueueDefinition.QueueUrl,
                MessageBody = GetNewId()
            });

            Assert.IsTrue(sent);
            Assert.AreEqual(0, buffer.SendBufferCount);
        }

        [Test]
        public void Send_is_buffered_when_buffering_enabled_and_disposing_drains()
        {
            var buffer = GetNewMqBuffer();

            var sent = buffer.Send(new SendMessageRequest
            {
                QueueUrl = buffer.QueueDefinition.QueueUrl,
                MessageBody = GetNewId()
            });

            Assert.IsFalse(sent);
            Assert.AreEqual(1, buffer.SendBufferCount, "Send did not buffer");

            buffer.Dispose();

            Assert.AreEqual(0, buffer.SendBufferCount, "Dispose did not drain");
        }

        [Test]
        public void Delete_is_not_buffered_when_buffering_disabled()
        {
            var buffer = GetNewMqBuffer(disasbleBuffering: true);

            Assert.Throws<ReceiptHandleIsInvalidException>(() => buffer.Delete(new DeleteMessageRequest
            {
                QueueUrl = buffer.QueueDefinition.QueueUrl,
                ReceiptHandle = GetNewId()
            }));
        }

        [Test]
        public void Delete_is_buffered_when_buffering_enabled_and_disposing_drains()
        {
            var buffer = GetNewMqBuffer();

            var deleted = buffer.Delete(new DeleteMessageRequest
            {
                QueueUrl = buffer.QueueDefinition.QueueUrl,
                ReceiptHandle = GetNewId()
            });

            Assert.IsFalse(deleted);
            Assert.AreEqual(1, buffer.DeleteBufferCount, "Delete did not buffer");

            buffer.Dispose();

            Assert.AreEqual(0, buffer.DeleteBufferCount, "Dispose did not drain");
        }

        [Test]
        public void Cv_is_not_buffered_when_buffering_disabled()
        {
            var buffer = GetNewMqBuffer(disasbleBuffering: true);

            Assert.Throws<ReceiptHandleIsInvalidException>(() => buffer.ChangeVisibility(new ChangeMessageVisibilityRequest
            {
                QueueUrl = buffer.QueueDefinition.QueueUrl,
                ReceiptHandle = GetNewId(),
                VisibilityTimeout = 0
            }));
        }

        [Test]
        public void Cv_is_buffered_when_buffering_enabled_and_disposing_drains()
        {
            var buffer = GetNewMqBuffer();

            var visChanged = buffer.ChangeVisibility(new ChangeMessageVisibilityRequest
            {
                QueueUrl = buffer.QueueDefinition.QueueUrl,
                ReceiptHandle = GetNewId(),
                VisibilityTimeout = 0
            });

            Assert.IsFalse(visChanged);
            Assert.AreEqual(1, buffer.ChangeVisibilityBufferCount, "CV did not buffer");

            buffer.Dispose();

            Assert.AreEqual(0, buffer.ChangeVisibilityBufferCount, "Dispose did not drain");
        }

        [Test]
        public void Receive_is_not_buffered_when_buffering_disabled()
        {
            var buffer = GetNewMqBuffer(disasbleBuffering: true);

            var body = GetNewId();

            var sent = buffer.Send(new SendMessageRequest
            {
                QueueUrl = buffer.QueueDefinition.QueueUrl,
                MessageBody = body
            });

            Assert.IsTrue(sent);
            Assert.AreEqual(0, buffer.SendBufferCount);

            var received = buffer.Receive(new ReceiveMessageRequest
            {
                QueueUrl = buffer.QueueDefinition.QueueUrl,
                MaxNumberOfMessages = 5
            });

            Assert.IsNotNull(received);
            Assert.AreEqual(body, received.Body);
            Assert.AreEqual(0, buffer.ReceiveBufferCount);
        }

        [Test]
        public void Receive_is_buffered_when_buffering_enabled_and_disposing_drains()
        {
            var buffer = GetNewMqBuffer();

            buffer.QueueDefinition.SendBufferSize = 1;

            10.Times(i =>
            {
                var sent = buffer.Send(new SendMessageRequest
                {
                    QueueUrl = buffer.QueueDefinition.QueueUrl,
                    MessageBody = GetNewId()
                });

                Assert.IsTrue(sent);
                Assert.AreEqual(0, buffer.SendBufferCount);
            });

            // Using a real SQS queue results in the Receive being sporadic in terms of actually returning
            // a batch of stuff on a receive call, as it is dependent on the size of the queue, where you land,
            // etc., so in a real SQS scenario, allow a few attempts to the server to actually receive a batch
            // of data, which is the best we can do

            var timesToTry = SqsTestAssert.IsFakeClient
                ? 1
                : 10;

            var attempts = 0;

            Message received = null;

            while (attempts < timesToTry)
            {
                received = buffer.Receive(new ReceiveMessageRequest
                {
                    QueueUrl = buffer.QueueDefinition.QueueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = SqsTestAssert.IsFakeClient
                        ? 0
                        : SqsQueueDefinition.MaxWaitTimeSeconds,
                    VisibilityTimeout = SqsQueueDefinition.MaxVisibilityTimeoutSeconds
                });

                if (received != null && buffer.ReceiveBufferCount > 0)
                    break;

                attempts++;
            }

            Assert.IsNotNull(received);
            SqsTestAssert.FakeEqualRealGreater(9, 0, buffer.ReceiveBufferCount);

            buffer.Dispose();

            Assert.AreEqual(0, buffer.ReceiveBufferCount, "Dispose did not drain");
        }

        [Test]
        public void ErrorHandler_is_called_for_failed_batch_send_items()
        {
            var buffer = GetNewMqBuffer();

            // Haven't figured out yet how to make a send fail at a REAL sqs instance, so this test can only
            // currently run if using fake...
            if (!SqsTestAssert.IsFakeClient)
            {
                Assert.Inconclusive("Have not figured out how to 'force' a send failure at a real SQS instance yet, until we do this test can only run against the Fake client.");
            }

            var itemsErrorHandled = new List<Exception>();

            buffer.ErrorHandler = itemsErrorHandled.Add;
            buffer.QueueDefinition.SendBufferSize = 5;

            3.Times(i => buffer.Send(new SendMessageRequest
            {
                QueueUrl = buffer.QueueDefinition.QueueUrl,
                MessageBody = GetNewId()
            }));

            var sent = false;

            2.Times(i =>
            {
                sent = buffer.Send(new SendMessageRequest
                {
                    QueueUrl = buffer.QueueDefinition.QueueUrl,
                    MessageBody = FakeSqsQueue.FakeBatchItemFailString
                });
            });

            Assert.IsTrue(sent);
            Assert.AreEqual(0, buffer.SendBufferCount);

            var messages = String.Join("\n|\n", itemsErrorHandled.Select(e => string.Concat("Type: [", e.GetType(), "]. Message: [", e.Message, "]")));
            Assert.AreEqual(2, itemsErrorHandled.Count, messages);
        }

        [Test]
        public void ErrorHandler_is_called_for_failed_batch_cv_items()
        {
            var buffer = GetNewMqBuffer();

            var itemsErrorHandled = new List<Exception>();

            buffer.ErrorHandler = itemsErrorHandled.Add;
            buffer.QueueDefinition.SendBufferSize = 1;
            buffer.QueueDefinition.ReceiveBufferSize = 1;
            buffer.QueueDefinition.ChangeVisibilityBufferSize = 5;

            // Buffer 3 that should be successful items
            3.Times(i =>
            {
                buffer.Send(new SendMessageRequest
                {
                    QueueUrl = buffer.QueueDefinition.QueueUrl,
                    MessageBody = GetNewId()
                });

                var message = buffer.Receive(new ReceiveMessageRequest
                {
                    QueueUrl = buffer.QueueDefinition.QueueUrl,
                    MaxNumberOfMessages = 1,
                    WaitTimeSeconds = 0
                });

                Assert.IsNotNull(message, "Receive message is null");
                Assert.That(message.ReceiptHandle, Is.Not.Null.Or.Empty, "ReceiptHandle is null or empty");

                buffer.ChangeVisibility(new ChangeMessageVisibilityRequest
                {
                    QueueUrl = buffer.QueueDefinition.QueueUrl,
                    ReceiptHandle = message.ReceiptHandle,
                    VisibilityTimeout = 10
                });
            });

            var sent = false;

            // 2 that should not
            2.Times(i =>
            {
                sent = buffer.ChangeVisibility(new ChangeMessageVisibilityRequest
                {
                    QueueUrl = buffer.QueueDefinition.QueueUrl,
                    ReceiptHandle = GetNewId(),
                    VisibilityTimeout = 10
                });
            });

            // Last send should have fired the buffers off for real
            Assert.IsTrue(sent);
            Assert.AreEqual(0, buffer.ChangeVisibilityBufferCount);

            var messages = string.Join("\n|\n", itemsErrorHandled.Select(e => string.Concat("Type: [", e.GetType(), "]. Message: [", e.Message, "]")));
            Assert.AreEqual(2, itemsErrorHandled.Count, messages);
        }

        [Test]
        public void ErrorHandler_is_called_for_failed_batch_delete_items()
        {
            var buffer = GetNewMqBuffer();

            var itemsErrorHandled = new List<Exception>();

            buffer.ErrorHandler = itemsErrorHandled.Add;
            buffer.QueueDefinition.SendBufferSize = 1;
            buffer.QueueDefinition.ReceiveBufferSize = 1;
            buffer.QueueDefinition.DeleteBufferSize = 5;

            // Buffer 3 that should be successful items
            3.Times(i =>
            {
                buffer.Send(new SendMessageRequest
                {
                    QueueUrl = buffer.QueueDefinition.QueueUrl,
                    MessageBody = GetNewId()
                });

                var message = buffer.Receive(new ReceiveMessageRequest
                {
                    QueueUrl = buffer.QueueDefinition.QueueUrl,
                    MaxNumberOfMessages = 1,
                    WaitTimeSeconds = 0
                });

                Assert.IsNotNull(message, "Receive message is null");
                Assert.That(message.ReceiptHandle, Is.Not.Null.Or.Empty, "ReceiptHandle is null or empty");

                buffer.Delete(new DeleteMessageRequest
                {
                    QueueUrl = buffer.QueueDefinition.QueueUrl,
                    ReceiptHandle = message.ReceiptHandle
                });
            });

            var sent = false;

            // 2 that should not
            2.Times(i =>
            {
                sent = buffer.Delete(new DeleteMessageRequest
                {
                    QueueUrl = buffer.QueueDefinition.QueueUrl,
                    ReceiptHandle = GetNewId()
                });
            });

            // Last send should have fired the buffers off for real
            Assert.IsTrue(sent);
            Assert.AreEqual(0, buffer.DeleteBufferCount);

            var messages = string.Join("\n|\n", itemsErrorHandled.Select(e => string.Concat("Type: [", e.GetType(), "]. Message: [", e.Message, "]")));
            Assert.AreEqual(2, itemsErrorHandled.Count, messages);
        }

        [Test]
        public void Buffers_are_drained_on_timer_even_if_not_full()
        {
            var buffer = GetNewMqBuffer();

            2.Times(i => buffer.Send(new SendMessageRequest
            {
                QueueUrl = buffer.QueueDefinition.QueueUrl,
                MessageBody = GetNewId()
            }));
            4.Times(i => buffer.Delete(new DeleteMessageRequest
            {
                QueueUrl = buffer.QueueDefinition.QueueUrl,
                ReceiptHandle = GetNewId()
            }));
            3.Times(i => buffer.ChangeVisibility(new ChangeMessageVisibilityRequest
            {
                QueueUrl = buffer.QueueDefinition.QueueUrl,
                ReceiptHandle = GetNewId()
            }));

            var buffer2 = GetNewMqBuffer();

            3.Times(i => buffer2.Send(new SendMessageRequest
            {
                QueueUrl = buffer2.QueueDefinition.QueueUrl,
                MessageBody = GetNewId()
            }));
            2.Times(i => buffer2.Delete(new DeleteMessageRequest
            {
                QueueUrl = buffer2.QueueDefinition.QueueUrl,
                ReceiptHandle = GetNewId()
            }));
            4.Times(i => buffer2.ChangeVisibility(new ChangeMessageVisibilityRequest
            {
                QueueUrl = buffer2.QueueDefinition.QueueUrl,
                ReceiptHandle = GetNewId()
            }));

            var buffer3 = GetNewMqBuffer();

            4.Times(i => buffer3.Send(new SendMessageRequest
            {
                QueueUrl = buffer3.QueueDefinition.QueueUrl,
                MessageBody = GetNewId()
            }));
            3.Times(i => buffer3.Delete(new DeleteMessageRequest
            {
                QueueUrl = buffer3.QueueDefinition.QueueUrl,
                ReceiptHandle = GetNewId()
            }));
            2.Times(i => buffer3.ChangeVisibility(new ChangeMessageVisibilityRequest
            {
                QueueUrl = buffer3.QueueDefinition.QueueUrl,
                ReceiptHandle = GetNewId()
            }));

            // Should all have something buffered
            Assert.Greater(buffer.SendBufferCount, 0, "1 SendBufferCount");
            Assert.Greater(buffer.DeleteBufferCount, 0, "1 DeleteBufferCount");
            Assert.Greater(buffer.ChangeVisibilityBufferCount, 0, "1 CvBufferCount");

            Assert.Greater(buffer2.SendBufferCount, 0, "2 SendBufferCount");
            Assert.Greater(buffer2.DeleteBufferCount, 0, "2 DeleteBufferCount");
            Assert.Greater(buffer2.ChangeVisibilityBufferCount, 0, "2 CvBufferCount");

            Assert.Greater(buffer3.SendBufferCount, 0, "3 SendBufferCount");
            Assert.Greater(buffer3.DeleteBufferCount, 0, "3 DeleteBufferCount");
            Assert.Greater(buffer3.ChangeVisibilityBufferCount, 0, "3 CvBufferCount");

            // Set the buffer flush on the factory. Setting it back to zero will still have the timer fire
            // at least once, and then it will be cleared after the first fire.
            sqsMqBufferFactory.BufferFlushIntervalSeconds = 1;
            sqsMqBufferFactory.BufferFlushIntervalSeconds = 0;
            Thread.Sleep(2000);

            // Should all be drained
            Assert.AreEqual(0, buffer.SendBufferCount, "1 SendBufferCount");
            Assert.AreEqual(0, buffer.DeleteBufferCount, "1 DeleteBufferCount");
            Assert.AreEqual(0, buffer.ChangeVisibilityBufferCount, "1 CvBufferCount");

            Assert.AreEqual(0, buffer2.SendBufferCount, "2 SendBufferCount");
            Assert.AreEqual(0, buffer2.DeleteBufferCount, "2 DeleteBufferCount");
            Assert.AreEqual(0, buffer2.ChangeVisibilityBufferCount, "2 CvBufferCount");

            Assert.AreEqual(0, buffer3.SendBufferCount, "3 SendBufferCount");
            Assert.AreEqual(0, buffer3.DeleteBufferCount, "3 DeleteBufferCount");
            Assert.AreEqual(0, buffer3.ChangeVisibilityBufferCount, "3 CvBufferCount");
        }

    }
}