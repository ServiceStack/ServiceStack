using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Messaging;

namespace ServiceStack.WebHost.Endpoints.Tests;

[DataContract]
public class TestMqMessage
{
    [DataMember]
    public int Id { get; set; }
    [DataMember]
    public string Name { get; set; }
}

[TestFixture]
public class MessagingModernizationTests
{
    [Test]
    public void TransientMessageServiceBase_GetStats_WhenStopped_DoesNotThrow()
    {
        using var service = new InMemoryTransientMessageService();
        // Service is not started, messageHandlers is null
        IMessageHandlerStats stats = null;
        Assert.DoesNotThrow(() => stats = service.GetStats());
        Assert.That(stats, Is.Not.Null);
        Assert.That(stats.TotalMessagesProcessed, Is.EqualTo(0));

        string description = null;
        Assert.DoesNotThrow(() => description = service.GetStatsDescription());
        Assert.That(description, Is.Not.Null);
        Assert.That(description, Does.Contain("#MQ HOST STATS:"));

        Assert.DoesNotThrow(() => service.DisposeMessageHandler(null));
    }

    [Test]
    public void TransientMessageServiceBase_RegisteredTypes_ThreadSafe()
    {
        using var service = new InMemoryTransientMessageService();
        service.RegisterHandler<TestMqMessage>(m => null);

        var types = service.RegisteredTypes;
        Assert.That(types, Does.Contain(typeof(TestMqMessage)));

        Assert.Throws<ArgumentException>(() =>
        {
            service.RegisterHandler<TestMqMessage>(m => null);
        });
    }

    [Test]
    public void InMemoryTransientMessageFactory_NullGuardsAndSendAllOneWay()
    {
        using var service = new InMemoryTransientMessageService();
        using var producer = service.MessageFactory.CreateMessageProducer();

        // Null checks
        Assert.DoesNotThrow(() => producer.Publish((IMessage<TestMqMessage>)null));
        Assert.DoesNotThrow(() => ((IOneWayClient)producer).SendOneWay(null));
        Assert.DoesNotThrow(() => ((IOneWayClient)producer).SendOneWay(null, null));
        Assert.DoesNotThrow(() => ((IOneWayClient)producer).SendAllOneWay(null));

        var messagesReceived = new List<TestMqMessage>();
        service.RegisterHandler<TestMqMessage>(m =>
        {
            lock (messagesReceived)
            {
                messagesReceived.Add(m.GetBody());
            }
            return null;
        });

        var batch = new object[]
        {
            new TestMqMessage { Id = 1, Name = "A" },
            null,
            new TestMqMessage { Id = 2, Name = "B" }
        };

        Assert.DoesNotThrow(() => ((IOneWayClient)producer).SendAllOneWay(batch));

        lock (messagesReceived)
        {
            Assert.That(messagesReceived.Count, Is.EqualTo(2));
            Assert.That(messagesReceived.Any(x => x.Id == 1 && x.Name == "A"), Is.True);
            Assert.That(messagesReceived.Any(x => x.Id == 2 && x.Name == "B"), Is.True);
        }
    }

    [Test]
    public void InMemoryTransientMessageService_LifecycleAndCleanDisposal()
    {
        var service = new InMemoryTransientMessageService();
        service.RegisterHandler<TestMqMessage>(m => null);
        Assert.That(service.GetStatus(), Is.EqualTo("Stopped"));

        // Start() processes queue synchronously in TransientMessageServiceBase and stops at end
        service.Start();
        Assert.That(service.GetStatus(), Is.EqualTo("Stopped"));

        service.Stop();
        Assert.That(service.GetStatus(), Is.EqualTo("Stopped"));

        Assert.DoesNotThrow(() => service.Dispose());
        // Double dispose safe
        Assert.DoesNotThrow(() => service.Dispose());
    }

    [Test]
    public void BackgroundMqService_PublishUnknownQueues_BeforeStartAndAfterStop()
    {
        using var bgService = new BackgroundMqService();

        // Publish before Start() - should not throw NullReferenceException
        Assert.DoesNotThrow(() => bgService.Publish("unknown.queue", new Message<TestMqMessage>(new TestMqMessage { Id = 100 })));

        bgService.RegisterHandler<TestMqMessage>(m => null);
        bgService.Start();
        Assert.That(bgService.GetStatus(), Is.EqualTo("Started"));

        // Publish while running
        Assert.DoesNotThrow(() => bgService.Publish("unknown.queue2", new Message<TestMqMessage>(new TestMqMessage { Id = 101 })));

        bgService.Stop();
        Assert.That(bgService.GetStatus(), Is.EqualTo("Stopped"));

        // Publish after Stop()
        Assert.DoesNotThrow(() => bgService.Publish("unknown.queue3", new Message<TestMqMessage>(new TestMqMessage { Id = 102 })));

        bgService.Dispose();
        // Double dispose safe
        Assert.DoesNotThrow(() => bgService.Dispose());
    }

    [Test]
    public void MessageHandler_NullGuards()
    {
        using var service = new InMemoryTransientMessageService();
        var handler = new MessageHandler<TestMqMessage>(service, m => null);

        Assert.DoesNotThrow(() => handler.ProcessMessage(null, (IMessage<TestMqMessage>)null));
        Assert.DoesNotThrow(() => handler.ProcessMessage(null, (object)null));
    }
}
