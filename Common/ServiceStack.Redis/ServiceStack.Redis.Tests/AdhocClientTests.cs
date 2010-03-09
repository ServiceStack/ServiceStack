using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace ServiceStack.Redis.Tests
{
	[TestFixture]
	public class AdhocClientTests
	{
		[Test]
		public void Search_Test()
		{
			using (var client = new RedisClient(TestConfig.SingleHost))
			{
				const string cacheKey = "urn+metadata:All:SearchProProfiles?SwanShinichi Osawa /0/8,0,0,0";
				const long value = 1L;
				client.Set(cacheKey, value);
				var result = client.Get<long>(cacheKey);

				Assert.That(result, Is.EqualTo(value));
			}
		}
	}
}