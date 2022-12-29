using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Messaging.Redis;
using ServiceStack.Redis;

namespace ServiceStack.Server.Tests.Benchmarks
{
    [TestFixture, Category("Benchmarks"), Ignore("Benchmarks")]
	public class RedisMqServerBenchmarks
	{
		public class Incr
		{
			public int Value { get; set; }
		}

        public class IncrBlocking
        {
            public int Value { get; set; }
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

        [Test]
        public void Can_receive_and_process_same_reply_responses()
        {
            var mqHost = CreateMqServer();
            var called = 0;

            mqHost.RegisterHandler<Incr>(m =>
            {
                called++;
                return new Incr { Value = m.GetBody().Value + 1 };
            });

            mqHost.Start();

            var mqClient = mqHost.CreateMessageQueueClient();
            mqClient.Publish(new Incr { Value = 1 });

            Thread.Sleep(10000);

            Debug.WriteLine("Times called: " + called);
        }

        [Test]
        public void Can_receive_and_process_same_reply_responses_blocking()
        {
            var mqHost = CreateMqServer();
            var called = 0;

            mqHost.RegisterHandler<IncrBlocking>(m =>
            {
                called++;
                mqHost.CreateMessageQueueClient().Publish(new IncrBlocking { Value = m.GetBody().Value + 1 });
                Thread.Sleep(100);
                return null;
            });

            mqHost.Start();

            var mqClient = mqHost.CreateMessageQueueClient();
            mqClient.Publish(new IncrBlocking { Value = 1 });

            Thread.Sleep(10000);

            Debug.WriteLine("Times called: " + called);
        }

	}
}