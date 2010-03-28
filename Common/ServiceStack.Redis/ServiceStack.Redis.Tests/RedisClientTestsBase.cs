using System.Text;
using NUnit.Framework;

namespace ServiceStack.Redis.Tests
{
	[TestFixture]
	public class RedisClientTestsBase
	{
		protected RedisClient Redis;

		[SetUp]
		public virtual void OnBeforeEachTest()
		{
			if (Redis != null) Redis.Dispose();
			Redis = new RedisClient(TestConfig.SingleHost);
			Redis.FlushDb();
		}

		public RedisClient GetRedisClient()
		{
			var client = new RedisClient(TestConfig.SingleHost);
			client.FlushDb();
			return client;
		}

		public string GetString(byte[] stringBytes)
		{
			return Encoding.UTF8.GetString(stringBytes);
		}

		public byte[] GetBytes(string stringValue)
		{
			return Encoding.UTF8.GetBytes(stringValue);
		}
	}
}