using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RabbitMQ.Client;
using ServiceStack.Messaging;
using ServiceStack.RabbitMq;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests.Messaging;

[TestFixture]
public class RabbitMqUnitTests
{
    public class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class MockBasicProperties : IBasicProperties
    {
        public string AppId { get; set; }
        public string ClusterId { get; set; }
        public string ContentEncoding { get; set; }
        public string ContentType { get; set; }
        public string CorrelationId { get; set; }
        public byte DeliveryMode { get; set; }
        public string Expiration { get; set; }
        public IDictionary<string, object> Headers { get; set; }
        public string MessageId { get; set; }
        public bool Persistent { get; set; }
        public byte Priority { get; set; }
        public string ReplyTo { get; set; }
        public PublicationAddress ReplyToAddress { get; set; }
        public AmqpTimestamp Timestamp { get; set; }
        public string Type { get; set; }
        public string UserId { get; set; }

        public ushort ProtocolClassId => 60;
        public string ProtocolClassName => "basic";

        public void ClearAppId() => AppId = null;
        public void ClearClusterId() => ClusterId = null;
        public void ClearContentEncoding() => ContentEncoding = null;
        public void ClearContentType() => ContentType = null;
        public void ClearCorrelationId() => CorrelationId = null;
        public void ClearDeliveryMode() => DeliveryMode = 0;
        public void ClearExpiration() => Expiration = null;
        public void ClearHeaders() => Headers = null;
        public void ClearMessageId() => MessageId = null;
        public void ClearPriority() => Priority = 0;
        public void ClearReplyTo() => ReplyTo = null;
        public void ClearTimestamp() => Timestamp = default;
        public void ClearType() => Type = null;
        public void ClearUserId() => UserId = null;

        public bool IsAppIdPresent() => AppId != null;
        public bool IsClusterIdPresent() => ClusterId != null;
        public bool IsContentEncodingPresent() => ContentEncoding != null;
        public bool IsContentTypePresent() => ContentType != null;
        public bool IsCorrelationIdPresent() => CorrelationId != null;
        public bool IsDeliveryModePresent() => DeliveryMode != 0;
        public bool IsExpirationPresent() => Expiration != null;
        public bool IsHeadersPresent() => Headers != null;
        public bool IsMessageIdPresent() => MessageId != null;
        public bool IsPriorityPresent() => Priority != 0;
        public bool IsReplyToPresent() => ReplyTo != null;
        public bool IsTimestampPresent() => Timestamp.UnixTime != 0;
        public bool IsTypePresent() => Type != null;
        public bool IsUserIdPresent() => UserId != null;
    }

    #region SharedQueue Tests

    [Test]
    public void SharedQueue_Enqueue_And_Dequeue_Fifo_Order()
    {
        var queue = new SharedQueue<int>();
        queue.Enqueue(10);
        queue.Enqueue(20);
        queue.Enqueue(30);

        Assert.That(queue.Dequeue(), Is.EqualTo(10));
        Assert.That(queue.Dequeue(), Is.EqualTo(20));
        Assert.That(queue.Dequeue(), Is.EqualTo(30));
    }

    [Test]
    public void SharedQueue_Dequeue_With_Timeout_Returns_False_When_Empty()
    {
        var queue = new SharedQueue<string>();
        var success = queue.Dequeue(TimeSpan.FromMilliseconds(50), out var result);

        Assert.That(success, Is.False);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void SharedQueue_Dequeue_With_Timeout_Returns_True_When_Available()
    {
        var queue = new SharedQueue<string>();
        queue.Enqueue("hello");

        var success = queue.Dequeue(TimeSpan.FromMilliseconds(100), out var result);

        Assert.That(success, Is.True);
        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public void SharedQueue_DequeueNoWait_Returns_Default_When_Empty()
    {
        var queue = new SharedQueue<string>();
        var result = queue.DequeueNoWait("fallback");

        Assert.That(result, Is.EqualTo("fallback"));

        queue.Enqueue("actual");
        Assert.That(queue.DequeueNoWait("fallback"), Is.EqualTo("actual"));
    }

    [Test]
    public void SharedQueue_Close_Causes_Waiting_Dequeue_To_Throw_EndOfStreamException()
    {
        var queue = new SharedQueue<int>();
        Exception caughtEx = null;

        var thread = new Thread(() =>
        {
            try
            {
                queue.Dequeue();
            }
            catch (Exception ex)
            {
                caughtEx = ex;
            }
        });

        thread.Start();
        Thread.Sleep(50);
        queue.Close();
        thread.Join(1000);

        Assert.That(caughtEx, Is.InstanceOf<EndOfStreamException>());
    }

    [Test]
    public void SharedQueue_Close_Causes_Enqueue_To_Throw_EndOfStreamException()
    {
        var queue = new SharedQueue<int>();
        queue.Close();

        Assert.Throws<EndOfStreamException>(() => queue.Enqueue(42));
    }

    [Test]
    public void SharedQueue_Enumerator_Throws_InvalidOperationException_Before_MoveNext()
    {
        var queue = new SharedQueue<int>();
        queue.Enqueue(100);

        using var enumerator = ((IEnumerable<int>)queue).GetEnumerator();
        Assert.Throws<InvalidOperationException>(() =>
        {
            var _ = enumerator.Current;
        });
    }

    [Test]
    public void SharedQueue_Enumerator_Iterates_And_Ends_On_Close()
    {
        var queue = new SharedQueue<string>();
        queue.Enqueue("item1");
        queue.Enqueue("item2");
        queue.Close();

        var list = new List<string>();
        foreach (var item in queue)
        {
            list.Add(item);
        }

        Assert.That(list, Is.EqualTo(new[] { "item1", "item2" }));
    }

    [Test]
    public void SharedQueue_Concurrent_Producer_Consumer()
    {
        var queue = new SharedQueue<int>();
        const int count = 500;
        var consumed = new List<int>();
        var consumerTask = Task.Run(() =>
        {
            for (int i = 0; i < count; i++)
            {
                consumed.Add(queue.Dequeue());
            }
        });

        var producerTask = Task.Run(() =>
        {
            for (int i = 0; i < count; i++)
            {
                queue.Enqueue(i);
            }
        });

        Task.WaitAll(producerTask, consumerTask);
        Assert.That(consumed.Count, Is.EqualTo(count));
        for (int i = 0; i < count; i++)
        {
            Assert.That(consumed[i], Is.EqualTo(i));
        }
    }

    #endregion

    #region RabbitMqExtensions.ToMessage Tests

    [Test]
    public void ToMessage_Deserializes_Json_Body()
    {
        var data = new TestData { Id = 123, Name = "Test" };
        var jsonBytes = Encoding.UTF8.GetBytes(data.ToJson());
        var props = new MockBasicProperties
        {
            MessageId = Guid.NewGuid().ToString(),
            Timestamp = new AmqpTimestamp(DateTime.UtcNow.ToUnixTime()),
            ContentType = MimeTypes.Json
        };

        var getResult = new BasicGetResult(
            deliveryTag: 1,
            redelivered: false,
            exchange: "ex",
            routingKey: "rk",
            messageCount: 0,
            basicProperties: props,
            body: jsonBytes);

        var msg = getResult.ToMessage<TestData>();

        Assert.That(msg, Is.Not.Null);
        Assert.That(msg.GetBody().Id, Is.EqualTo(123));
        Assert.That(msg.GetBody().Name, Is.EqualTo("Test"));
        Assert.That(msg.Id, Is.EqualTo(Guid.Parse(props.MessageId)));
    }

    [Test]
    public void ToMessage_Resilient_To_Non_Guid_MessageId()
    {
        var data = new TestData { Id = 456, Name = "NonGuid" };
        var jsonBytes = Encoding.UTF8.GetBytes(data.ToJson());
        var props = new MockBasicProperties
        {
            MessageId = "external-msg-99999", // non-Guid string
            Timestamp = new AmqpTimestamp(DateTime.UtcNow.ToUnixTime()),
            ContentType = MimeTypes.Json
        };

        var getResult = new BasicGetResult(1, false, "ex", "rk", 0, props, jsonBytes);

        var msg = getResult.ToMessage<TestData>();

        Assert.That(msg, Is.Not.Null);
        Assert.That(msg.GetBody().Id, Is.EqualTo(456));
        Assert.That(msg.Id, Is.EqualTo(Guid.Empty)); // Safely defaulted, no FormatException thrown
    }

    [Test]
    public void ToMessage_Resilient_To_Non_Guid_CorrelationId()
    {
        var data = new TestData { Id = 789, Name = "NonGuidCorr" };
        var jsonBytes = Encoding.UTF8.GetBytes(data.ToJson());
        var props = new MockBasicProperties
        {
            CorrelationId = "corr-custom-string-id", // non-Guid string
            Timestamp = new AmqpTimestamp(DateTime.UtcNow.ToUnixTime()),
            ContentType = MimeTypes.Json
        };

        var getResult = new BasicGetResult(1, false, "ex", "rk", 0, props, jsonBytes);

        var msg = getResult.ToMessage<TestData>();

        Assert.That(msg, Is.Not.Null);
        Assert.That(msg.ReplyId, Is.Null); // Safely skipped, no FormatException thrown
    }

    [Test]
    public void ToMessage_Parses_Valid_Guid_CorrelationId()
    {
        var corrId = Guid.NewGuid();
        var data = new TestData { Id = 1, Name = "WithCorr" };
        var jsonBytes = Encoding.UTF8.GetBytes(data.ToJson());
        var props = new MockBasicProperties
        {
            CorrelationId = corrId.ToString(),
            Timestamp = new AmqpTimestamp(DateTime.UtcNow.ToUnixTime()),
            ContentType = MimeTypes.Json
        };

        var getResult = new BasicGetResult(1, false, "ex", "rk", 0, props, jsonBytes);

        var msg = getResult.ToMessage<TestData>();

        Assert.That(msg, Is.Not.Null);
        Assert.That(msg.ReplyId, Is.EqualTo(corrId));
    }

    [Test]
    public void ToMessage_Populates_Error_And_Metadata()
    {
        var data = new TestData { Id = 2, Name = "ErrorTest" };
        var jsonBytes = Encoding.UTF8.GetBytes(data.ToJson());
        var errorStatus = new ResponseStatus { ErrorCode = "NotFound", Message = "Item not found" };
        var props = new MockBasicProperties
        {
            Timestamp = new AmqpTimestamp(DateTime.UtcNow.ToUnixTime()),
            ContentType = MimeTypes.Json,
            Headers = new Dictionary<string, object>
            {
                ["Error"] = errorStatus.ToJson().ToUtf8Bytes(),
                ["CustomHeader"] = "CustomValue".ToUtf8Bytes()
            }
        };

        var getResult = new BasicGetResult(1, false, "ex", "rk", 0, props, jsonBytes);

        var msg = getResult.ToMessage<TestData>();

        Assert.That(msg.Error, Is.Not.Null);
        Assert.That(msg.Error.ErrorCode, Is.EqualTo("NotFound"));
        Assert.That(msg.Error.Message, Is.EqualTo("Item not found"));
        Assert.That(msg.Meta, Is.Not.Null);
        Assert.That(msg.Meta["CustomHeader"], Is.EqualTo("CustomValue"));
    }

    #endregion

    #region RabbitMqExtensions.IsServerNamedQueue Tests

    [Test]
    public void IsServerNamedQueue_Identifies_Special_Queues()
    {
        Assert.That("amq.gen-ABC123xyz".IsServerNamedQueue(), Is.True);
        Assert.That("AMQ.GEN-ABC123XYZ".IsServerNamedQueue(), Is.True);
        Assert.That((QueueNames.TempMqPrefix + "custom-id").IsServerNamedQueue(), Is.True);
        Assert.That("normal.inq".IsServerNamedQueue(), Is.False);
        Assert.That("orders.priorityq".IsServerNamedQueue(), Is.False);

        Assert.Throws<ArgumentNullException>(() => ((string)null).IsServerNamedQueue());
        Assert.Throws<ArgumentNullException>(() => "".IsServerNamedQueue());
    }

    #endregion

    #region RabbitMqMessageFactory Tests

    [Test]
    public void RabbitMqMessageFactory_Parses_Connection_Strings()
    {
        var factory = new RabbitMqMessageFactory("rabbitmq.mydomain.com:5673");
        Assert.That(factory.ConnectionFactory.HostName, Is.EqualTo("rabbitmq.mydomain.com"));
        Assert.That(factory.ConnectionFactory.Port, Is.EqualTo(5673));

        var uriFactory = new RabbitMqMessageFactory("amqp://admin:secret@broker.local:5672/myvhost");
        Assert.That(uriFactory.ConnectionFactory.Uri, Is.EqualTo(new Uri("amqp://admin:secret@broker.local:5672/myvhost")));
    }

    [Test]
    public void RabbitMqMessageFactory_RetryCount_Validation()
    {
        var factory = new RabbitMqMessageFactory("localhost");
        factory.RetryCount = 0;
        Assert.That(factory.RetryCount, Is.EqualTo(0));
        factory.RetryCount = 1;
        Assert.That(factory.RetryCount, Is.EqualTo(1));

        Assert.Throws<ArgumentOutOfRangeException>(() => factory.RetryCount = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.RetryCount = 2);
    }

    #endregion

    #region RabbitMqQueueClient Delivery Tag Tests

    [Test]
    public void RabbitMqQueueClient_Ack_Guards_Invalid_Tags()
    {
        var factory = new RabbitMqMessageFactory("localhost");
        var client = new RabbitMqQueueClient(factory);

        Assert.Throws<ArgumentNullException>(() => client.Ack(null));
        Assert.Throws<ArgumentNullException>(() => client.Ack(new Message<string>("test") { Tag = null }));
        Assert.Throws<ArgumentException>(() => client.Ack(new Message<string>("test") { Tag = "not-a-number" }));
    }

    #endregion
}
