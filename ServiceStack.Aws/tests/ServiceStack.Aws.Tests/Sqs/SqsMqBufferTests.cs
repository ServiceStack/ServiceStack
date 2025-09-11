using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Amazon.SQS.Model;
using NUnit.Framework;
using ServiceStack.Aws.Sqs;
using ServiceStack.Aws.Sqs.Fake;
using ServiceStack.Text;

namespace ServiceStack.Aws.Tests.Sqs;

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
        bool? disableBuffering = null)
    {
        var qd = sqsQueueManager.CreateQueue(GetNewId(), visibilityTimeoutSeconds, receiveWaitTimeSeconds, disableBuffering);
        var buffer = sqsMqBufferFactory.GetOrCreate(qd);
        return buffer;
    }

    private string GetNewId() => Guid.NewGuid().ToString("N");

    [Test]
    public void Can_send_and_receive_message_with_Attributes()
    {
        using var buffer = GetNewMqBuffer(disableBuffering: true);

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
            MessageAttributeNames = ["All"],
        });

        Assert.That(responseMsg.Body, Is.EqualTo(msgBody));

        Assert.That(responseMsg.MessageAttributes["Custom"].StringValue, Is.EqualTo("Header"));
    }

    [Test]
    public void Send_is_not_buffered_when_buffering_disabled()
    {
        using var buffer = GetNewMqBuffer(disableBuffering: true);

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
        using var buffer = GetNewMqBuffer();

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
        using var buffer = GetNewMqBuffer(disableBuffering: true);

        Assert.Throws<ReceiptHandleIsInvalidException>(() => buffer.Delete(new DeleteMessageRequest
        {
            QueueUrl = buffer.QueueDefinition.QueueUrl,
            ReceiptHandle = GetNewId()
        }));
    }

    [Test]
    public void Delete_is_buffered_when_buffering_enabled_and_disposing_drains()
    {
        using var buffer = GetNewMqBuffer();

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
        using var buffer = GetNewMqBuffer(disableBuffering: true);

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
        using var buffer = GetNewMqBuffer();

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
        using var buffer = GetNewMqBuffer(disableBuffering: true);

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
        using var buffer = GetNewMqBuffer();

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
        using var buffer = GetNewMqBuffer();

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
        using var buffer = GetNewMqBuffer();

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
        using var buffer = GetNewMqBuffer();

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

}