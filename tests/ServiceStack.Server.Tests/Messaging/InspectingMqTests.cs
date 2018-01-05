using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Messaging.Redis;
using ServiceStack.Redis;
using ServiceStack.Text;
using ServiceStack.Messaging;

namespace ServiceStack.Server.Tests.Messaging
{
	public class MessageType1
	{
		public string Name { get; set; }
	}
	
	public class MessageType2
	{
		public string Name { get; set; }
	}
	
	public class MessageType3
	{
		public string Name { get; set; }
	}

	public class MessageStat
	{
		public string MqName { get; set; }
		public string MqType { get; set; }
		public string MessageType { get; set; }
		public int Count { get; set; }
	}
	
	[TestFixture]
	public class InspectingMqTests
	{
		IMessageService mqService;
		IRedisClientsManager redisManager;

		[OneTimeSetUp]
		public void TestFixtureSetUp()
		{
			redisManager = new BasicRedisClientManager();
			mqService = new RedisMqServer(redisManager, 2, null);

			redisManager.Exec(r => r.FlushAll());

			using (var mqPublisher = mqService.MessageFactory.CreateMessageProducer())
			{
				var i=0;
				mqPublisher.Publish(new MessageType1 { Name = "msg-" + i++ });
				mqPublisher.Publish(new MessageType2 { Name = "msg-" + i++ });
				mqPublisher.Publish(new MessageType2 { Name = "msg-" + i++ });
				mqPublisher.Publish(new MessageType3 { Name = "msg-" + i++ });
				mqPublisher.Publish(new MessageType3 { Name = "msg-" + i++ });
				mqPublisher.Publish(new MessageType3 { Name = "msg-" + i++ });
			}
		}
		
		[OneTimeTearDown]
		public void TestFixtureTearDown()
		{
			mqService.Dispose();
			redisManager.Dispose();
		}

		[Test]
		public void Can_get_RedisMq_stats()
		{
			var redisMqStats = new List<MessageStat>();
			using (var redis = redisManager.GetClient())
			{
				var keys = redis.SearchKeys("mq:*");
				foreach (var key in keys)
				{
					if (redis.GetEntryType(key) != RedisKeyType.List) continue;


					var stat = new MessageStat {
						MqName = key,
					};

					redisMqStats.Add(stat);
				}
			}

		}
	}
}

