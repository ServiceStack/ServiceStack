using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Messaging.Redis;
using ServiceStack.Redis;
using ServiceStack.Text;

namespace ServiceStack.Server.Tests.Messaging
{
    [TestFixture, Category("Integration")]
    public class RedisMqServerSpinServerTests
    {
        public class Counters
        {
            public int Spin0;
            public int Spin10;
            public int Spin100;
            public int Spin1000;
        }

        class Spin0
        {
            public int Id { get; set; }
        }
        class Spin10
        {
            public int Id { get; set; }
        }
        class Spin100
        {
            public int Id { get; set; }
        }
        class Spin1000
        {
            public int Id { get; set; }
        }

        readonly Counters counter = new Counters();

        RedisMqServer CreateServer()
        {
            using (var redis = new RedisClient(TestConfig.SingleHost))
                redis.FlushAll();

            var mqServer = new RedisMqServer(TestConfig.BasicClientManger);
            mqServer.RegisterHandler<Spin0>(m => new Spin0 { Id = Interlocked.Increment(ref counter.Spin0) });

            mqServer.RegisterHandler<Spin10>(m => {
                var sw = Stopwatch.StartNew();
                SpinWait.SpinUntil(() => sw.ElapsedMilliseconds < 10);
                return new Spin10 { Id = Interlocked.Increment(ref counter.Spin10) };
            });
            mqServer.RegisterHandler<Spin100>(m => {
                var sw = Stopwatch.StartNew();
                SpinWait.SpinUntil(() => sw.ElapsedMilliseconds < 100);
                return new Spin100 { Id = Interlocked.Increment(ref counter.Spin100) };
            });
            mqServer.RegisterHandler<Spin1000>(m => {
                var sw = Stopwatch.StartNew();
                SpinWait.SpinUntil(() => sw.ElapsedMilliseconds < 1000);
                return new Spin1000 { Id = Interlocked.Increment(ref counter.Spin1000) };
            });


            return mqServer;
        }

        [Test]
        public void Run_for_1_seconds()
        {
            RunFor(TimeSpan.FromSeconds(1));
        }

        [Ignore("Debug Test")]
        [Test]
        public void Run_for_5_seconds()
        {
            RunFor(TimeSpan.FromSeconds(5));
        }

        [Ignore("Debug Test")]
        [Test]
        public void Run_for_10_seconds()
        {
            RunFor(TimeSpan.FromSeconds(10));
        }

        [Ignore("Debug Test")]
        [Test]
        public void Run_for_30_seconds()
        {
            RunFor(TimeSpan.FromSeconds(30));
        }

        private void RunFor(TimeSpan SpinFor)
        {
            var mqServer = CreateServer();

            mqServer.Start();

            using (var mqClient = mqServer.CreateMessageQueueClient())
            {
                mqClient.Publish(new Spin0());
                mqClient.Publish(new Spin10());
                mqClient.Publish(new Spin100());
                mqClient.Publish(new Spin1000());
            }

            Thread.Sleep(SpinFor);

            Debug.WriteLine(counter.Dump());

            Debug.WriteLine("Disposing...");
            mqServer.Dispose();

            Debug.WriteLine(counter.Dump());
        }
    }
}