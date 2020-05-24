using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using RabbitMQ.Client;
using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.RabbitMq;
using ServiceStack.Text;

namespace ServiceStack.Server.Tests.Messaging
{
    public class Reverse
    {
        public string Value { get; set; }
    }

    public class Rot13
    {
        public string Value { get; set; }
    }

    public class AlwaysThrows
    {
        public string Value { get; set; }
    }

    [TestFixture, Category("Integration")]
    public class RabbitMqServerTests
    {
        static readonly string ConnectionString = Config.RabbitMQConnString;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            LogManager.LogFactory = new ConsoleLogFactory();
        }

        internal static RabbitMqServer CreateMqServer(int noOfRetries = 2)
        {
            var mqServer = new RabbitMqServer(ConnectionString);
            using var conn = mqServer.ConnectionFactory.CreateConnection();
            using var channel = conn.CreateModel();
            channel.PurgeQueue<Reverse>();
            channel.PurgeQueue<Rot13>();
            channel.PurgeQueue<Incr>();
            channel.PurgeQueue<Wait>();
            channel.PurgeQueue<Hello>();
            channel.PurgeQueue<HelloResponse>();
            channel.PurgeQueue<HelloNull>();
            return mqServer;
        }

        internal static void Publish_4_messages(IMessageQueueClient mqClient)
        {
            mqClient.Publish(new Reverse { Value = "Hello" });
            mqClient.Publish(new Reverse { Value = "World" });
            mqClient.Publish(new Reverse { Value = "ServiceStack" });
            mqClient.Publish(new Reverse { Value = "Redis" });
        }

        private static void Publish_4_Rot13_messages(IMessageQueueClient mqClient)
        {
            mqClient.Publish(new Rot13 { Value = "Hello" });
            mqClient.Publish(new Rot13 { Value = "World" });
            mqClient.Publish(new Rot13 { Value = "ServiceStack" });
            mqClient.Publish(new Rot13 { Value = "Redis" });
        }

        [Test]
        public void Utils_publish_Reverse_messages()
        {
            using (var mqHost = new RabbitMqServer(ConnectionString))
            using (var mqClient = mqHost.CreateMessageQueueClient())
            {
                Publish_4_messages(mqClient);
            }
        }

        [Test]
        public void Utils_publish_Rot13_messages()
        {
            using (var mqHost = new RabbitMqServer(ConnectionString))
            using (var mqClient = mqHost.CreateMessageQueueClient())
            {
                Publish_4_Rot13_messages(mqClient);
            }
        }

        [Test]
        public void Only_allows_1_BgThread_to_run_at_a_time()
        {
            using (var mqHost = CreateMqServer())
            {
                mqHost.RegisterHandler<Reverse>(x => x.GetBody().Value.Reverse());
                mqHost.RegisterHandler<Rot13>(x => x.GetBody().Value.ToRot13());

                5.Times(x => ThreadPool.QueueUserWorkItem(y => mqHost.Start()));
                ExecUtils.RetryOnException(() =>
                {
                    Thread.Sleep(100);
                    Assert.That(mqHost.GetStatus(), Is.EqualTo("Started"));
                    Assert.That(mqHost.BgThreadCount, Is.EqualTo(1));
                    Thread.Sleep(100);
                }, TimeSpan.FromSeconds(5));

                10.Times(x => ThreadPool.QueueUserWorkItem(y => mqHost.Stop()));
                ExecUtils.RetryOnException(() =>
                {
                    Thread.Sleep(100);
                    Assert.That(mqHost.GetStatus(), Is.EqualTo("Stopped"));
                    Thread.Sleep(100);
                }, TimeSpan.FromSeconds(5));

                ThreadPool.QueueUserWorkItem(y => mqHost.Start());
                ExecUtils.RetryOnException(() =>
                {
                    Thread.Sleep(100);
                    Assert.That(mqHost.GetStatus(), Is.EqualTo("Started"));
                    Assert.That(mqHost.BgThreadCount, Is.EqualTo(2));
                    Thread.Sleep(100);
                }, TimeSpan.FromSeconds(5));

                //Debug.WriteLine(mqHost.GetStats());
            }
        }

        [Test]
        public void Cannot_Start_a_Disposed_MqHost()
        {
            var mqHost = CreateMqServer();

            mqHost.RegisterHandler<Reverse>(x => x.GetBody().Value.Reverse());
            mqHost.Dispose();

            try
            {
                mqHost.Start();
                Assert.Fail("Should throw ObjectDisposedException");
            }
            catch (ObjectDisposedException) { }
        }

        [Test]
        public void Cannot_Stop_a_Disposed_MqHost()
        {
            var mqHost = CreateMqServer();

            mqHost.RegisterHandler<Reverse>(x => x.GetBody().Value.Reverse());
            mqHost.Start();
            Thread.Sleep(100);

            mqHost.Dispose();

            try
            {
                mqHost.Stop();
                Assert.Fail("Should throw ObjectDisposedException");
            }
            catch (ObjectDisposedException) { }
        }

        public class Incr
        {
            public int Value { get; set; }
        }

        [Test]
        public void Can_receive_and_process_same_reply_responses()
        {
            var called = 0;
            using (var mqHost = CreateMqServer())
            {

                using (var conn = mqHost.ConnectionFactory.CreateConnection())
                using (var channel = conn.CreateModel())
                {
                    channel.PurgeQueue<Incr>();
                }

                mqHost.RegisterHandler<Incr>(m =>
                {
                    Debug.WriteLine("In Incr #" + m.GetBody().Value);
                    Interlocked.Increment(ref called);
                    return m.GetBody().Value > 0 ? new Incr { Value = m.GetBody().Value - 1 } : null;
                });

                mqHost.Start();

                var incr = new Incr { Value = 5 };
                using (var mqClient = mqHost.CreateMessageQueueClient())
                {
                    mqClient.Publish(incr);
                }

                ExecUtils.RetryOnException(() =>
                {
                    Thread.Sleep(300);
                    Assert.That(called, Is.EqualTo(1 + incr.Value));
                }, TimeSpan.FromSeconds(5));
            }
        }

        public class Hello : IReturn<HelloResponse>
        {
            public string Name { get; set; }
        }
        public class HelloNull : IReturn<HelloResponse>
        {
            public string Name { get; set; }
        }
        public class HelloResponse
        {
            public string Result { get; set; }
        }

        [Test]
        public void Can_receive_and_process_standard_request_reply_combo()
        {
            using (var mqHost = CreateMqServer())
            {
                using (var conn = mqHost.ConnectionFactory.CreateConnection())
                using (var channel = conn.CreateModel())
                {
                    channel.PurgeQueue<Hello>();
                    channel.PurgeQueue<HelloResponse>();
                }

                string messageReceived = null;

                mqHost.RegisterHandler<Hello>(m =>
                    new HelloResponse { Result = "Hello, " + m.GetBody().Name });

                mqHost.RegisterHandler<HelloResponse>(m =>
                {
                    messageReceived = m.GetBody().Result; return null;
                });

                mqHost.Start();

                using (var mqClient = mqHost.CreateMessageQueueClient())
                {
                    var dto = new Hello { Name = "ServiceStack" };
                    mqClient.Publish(dto);

                    ExecUtils.RetryOnException(() =>
                    {
                        Thread.Sleep(300);
                        Assert.That(messageReceived, Is.EqualTo("Hello, ServiceStack"));
                    }, TimeSpan.FromSeconds(5));
                }
            }
        }

        public class Wait
        {
            public int ForMs { get; set; }
        }

        [Test]
        public void Can_handle_requests_concurrently_in_4_threads()
        {
            RunHandlerOnMultipleThreads(noOfThreads: 4, msgs: 10);
        }

        private static void RunHandlerOnMultipleThreads(int noOfThreads, int msgs)
        {
            using var mqHost = CreateMqServer();
            var timesCalled = 0;

            mqHost.RegisterHandler<Wait>(m => {
                Interlocked.Increment(ref timesCalled);
                Thread.Sleep(m.GetBody().ForMs);
                return null;
            }, noOfThreads);

            mqHost.Start();

            using var mqClient = mqHost.CreateMessageQueueClient();
            var dto = new Wait { ForMs = 100 };
            msgs.Times(i => mqClient.Publish(dto));

            ExecUtils.RetryOnException(() =>
            {
                Thread.Sleep(300);
                Assert.That(timesCalled, Is.EqualTo(msgs));
            }, TimeSpan.FromSeconds(5));
        }

        [Test]
        public void Can_publish_and_receive_messages_with_MessageFactory()
        {
            using (var mqFactory = new RabbitMqMessageFactory(Config.RabbitMQConnString))
            using (var mqClient = mqFactory.CreateMessageQueueClient())
            {
                mqClient.Publish(new Hello { Name = "Foo" });
                var msg = mqClient.Get<Hello>(QueueNames<Hello>.In);

                Assert.That(msg.GetBody().Name, Is.EqualTo("Foo"));
            }
        }

        [Test]
        public void Can_filter_published_and_received_messages()
        {
            string receivedMsgApp = null;
            string receivedMsgType = null;

            using (var mqServer = CreateMqServer())
            {
                mqServer.PublishMessageFilter = (queueName, properties, msg) =>
                {
                    properties.AppId = "app:{0}".Fmt(queueName);
                };
                mqServer.GetMessageFilter = (queueName, basicMsg) =>
                {
                    var props = basicMsg.BasicProperties;
                    receivedMsgType = props.Type; //automatically added by RabbitMqProducer
                receivedMsgApp = props.AppId;
                };

                mqServer.RegisterHandler<Hello>(m => {
                    return new HelloResponse { Result = "Hello, {0}!".Fmt(m.GetBody().Name) };
                });

                mqServer.Start();

                using (var mqClient = mqServer.CreateMessageQueueClient())
                {
                    mqClient.Publish(new Hello { Name = "Bugs Bunny" });
                }

                Thread.Sleep(100);

                Assert.That(receivedMsgApp, Is.EqualTo("app:{0}".Fmt(QueueNames<Hello>.In)));
                Assert.That(receivedMsgType, Is.EqualTo(typeof(Hello).Name));

                using (IConnection connection = mqServer.ConnectionFactory.CreateConnection())
                using (IModel channel = connection.CreateModel())
                {
                    var queueName = QueueNames<HelloResponse>.In;
                    channel.RegisterQueue(queueName);

                    var basicMsg = channel.BasicGet(queueName, autoAck: true);
                    var props = basicMsg.BasicProperties;

                    Assert.That(props.Type, Is.EqualTo(typeof(HelloResponse).Name));
                    Assert.That(props.AppId, Is.EqualTo("app:{0}".Fmt(queueName)));

                    var msg = basicMsg.ToMessage<HelloResponse>();
                    Assert.That(msg.GetBody().Result, Is.EqualTo("Hello, Bugs Bunny!"));
                }
            }
        }

        [Test]
        public void Messages_with_null_Response_is_published_to_OutMQ()
        {
            int msgsReceived = 0;
            using (var mqServer = CreateMqServer())
            {
                mqServer.RegisterHandler<HelloNull>(m =>
                {
                    Interlocked.Increment(ref msgsReceived);
                    return null;
                });

                mqServer.Start();

                using (var mqClient = mqServer.CreateMessageQueueClient())
                {
                    mqClient.Publish(new HelloNull { Name = "Into the Void" });

                    var msg = mqClient.Get<HelloNull>(QueueNames<HelloNull>.Out, TimeSpan.FromSeconds(5));
                    Assert.That(msg, Is.Not.Null);

                    HelloNull response = msg.GetBody();

                    Thread.Sleep(100);

                    Assert.That(response.Name, Is.EqualTo("Into the Void"));
                    Assert.That(msgsReceived, Is.EqualTo(1));
                }
            }
        }

        [Test]
        public void Messages_with_null_responses_are_not_published_when_DisablePublishingToOutq()
        {
            int msgsReceived = 0;
            using (var mqServer = CreateMqServer())
            {
                mqServer.DisablePublishingToOutq = true;
                mqServer.RegisterHandler<HelloNull>(m =>
                {
                    Interlocked.Increment(ref msgsReceived);
                    return null;
                });

                mqServer.Start();

                using (var mqClient = mqServer.CreateMessageQueueClient())
                {
                    mqClient.Publish(new HelloNull { Name = "Into the Void" });

                    var msg = mqClient.Get<HelloNull>(QueueNames<HelloNull>.Out, TimeSpan.FromSeconds(2));
                    Assert.That(msg, Is.Null);
                }
            }
        }

        [Test]
        public void Messages_with_null_Response_is_published_to_ReplyMQ()
        {
            int msgsReceived = 0;
            using (var mqServer = CreateMqServer())
            {
                mqServer.RegisterHandler<HelloNull>(m =>
                {
                    Interlocked.Increment(ref msgsReceived);
                    return null;
                });

                mqServer.Start();

                using (var mqClient = mqServer.CreateMessageQueueClient())
                {
                    var replyMq = mqClient.GetTempQueueName();
                    mqClient.Publish(new Message<HelloNull>(new HelloNull { Name = "Into the Void" })
                    {
                        ReplyTo = replyMq
                    });

                    var msg = mqClient.Get<HelloNull>(replyMq);

                    HelloNull response = msg.GetBody();

                    Thread.Sleep(100);

                    Assert.That(response.Name, Is.EqualTo("Into the Void"));
                    Assert.That(msgsReceived, Is.EqualTo(1));
                }
            }
        }
    }

    [Ignore("These Flaky tests pass when run manually")]
    [TestFixture, Category("Integration")]
    public class RabbitMqServerFragileTests
    {
        [Test]
        public void Does_process_all_messages_and_Starts_Stops_correctly_with_multiple_threads_racing()
        {
            using (var mqHost = RabbitMqServerTests.CreateMqServer())
            {
                using (var conn = mqHost.ConnectionFactory.CreateConnection())
                using (var channel = conn.CreateModel())
                {
                    channel.PurgeQueue<Reverse>();
                    channel.PurgeQueue<Rot13>();
                }

                var reverseCalled = 0;
                var rot13Called = 0;

                mqHost.RegisterHandler<Reverse>(x =>
                {
                    "Processing Reverse {0}...".Print(Interlocked.Increment(ref reverseCalled));
                    return x.GetBody().Value.Reverse();
                });
                mqHost.RegisterHandler<Rot13>(x =>
                {
                    "Processing Rot13 {0}...".Print(Interlocked.Increment(ref rot13Called));
                    return x.GetBody().Value.ToRot13();
                });

                using (var mqClient = mqHost.CreateMessageQueueClient())
                {
                    mqClient.Publish(new Reverse { Value = "Hello" });
                    mqClient.Publish(new Reverse { Value = "World" });
                    mqClient.Publish(new Rot13 { Value = "ServiceStack" });

                    mqHost.Start();

                    ExecUtils.RetryOnException(() =>
                    {
                        Assert.That(mqHost.GetStatus(), Is.EqualTo("Started"));
                        Assert.That(mqHost.GetStats().TotalMessagesProcessed, Is.EqualTo(3));
                        Thread.Sleep(100);
                    }, TimeSpan.FromSeconds(5));

                    mqClient.Publish(new Reverse { Value = "Foo" });
                    mqClient.Publish(new Rot13 { Value = "Bar" });

                    10.Times(x => ThreadPool.QueueUserWorkItem(y => mqHost.Start()));
                    Assert.That(mqHost.GetStatus(), Is.EqualTo("Started"));

                    5.Times(x => ThreadPool.QueueUserWorkItem(y => mqHost.Stop()));
                    ExecUtils.RetryOnException(() =>
                    {
                        Assert.That(mqHost.GetStatus(), Is.EqualTo("Stopped"));
                        Thread.Sleep(100);
                    }, TimeSpan.FromSeconds(5));

                    10.Times(x => ThreadPool.QueueUserWorkItem(y => mqHost.Start()));
                    ExecUtils.RetryOnException(() =>
                    {
                        Assert.That(mqHost.GetStatus(), Is.EqualTo("Started"));
                        Thread.Sleep(100);
                    }, TimeSpan.FromSeconds(5));

                    Debug.WriteLine("\n" + mqHost.GetStats());

                    Assert.That(mqHost.GetStats().TotalMessagesProcessed, Is.GreaterThanOrEqualTo(5));
                    Assert.That(reverseCalled, Is.EqualTo(3));
                    Assert.That(rot13Called, Is.EqualTo(2));
                }
            }
        }


        [Test]
        public void Does_retry_messages_with_errors_by_RetryCount()
        {
            var retryCount = 1;
            var totalRetries = 1 + retryCount; //in total, inc. first try

            using (var mqHost = RabbitMqServerTests.CreateMqServer(retryCount))
            {
                using (var conn = mqHost.ConnectionFactory.CreateConnection())
                using (var channel = conn.CreateModel())
                {
                    channel.PurgeQueue<Reverse>();
                    channel.PurgeQueue<Rot13>();
                    channel.PurgeQueue<AlwaysThrows>();
                }

                var reverseCalled = 0;
                var rot13Called = 0;

                mqHost.RegisterHandler<Reverse>(x => { Interlocked.Increment(ref reverseCalled); return x.GetBody().Value.Reverse(); });
                mqHost.RegisterHandler<Rot13>(x => { Interlocked.Increment(ref rot13Called); return x.GetBody().Value.ToRot13(); });
                mqHost.RegisterHandler<AlwaysThrows>(x => { throw new Exception("Always Throwing! " + x.GetBody().Value); });
                mqHost.Start();

                using (var mqClient = mqHost.CreateMessageQueueClient())
                {
                    mqClient.Publish(new AlwaysThrows { Value = "1st" });
                    mqClient.Publish(new Reverse { Value = "Hello" });
                    mqClient.Publish(new Reverse { Value = "World" });
                    mqClient.Publish(new Rot13 { Value = "ServiceStack" });

                    ExecUtils.RetryOnException(() =>
                    {
                        Assert.That(mqHost.GetStats().TotalMessagesFailed, Is.EqualTo(1 * totalRetries));
                        Assert.That(mqHost.GetStats().TotalMessagesProcessed, Is.EqualTo(2 + 1));
                        Thread.Sleep(100);
                    }, TimeSpan.FromSeconds(5));

                    5.Times(x => mqClient.Publish(new AlwaysThrows { Value = "#" + x }));

                    mqClient.Publish(new Reverse { Value = "Hello" });
                    mqClient.Publish(new Reverse { Value = "World" });
                    mqClient.Publish(new Rot13 { Value = "ServiceStack" });
                }
                //Debug.WriteLine(mqHost.GetStatsDescription());

                ExecUtils.RetryOnException(() =>
                {
                    Assert.That(mqHost.GetStats().TotalMessagesFailed, Is.EqualTo((1 + 5) * totalRetries));
                    Assert.That(mqHost.GetStats().TotalMessagesProcessed, Is.EqualTo(6));

                    Assert.That(reverseCalled, Is.EqualTo(2 + 2));
                    Assert.That(rot13Called, Is.EqualTo(1 + 1));

                    Thread.Sleep(100);
                }, TimeSpan.FromSeconds(5));
            }
        }

        [Test]
        public void Does_process_messages_sent_before_it_was_started()
        {
            var reverseCalled = 0;

            using (var mqServer = RabbitMqServerTests.CreateMqServer())
            {
                using (var conn = mqServer.ConnectionFactory.CreateConnection())
                using (var channel = conn.CreateModel())
                {
                    channel.PurgeQueue<Reverse>();
                }

                mqServer.RegisterHandler<Reverse>(x => { Interlocked.Increment(ref reverseCalled); return x.GetBody().Value.Reverse(); });

                using (var mqClient = mqServer.CreateMessageQueueClient())
                {
                    RabbitMqServerTests.Publish_4_messages(mqClient);

                    mqServer.Start();

                    ExecUtils.RetryOnException(() =>
                    {
                        Assert.That(mqServer.GetStats().TotalMessagesProcessed, Is.EqualTo(4));
                        Assert.That(reverseCalled, Is.EqualTo(4));
                        Thread.Sleep(100);
                    }, TimeSpan.FromSeconds(5));
                }
            }
        }
    }
}