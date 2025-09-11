using System;
using System.Collections.Generic;
using System.Threading;
using Amazon.SQS.Model;
using NUnit.Framework;
using ServiceStack.Aws.Sqs;

namespace ServiceStack.Aws.Tests.Sqs;

// Note: Test needs to run after SqsMqBufferTests
[TestFixture]
public class SqsMqBufferTestsDrain
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
    public void Buffers_are_drained_on_timer_even_if_not_full()
    {
        using var buffer = GetNewMqBuffer();

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

        using var buffer2 = GetNewMqBuffer();

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

        using var buffer3 = GetNewMqBuffer();

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