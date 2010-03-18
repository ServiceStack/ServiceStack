using System;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace ServiceStack.Redis.Tests
{
	[TestFixture]
	public class RedisClientTests
	{
		[Test]
		public void Can_Set_and_Get_string()
		{
			const string value = "value";
			using (var redis = new RedisClient(TestConfig.SingleHost))
			{
				redis.SetString("key", value);
				var valueBytes = redis.Get("key");
				var valueString = Encoding.UTF8.GetString(valueBytes);

				Assert.That(valueString, Is.EqualTo(value));
			}
		}

		[Test]
		public void Can_Set_and_Get_key_with_spaces()
		{
			const string key = "key with spaces";
			const string value = "value";
			using (var redis = new RedisClient(TestConfig.SingleHost))
			{
				redis.SetString(key, value);
				var valueBytes = redis.Get(key);
				var valueString = Encoding.UTF8.GetString(valueBytes);

				Assert.That(valueString, Is.EqualTo(value));
			}
		}

		[Test]
		public void Can_Set_and_Get_key_with_all_byte_values()
		{
			const string key = "bytesKey";
			
			var value = new byte[256];
			for (var i = 0; i < value.Length; i++)
			{
				value[i] = (byte) i;
			}

			using (var redis = new RedisClient(TestConfig.SingleHost))
			{
				redis.Set(key, value);
				var resultValue = redis.Get(key);

				Assert.That(resultValue, Is.EquivalentTo(value));
			}
		}

		[Test]
		public void GetKeys_returns_matching_collection()
		{
			using (var redis = new RedisClient(TestConfig.SingleHost))
			{
				redis.Set("ss-tests:a1", "One");
				redis.Set("ss-tests:a2", "One");
				redis.Set("ss-tests:b3", "One");

				var matchingKeys = redis.GetKeys("ss-tests:a*");

				Assert.That(matchingKeys.Length, Is.EqualTo(2));
			}
		}

		[Test]
		public void GetKeys_on_non_existent_keys_returns_empty_collection()
		{
			using (var redis = new RedisClient(TestConfig.SingleHost))
			{
				var matchingKeys = redis.GetKeys("ss-tests:NOTEXISTS");

				Assert.That(matchingKeys.Length, Is.EqualTo(0));
			}
		}
	}
}
