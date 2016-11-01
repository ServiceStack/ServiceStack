using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Funq;
using NUnit.Framework;
using ServiceStack.Messaging;
using ServiceStack.Messaging.Redis;
using ServiceStack.RabbitMq;
using ServiceStack.Redis;
using ServiceStack.Text;

namespace ServiceStack.Server.Tests.Messaging
{
    public class RabbitMqRequestReplyTests : MqRequestReplyTests
    {
        public override IMessageService CreateMqServer(int retryCount = 1)
        {
            return new RabbitMqServer { RetryCount = retryCount };
        }
    }

    public class RedisMqRequestReplyTests : MqRequestReplyTests
    {
        public override IMessageService CreateMqServer(int retryCount = 1)
        {
            return new RedisMqServer(new BasicRedisClientManager()) { RetryCount = retryCount };
        }

        [Test]
        public void Can_expire_temp_queues()
        {
            using (var mqServer = (RedisMqServer)CreateMqServer())
            using (var client = mqServer.ClientsManager.GetClient())
            {
                client.FlushAll();

                100.Times(i =>
                    client.AddItemToList(QueueNames.GetTempQueueName(), i.ToString()));

                var itemsToExpire = mqServer.ExpireTemporaryQueues(afterMs: 100);

                var tmpWildCard = QueueNames.TempMqPrefix + "*";
                Assert.That(itemsToExpire, Is.EqualTo(100));
                Assert.That(client.SearchKeys(tmpWildCard).Count, Is.EqualTo(100));
                Thread.Sleep(200);
                Assert.That(client.SearchKeys(tmpWildCard).Count, Is.EqualTo(0));
            }
        }
    }

    public class InMemoryMqRequestReplyTests : MqRequestReplyTests
    {
        public override IMessageService CreateMqServer(int retryCount = 1)
        {
            return new InMemoryTransientMessageService { RetryCount = retryCount };
        }
    }

    [Explicit("Integration Tests")]
    [TestFixture]
    public abstract class MqRequestReplyTests
    {
        public abstract IMessageService CreateMqServer(int retryCount = 1);

        [Test]
        public void Can_publish_messages_to_the_ReplyTo_temporary_queue()
        {
            using (var mqServer = CreateMqServer())
            {
                mqServer.RegisterHandler<HelloIntro>(m =>
                    new HelloIntroResponse { Result = "Hello, {0}!".Fmt(m.GetBody().Name) });
                mqServer.Start();

                using (var mqClient = mqServer.CreateMessageQueueClient())
                {
                    var replyToMq = mqClient.GetTempQueueName();
                    mqClient.Publish(new Message<HelloIntro>(new HelloIntro { Name = "World" })
                    {
                        ReplyTo = replyToMq
                    });

                    IMessage<HelloIntroResponse> responseMsg = mqClient.Get<HelloIntroResponse>(replyToMq);
                    mqClient.Ack(responseMsg);
                    Assert.That(responseMsg.GetBody().Result, Is.EqualTo("Hello, World!"));
                }
            }
        }

        [Test]
        public void Can_send_message_with_custom_Tag()
        {
            using (var mqServer = CreateMqServer())
            {
                if (mqServer is RabbitMqServer)
                    return; //Uses DeliveryTag for Tag

                mqServer.RegisterHandler<HelloIntro>(m =>
                    new Message<HelloIntroResponse>(new HelloIntroResponse { Result = "Hello, {0}!".Fmt(m.GetBody().Name) }) { Tag = m.Tag });
                mqServer.Start();

                using (var mqClient = mqServer.CreateMessageQueueClient())
                {
                    var replyToMq = mqClient.GetTempQueueName();
                    mqClient.Publish(new Message<HelloIntro>(new HelloIntro { Name = "World" })
                    {
                        ReplyTo = replyToMq,
                        Tag = "Custom"
                    });

                    IMessage<HelloIntroResponse> responseMsg = mqClient.Get<HelloIntroResponse>(replyToMq);
                    mqClient.Ack(responseMsg);
                    Assert.That(responseMsg.GetBody().Result, Is.EqualTo("Hello, World!"));
                    Assert.That(responseMsg.Tag, Is.EqualTo("Custom"));
                }
            }
        }

        [Test]
        public void Can_send_message_with_custom_Header()
        {
            using (var mqServer = CreateMqServer())
            {
                mqServer.RegisterHandler<HelloIntro>(m =>
                    new Message<HelloIntroResponse>(new HelloIntroResponse { Result = "Hello, {0}!".Fmt(m.GetBody().Name) }) { Meta = m.Meta });
                mqServer.Start();

                using (var mqClient = mqServer.CreateMessageQueueClient())
                {
                    var replyToMq = mqClient.GetTempQueueName();
                    mqClient.Publish(new Message<HelloIntro>(new HelloIntro { Name = "World" })
                    {
                        ReplyTo = replyToMq,
                        Meta = new Dictionary<string, string> { { "Custom", "Header" } }
                    });

                    IMessage<HelloIntroResponse> responseMsg = mqClient.Get<HelloIntroResponse>(replyToMq);
                    mqClient.Ack(responseMsg);
                    Assert.That(responseMsg.GetBody().Result, Is.EqualTo("Hello, World!"));
                    Assert.That(responseMsg.Meta["Custom"], Is.EqualTo("Header"));
                }
            }
        }

        public class Incr
        {
            public long Value { get; set; }
        }

        public class IncrResponse
        {
            public long Result { get; set; }
        }

        [Explicit("Takes too long")]
        [Test]
        public void Can_handle_multiple_rpc_clients()
        {
            int NoOfClients = 10;
            int TimeMs = 5000;

            var errors = new ConcurrentDictionary<long, string>();
            using (var mqServer = CreateMqServer())
            {
                mqServer.RegisterHandler<Incr>(m =>
                    new IncrResponse { Result = m.GetBody().Value + 1 });
                mqServer.Start();

                long counter = 0;
                int activeClients = 0;
                var activeClientsLock = new object();

                NoOfClients.Times(() => {
                    ThreadPool.QueueUserWorkItem(_ => {
                        using (var mqClient = mqServer.CreateMessageQueueClient())
                        {
                            var sw = Stopwatch.StartNew();
                            var clientId = Interlocked.Increment(ref activeClients);
                            while (sw.ElapsedMilliseconds < TimeMs)
                            {
                                var next = Interlocked.Increment(ref counter);
                                try
                                {
                                    var replyToMq = mqClient.GetTempQueueName();
                                    mqClient.Publish(new Message<Incr>(new Incr { Value = next })
                                    {
                                        ReplyTo = replyToMq
                                    });

                                    var responseMsg = mqClient.Get<IncrResponse>(replyToMq, TimeSpan.FromMilliseconds(TimeMs));
                                    mqClient.Ack(responseMsg);

                                    var actual = responseMsg.GetBody().Result;
                                    var expected = next + 1;
                                    if (actual != expected)
                                    {
                                        errors[next] = string.Format("Actual: {1}, Expected: {0}",
                                            actual, expected);
                                    }

                                }
                                catch (Exception ex)
                                {
                                    errors[next] = ex.Message + "\nStackTrace:\n" + ex.StackTrace;
                                }
                            }

                            "Client {0} finished".Print(clientId);
                            if (Interlocked.Decrement(ref activeClients) == 0)
                            {
                                "All Clients Finished".Print();
                                lock (activeClientsLock)
                                    Monitor.Pulse(activeClientsLock);
                            }
                        }
                    });
                });

                lock (activeClientsLock)
                    Monitor.Wait(activeClientsLock);
    
                "Stopping Server...".Print();
                "Requests: {0}".Print(counter);
            }

            Assert.That(errors.Count, Is.EqualTo(0));
        }
    }
}
