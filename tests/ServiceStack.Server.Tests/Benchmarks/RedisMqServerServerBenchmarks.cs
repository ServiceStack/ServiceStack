using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Messaging.Redis;
using ServiceStack.Redis;
using ServiceStack.Text;

namespace ServiceStack.Server.Tests.Benchmarks
{
    [Ignore("Benchmarks")]
    [Explicit]
    [TestFixture, Category("Benchmarks")]
    public class RedisMqServerServerBenchmarks
    {
        public class Incr
        {
            public int Value { get; set; }
        }

        public class IncrBlocking
        {
            public int Value { get; set; }
        }

        private static RedisMqServer CreateMqHostServer()
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
            var mqHost = new RedisMqServer(redisFactory);
            return mqHost;
        }

        [Test]
        public void Can_receive_and_process_same_reply_responses()
        {
            var mqHost = CreateMqHostServer();
            var called = 0;

            mqHost.RegisterHandler<Incr>(m =>
            {
                called++;
                return new Incr { Value = m.GetBody().Value + 1 };
            }, noOfThreads: 3);

            mqHost.Start();

            var mqClient = mqHost.CreateMessageQueueClient();
            mqClient.Publish(new Incr { Value = 1 });

            Thread.Sleep(10000);

            Debug.WriteLine("Times called: " + called);
        }

        [Test]
        public void Can_receive_and_process_same_reply_responses_blocking()
        {
            var mqHost = CreateMqHostServer();
            var called = 0;

            mqHost.RegisterHandler<IncrBlocking>(m =>
            {
                called++;
                mqHost.CreateMessageQueueClient().Publish(new IncrBlocking { Value = m.GetBody().Value + 1 });
                Thread.Sleep(100);
                return null;
            }, noOfThreads:5);

            mqHost.Start();

            var mqClient = mqHost.CreateMessageQueueClient();
            mqClient.Publish(new IncrBlocking { Value = 1 });

            Thread.Sleep(10000);

            Debug.WriteLine("Times called: " + called);
        }

        [Test]
        public void Can_receive_and_process_same_reply_responses_blocking_and_non_blocking()
        {
            var mqHost = CreateMqHostServer();
            var nonBlocking = 0;
            var blocking = 0;

            mqHost.RegisterHandler<Incr>(m =>
            {
                nonBlocking++;
                return new Incr { Value = m.GetBody().Value + 1 };
            }, 1); //Non-blocking less no of threads the better

            mqHost.RegisterHandler<IncrBlocking>(m =>
            {
                blocking++;
                mqHost.CreateMessageQueueClient().Publish(new IncrBlocking { Value = m.GetBody().Value + 1 });
                Thread.Sleep(100);
                return null;
            }, 5); //Blocking, more threads == better

            mqHost.Start();

            var mqClient = mqHost.CreateMessageQueueClient();
            mqClient.Publish(new Incr { Value = 1 });
            mqClient.Publish(new IncrBlocking { Value = 1 });

            Thread.Sleep(10000);

            Debug.WriteLine("Times called: non-blocking: {0}, blocking: {1}".Fmt(nonBlocking, blocking));
        }

        [Test]
        public void Test_Blocking_messages_throughput()
        {
            var mqHost = CreateMqHostServer();
            var blocking = 0;
            const int BlockFor = 100;
            const int NoOfThreads = 5;
            const int SendEvery = BlockFor / NoOfThreads / 4;

            mqHost.RegisterHandler<IncrBlocking>(m =>
            {
                blocking++;
                Thread.Sleep(BlockFor);
                return null;
            }, NoOfThreads);

            mqHost.Start();

            var startedAt = DateTime.Now;
            var mqClient = mqHost.CreateMessageQueueClient();
            while (DateTime.Now - startedAt < TimeSpan.FromSeconds(10))
            {
                mqClient.Publish(new IncrBlocking { Value = 1 });
                Thread.Sleep(SendEvery);
            }

            Debug.WriteLine("Times called: blocking: {0}".Fmt(blocking));
        }

        [Test]
        public void Test_Blocking_and_NonBlocking_messages_throughput()
        {
            var mqHost = CreateMqHostServer();
            var nonBlocking = 0;
            var blocking = 0;
            const int BlockFor = 100;
            const int NoOfThreads = 5;
            const int SendBlockingMsgEvery = BlockFor / NoOfThreads / 4;

            mqHost.RegisterHandler<Incr>(m =>
            {
                nonBlocking++;
                return null;
            }, 3);

            mqHost.RegisterHandler<IncrBlocking>(m =>
            {
                blocking++;
                Thread.Sleep(BlockFor);
                return null;
            }, NoOfThreads);

            mqHost.Start();

            var mqClient = mqHost.CreateMessageQueueClient();

            var stopWatch = Stopwatch.StartNew();
            long lastBlockingSentAtMs = 0;

            while (stopWatch.ElapsedMilliseconds < 10 * 1000)
            {
                mqClient.Publish(new Incr { Value = 1 });
                while (stopWatch.ElapsedMilliseconds - lastBlockingSentAtMs > SendBlockingMsgEvery)
                {
                    mqClient.Publish(new IncrBlocking { Value = 1 });
                    lastBlockingSentAtMs = stopWatch.ElapsedMilliseconds;
                }
            }

            Debug.WriteLine("Times called: non-blocking: {0}, blocking: {1}".Fmt(nonBlocking, blocking));
        }

    }
}