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
			using (var redis = new RedisClient())
			{
				redis.Set("key", value);
				var valueBytes = redis.Get("key");
				var valueString = Encoding.UTF8.GetString(valueBytes);

				Assert.That(valueString, Is.EqualTo(value));
			}
		}
	}
}
