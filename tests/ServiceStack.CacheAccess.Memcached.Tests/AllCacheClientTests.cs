using NUnit.Framework;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Redis;

namespace ServiceStack.CacheAccess.Memcached.Tests
{
	[TestFixture]
	[Ignore("Ignoring integration tests that require infracture")]
	public class AllCacheClientTests : AllCacheClientsTestBase
	{
		[Test]
		public void Memory_GetAll_returns_missing_keys()
		{
			AssertGetAll(new MemoryCacheClient());
		}

		[Test]
		public void Redis_GetAll_returns_missing_keys()
		{
			AssertGetAll(new RedisClient(TestConfig.SingleHost));
		}

		[Test]
		public void Memcached_GetAll_returns_missing_keys()
		{
			AssertGetAll(new MemcachedClientCache(TestConfig.MasterHosts));
		}

		[Test]
		public void Memory_GetSetIntValue_returns_missing_keys()
		{
			AssertGetSetIntValue(new MemoryCacheClient());
		}

		[Test]
		public void Redis_GetSetIntValue_returns_missing_keys()
		{
			AssertGetSetIntValue(new RedisClient(TestConfig.SingleHost));
		}

		[Test]
		public void Memcached_GetSetIntValue_returns_missing_keys()
		{
			var client = new MemcachedClientCache(TestConfig.MasterHosts);
			AssertGetSetIntValue((IMemcachedClient)client);
			AssertGetSetIntValue((ICacheClient)client);
		}

	}
}