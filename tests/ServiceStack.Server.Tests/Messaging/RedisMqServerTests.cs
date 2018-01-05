using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.Messaging.Redis;
using ServiceStack.Redis;
using ServiceStack.Text;

namespace ServiceStack.Server.Tests.Messaging
{
    [TestFixture, Category("Integration")]
    public class RedisMqServerTests
    {
        public class Reverse
        {
            public string Value { get; set; }
        }

        public class Rot13
        {
            public string Value { get; set; }
        }

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            RedisClient.NewFactoryFn = () => new RedisClient(TestConfig.SingleHost);
            //LogManager.LogFactory = new ConsoleLogFactory();
        }

        private static RedisMqServer CreateMqServer(int noOfRetries = 2)
        {
            var redisFactory = TestConfig.BasicClientManger;
            try
            {
                redisFactory.Exec(redis => redis.FlushAll());
            }
            catch (RedisException rex)
            {
                Debug.WriteLine("WARNING: Redis not started? \n" + rex.Message);
            }
            var mqHost = new RedisMqServer(redisFactory, noOfRetries);
            return mqHost;
        }

        private static void Publish_4_messages(IMessageQueueClient mqClient)
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
            var mqHost = new RedisMqServer(TestConfig.BasicClientManger, 2);
            var mqClient = mqHost.CreateMessageQueueClient();
            Publish_4_messages(mqClient);
            mqHost.Stop();
        }

        [Test]
        public void Utils_publish_Rot13_messages()
        {
            var mqHost = new RedisMqServer(TestConfig.BasicClientManger, 2);
            var mqClient = mqHost.CreateMessageQueueClient();
            Publish_4_Rot13_messages(mqClient);
            mqHost.Stop();
        }

        [Test]
        public void Does_process_messages_sent_before_it_was_started()
        {
            var reverseCalled = 0;

            var mqHost = CreateMqServer();
            mqHost.RegisterHandler<Reverse>(x => { Interlocked.Increment(ref reverseCalled); return x.GetBody().Value.Reverse(); });

            var mqClient = mqHost.CreateMessageQueueClient();
            Publish_4_messages(mqClient);

            mqHost.Start();
            Thread.Sleep(3000);

            Assert.That(mqHost.GetStats().TotalMessagesProcessed, Is.EqualTo(4));
            Assert.That(reverseCalled, Is.EqualTo(4));

            mqHost.Dispose();
        }

        [Test]
        public void Does_process_all_messages_and_Starts_Stops_correctly_with_multiple_threads_racing()
        {
            var mqHost = CreateMqServer();

            var reverseCalled = 0;
            var rot13Called = 0;

            mqHost.RegisterHandler<Reverse>(x => { Interlocked.Increment(ref reverseCalled); return x.GetBody().Value.Reverse(); });
            mqHost.RegisterHandler<Rot13>(x => { Interlocked.Increment(ref rot13Called); return x.GetBody().Value.ToRot13(); });

            var mqClient = mqHost.CreateMessageQueueClient();
            mqClient.Publish(new Reverse { Value = "Hello" });
            mqClient.Publish(new Reverse { Value = "World" });
            mqClient.Publish(new Rot13 { Value = "ServiceStack" });

            mqHost.Start();
            Thread.Sleep(3000);
            Assert.That(mqHost.GetStatus(), Is.EqualTo("Started"));
            Assert.That(mqHost.GetStats().TotalMessagesProcessed, Is.EqualTo(3));

            mqClient.Publish(new Reverse { Value = "Foo" });
            mqClient.Publish(new Rot13 { Value = "Bar" });

            10.Times(x => ThreadPool.QueueUserWorkItem(y => mqHost.Start()));
            Assert.That(mqHost.GetStatus(), Is.EqualTo("Started"));

            5.Times(x => ThreadPool.QueueUserWorkItem(y => mqHost.Stop()));
            Thread.Sleep(1000);
            Assert.That(mqHost.GetStatus(), Is.EqualTo("Stopped"));

            10.Times(x => ThreadPool.QueueUserWorkItem(y => mqHost.Start()));
            Thread.Sleep(3000);
            Assert.That(mqHost.GetStatus(), Is.EqualTo("Started"));

            Debug.WriteLine("\n" + mqHost.GetStats());

            Assert.That(mqHost.GetStats().TotalMessagesProcessed, Is.EqualTo(5));
            Assert.That(reverseCalled, Is.EqualTo(3));
            Assert.That(rot13Called, Is.EqualTo(2));

            mqHost.Dispose();
        }

        [Test]
        public void Only_allows_1_BgThread_to_run_at_a_time()
        {
            var mqHost = CreateMqServer();
            var redisPubSub = (RedisPubSubServer)mqHost.RedisPubSub;

            mqHost.RegisterHandler<Reverse>(x => x.GetBody().Value.Reverse());
            mqHost.RegisterHandler<Rot13>(x => x.GetBody().Value.ToRot13());

            5.Times(x => ThreadPool.QueueUserWorkItem(y => mqHost.Start()));
            Thread.Sleep(1000);
            Assert.That(mqHost.GetStatus(), Is.EqualTo("Started"));
            Assert.That(redisPubSub.BgThreadCount, Is.EqualTo(1));

            10.Times(x => ThreadPool.QueueUserWorkItem(y => mqHost.Stop()));
            Thread.Sleep(1000);
            Assert.That(mqHost.GetStatus(), Is.EqualTo("Stopped"));

            ThreadPool.QueueUserWorkItem(y => mqHost.Start());
            Thread.Sleep(1000);
            Assert.That(mqHost.GetStatus(), Is.EqualTo("Started"));

            Assert.That(redisPubSub.BgThreadCount, Is.EqualTo(2));

            Debug.WriteLine(mqHost.GetStats());

            mqHost.Dispose();
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
            Thread.Sleep(1000);

            mqHost.Dispose();

            try
            {
                mqHost.Stop();
                Assert.Fail("Should throw ObjectDisposedException");
            }
            catch (ObjectDisposedException) { }
        }

        public class AlwaysThrows
        {
            public string Value { get; set; }
        }

        [Test]
        public void Does_retry_messages_with_errors_by_RetryCount()
        {
            var retryCount = 3;
            var totalRetries = 1 + retryCount; //in total, inc. first try

            var mqHost = CreateMqServer(retryCount);

            var reverseCalled = 0;
            var rot13Called = 0;

            mqHost.RegisterHandler<Reverse>(x => { Interlocked.Increment(ref reverseCalled); return x.GetBody().Value.Reverse(); });
            mqHost.RegisterHandler<Rot13>(x => { Interlocked.Increment(ref rot13Called); return x.GetBody().Value.ToRot13(); });
            mqHost.RegisterHandler<AlwaysThrows>(x => { throw new Exception("Always Throwing! " + x.GetBody().Value); });
            mqHost.Start();

            var mqClient = mqHost.CreateMessageQueueClient();
            mqClient.Publish(new AlwaysThrows { Value = "1st" });
            mqClient.Publish(new Reverse { Value = "Hello" });
            mqClient.Publish(new Reverse { Value = "World" });
            mqClient.Publish(new Rot13 { Value = "ServiceStack" });

            Thread.Sleep(3000);
            Assert.That(mqHost.GetStats().TotalMessagesFailed, Is.EqualTo(1 * totalRetries));
            Assert.That(mqHost.GetStats().TotalMessagesProcessed, Is.EqualTo(2 + 1));

            5.Times(x => mqClient.Publish(new AlwaysThrows { Value = "#" + x }));

            mqClient.Publish(new Reverse { Value = "Hello" });
            mqClient.Publish(new Reverse { Value = "World" });
            mqClient.Publish(new Rot13 { Value = "ServiceStack" });

            Thread.Sleep(5000);

            Debug.WriteLine(mqHost.GetStatsDescription());

            Assert.That(mqHost.GetStats().TotalMessagesFailed, Is.EqualTo((1 + 5) * totalRetries));
            Assert.That(mqHost.GetStats().TotalMessagesProcessed, Is.EqualTo(6));

            Assert.That(reverseCalled, Is.EqualTo(2 + 2));
            Assert.That(rot13Called, Is.EqualTo(1 + 1));
        }

        public class Incr
        {
            public int Value { get; set; }
        }

        [Test]
        public void Can_receive_and_process_same_reply_responses()
        {
            var mqHost = CreateMqServer();
            var called = 0;

            mqHost.RegisterHandler<Incr>(m => {
                Debug.WriteLine("In Incr #" + m.GetBody().Value);
                Interlocked.Increment(ref called);
                return m.GetBody().Value > 0 ? new Incr { Value = m.GetBody().Value - 1 } : null;
            });

            mqHost.Start();

            var mqClient = mqHost.CreateMessageQueueClient();

            var incr = new Incr { Value = 5 };
            mqClient.Publish(incr);

            Thread.Sleep(1000);

            Assert.That(called, Is.EqualTo(1 + incr.Value));
        }

        public class Hello { public string Name { get; set; } }
        public class HelloResponse { public string Result { get; set; } }

        [Test]
        public void Can_receive_and_process_standard_request_reply_combo()
        {
            var mqHost = CreateMqServer();

            string messageReceived = null;

            mqHost.RegisterHandler<Hello>(m =>
                new HelloResponse { Result = "Hello, " + m.GetBody().Name });

            mqHost.RegisterHandler<HelloResponse>(m => {
                messageReceived = m.GetBody().Result; return null;
            });

            mqHost.Start();

            var mqClient = mqHost.CreateMessageQueueClient();

            var dto = new Hello { Name = "ServiceStack" };
            mqClient.Publish(dto);

            Thread.Sleep(1000);

            Assert.That(messageReceived, Is.EqualTo("Hello, ServiceStack"));
        }

        [Test]
        public void Can_BlockingPop_from_multiple_queues()
        {
            const int noOf = 5;
            var queueNames = noOf.Times(x => "queue:" + x).ToArray();

            ThreadPool.QueueUserWorkItem(state => {
                Thread.Sleep(100);
                var i = 0;
                var client = RedisClient.New();
                foreach (var queueName in queueNames)
                {
                    var msgName = "msg:" + Interlocked.Increment(ref i);
                    Debug.WriteLine("SEND " + msgName);
                    client.PrependItemToList(queueName, msgName);
                }
            });

            var server = RedisClient.New();
            noOf.Times(x => {
                Debug.WriteLine("Blocking... " + x);
                var result = server.BlockingDequeueItemFromLists(queueNames, TimeSpan.FromSeconds(3));
                Debug.WriteLine("RECV: " + result.Dump());
            });
        }

        public class Wait
        {
            public int ForMs { get; set; }
        }

        [Test]
        public void Can_handle_requests_concurrently_in_2_threads()
        {
            RunHandlerOnMultipleThreads(noOfThreads: 2, msgs: 10);
        }

        [Test]
        public void Can_handle_requests_concurrently_in_3_threads()
        {
            RunHandlerOnMultipleThreads(noOfThreads: 3, msgs: 10);
        }

        [Test]
        public void Can_handle_requests_concurrently_in_4_threads()
        {
            RunHandlerOnMultipleThreads(noOfThreads: 4, msgs: 10);
        }

        private static void RunHandlerOnMultipleThreads(int noOfThreads, int msgs)
        {
            var timesCalled = 0;
            var mqHost = CreateMqServer();
            mqHost.RegisterHandler<Wait>(m => {
                Interlocked.Increment(ref timesCalled);
                Thread.Sleep(m.GetBody().ForMs);
                return null;
            }, noOfThreads);

            mqHost.Start();

            var mqClient = mqHost.CreateMessageQueueClient();

            var dto = new Wait { ForMs = 100 };
            msgs.Times(i => mqClient.Publish(dto));

            const double buffer = 1.1;

            var sleepForMs = (int)((msgs * 100 / (double)noOfThreads) * buffer);
            "Sleeping for {0}ms...".Print(sleepForMs);
            Thread.Sleep(sleepForMs);

            mqHost.Dispose();

            Assert.That(timesCalled, Is.EqualTo(msgs));
        }
    }
}