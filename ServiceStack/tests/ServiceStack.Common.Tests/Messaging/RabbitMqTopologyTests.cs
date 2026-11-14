#if NETCORE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RabbitMQ.Client;
using ServiceStack.Messaging;
using ServiceStack.RabbitMq;

namespace ServiceStack.Common.Tests.Messaging;

[TestFixture]
public class RabbitMqTopologyTests
{
    public class Request { }

    public class ModelProxy : DispatchProxy
    {
        public readonly List<(string Name, bool Durable, bool Exclusive, bool AutoDelete)> Queues = new();
        public readonly List<string> Bindings = new();
        public IModel Model;
        public bool MissingOnFirstGet;
        protected override object Invoke(MethodInfo method, object[] args)
        {
            switch (method.Name)
            {
                case "get_IsOpen": return true;
                case "BasicGet":
                    if (MissingOnFirstGet)
                    {
                        MissingOnFirstGet = false;
                        throw new RabbitMQ.Client.Exceptions.OperationInterruptedException(
                            new ShutdownEventArgs(ShutdownInitiator.Peer, 404, "missing queue"));
                    }
                    return null;
                case "CreateModel": return Model;
                case "QueueDeclare":
                    Queues.Add(((string)args[0], (bool)args[1], (bool)args[2], (bool)args[3]));
                    return new QueueDeclareOk((string)args[0], 0, 0);
                case "QueueBind": Bindings.Add((string)args[0]); break;
                case "CreateBasicProperties": return new RabbitMqUnitTests.MockBasicProperties();
            }
            return method.ReturnType == typeof(void) || !method.ReturnType.IsValueType
                ? null : Activator.CreateInstance(method.ReturnType);
        }
    }

    static IModel CreateModel(out ModelProxy recorder)
    {
        var model = DispatchProxy.Create<IModel, ModelProxy>();
        recorder = (ModelProxy)(object)model;
        return model;
    }

    static RabbitMqQueueClient CreateClient(RabbitMqMessageFactory factory, IModel model)
    {
        var client = (RabbitMqQueueClient)factory.CreateMessageQueueClient();
        typeof(RabbitMqProducer).GetField("channel", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(client, model);
        return client;
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Both_topology_overloads_support_skipping_output_queues(bool generic)
    {
        var model = CreateModel(out var calls);
        if (generic)
            model.RegisterQueues<Request>(durableTopics: true, declareTopics: false);
        else
            model.RegisterQueues(new QueueNames(typeof(Request)), durableTopics: true, declareTopics: false);
        Assert.That(calls.Queues.Select(x => x.Name), Is.EquivalentTo(new[] {
            QueueNames<Request>.In, QueueNames<Request>.Priority, QueueNames<Request>.Dlq }));
        Assert.That(calls.Bindings, Does.Not.Contain(QueueNames<Request>.Out));
        Assert.That(calls.Queues.All(x => x.Durable && !x.Exclusive && !x.AutoDelete), Is.True);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Original_topology_overloads_default_to_durable_shared_output(bool generic)
    {
        var model = CreateModel(out var calls);
        if (generic) model.RegisterQueues<Request>();
        else model.RegisterQueues(new QueueNames(typeof(Request)));
        Assert.That(calls.Queues.Count, Is.EqualTo(4));
        Assert.That(calls.Queues.All(x => x.Durable && !x.Exclusive && !x.AutoDelete), Is.True);
        Assert.That(calls.Bindings, Does.Contain(QueueNames<Request>.Out));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Startup_uses_server_options_without_an_AppHost(bool disable)
    {
        var model = CreateModel(out var calls);
        var connection = DispatchProxy.Create<IConnection, ModelProxy>();
        ((ModelProxy)(object)connection).Model = model;
        using var server = new RabbitMqServer { DisableTopicQueues = disable, DurableTopicQueues = false };
        typeof(RabbitMqServer).GetField("connection", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(server, connection);
        server.DisablePublishingToOutq = true;
        server.RegisterHandler<Request>(_ => null);
        server.Init();
        var output = calls.Queues.Where(x => x.Name == QueueNames<Request>.Out).ToList();
        Assert.That(output.Count, Is.EqualTo(disable ? 0 : 1));
        if (!disable) Assert.That(output[0].Durable, Is.False);
        Assert.That(calls.Queues.Where(x => x.Name != QueueNames<Request>.Out).All(x => x.Durable), Is.True);
        Assert.That(((RabbitMqMessageFactory)server.MessageFactory).DurableTopicQueues, Is.False);
    }

    [Test]
    public void Standalone_clients_use_their_own_durability_and_declaration_cache()
    {
        foreach (var durable in new[] { true, false })
        {
            var model = CreateModel(out var calls);
            var factory = new RabbitMqMessageFactory { DurableTopicQueues = durable };
            using var client = CreateClient(factory, model);
            client.Notify(QueueNames<Request>.Out, new Message<Request>(new Request()));
            client.GetAsync<Request>(QueueNames<Request>.Out);
            Assert.That(calls.Queues.Count, Is.EqualTo(1));
            Assert.That(calls.Queues[0].Durable, Is.EqualTo(durable));
            Assert.That(calls.Queues[0].Exclusive, Is.False);
            Assert.That(calls.Bindings, Does.Contain(QueueNames<Request>.Out));
        }
    }

    [Test]
    public void Lazy_polling_declares_durable_output_by_default()
    {
        var model = CreateModel(out var calls);
        using var client = CreateClient(new RabbitMqMessageFactory(), model);
        client.GetAsync<Request>(QueueNames<Request>.Out);
        Assert.That(calls.Queues.Single().Durable, Is.True);
    }

    [Test]
    public void Temporary_reply_queues_remain_transient_and_exclusive()
    {
        var model = CreateModel(out var calls);
        using var client = CreateClient(new RabbitMqMessageFactory(), model);
        client.GetTempQueueName();
        var queue = calls.Queues.Single();
        Assert.That(queue.Durable, Is.False);
        Assert.That(queue.Exclusive, Is.True);
        Assert.That(queue.AutoDelete, Is.True);
    }
    [Test]
    public void Missing_queue_recovery_preserves_factory_durability()
    {
        var model = CreateModel(out var calls);
        calls.MissingOnFirstGet = true;
        using var client = CreateClient(new RabbitMqMessageFactory { DurableTopicQueues = false }, model);
        client.GetAsync<Request>(QueueNames<Request>.Out);
        Assert.That(calls.Queues.Count, Is.EqualTo(2));
        Assert.That(calls.Queues.All(x => !x.Durable), Is.True);
    }

    // Opt in against a disposable broker; never point this at production queues.
    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void Live_broker_starts_and_processes_with_optional_output(bool disable, bool polling)
    {
        var host = Environment.GetEnvironmentVariable("SERVICESTACK_RABBITMQ_TEST_HOST");
        if (string.IsNullOrEmpty(host)) Assert.Ignore("Set SERVICESTACK_RABBITMQ_TEST_HOST to a disposable broker.");
        using var server = new RabbitMqServer(host) {
            DisableTopicQueues = disable,
            DisablePublishingToOutq = disable,
            UsePolling = polling,
        };
        using var connection = server.ConnectionFactory.CreateConnection();
        // Fresh channels keep cleanup independent of missing-queue errors.
        void Cleanup()
        {
            foreach (var name in QueueNames<Request>.AllQueueNames)
            {
                using var channel = connection.CreateModel();
                channel.QueueDelete(name);
            }
        }
        Cleanup();
        using var processed = new System.Threading.ManualResetEventSlim();
        server.RegisterHandler<Request>(_ => { processed.Set(); return null; });
        try
        {
            server.Start();
            using var client = server.CreateMessageQueueClient();
            client.Publish(new Request());
            Assert.That(processed.Wait(TimeSpan.FromSeconds(5)), Is.True);
            if (disable)
            {
                using var channel = connection.CreateModel();
                var ex = Assert.Throws<RabbitMQ.Client.Exceptions.OperationInterruptedException>(
                    () => channel.QueueDeclarePassive(QueueNames<Request>.Out));
                Assert.That(ex.ShutdownReason.ReplyCode, Is.EqualTo(404));
            }
            else
            {
                Assert.That(client.Get<Request>(QueueNames<Request>.Out, TimeSpan.FromSeconds(5)), Is.Not.Null);
                using var channel = connection.CreateModel();
                // Inequivalent durability would fail here, verifying the actual broker property.
                channel.QueueDeclare(QueueNames<Request>.Out, true, false, false, null);
            }
        }
        finally
        {
            server.Stop();
            server.WaitForWorkersToStop(TimeSpan.FromSeconds(5));
            Cleanup();
        }
    }

}
#endif
