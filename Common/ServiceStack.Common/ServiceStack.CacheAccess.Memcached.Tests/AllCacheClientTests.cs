using NUnit.Framework;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Redis;

namespace ServiceStack.CacheAccess.Memcached.Tests
{
	[TestFixture]
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
			AssertGetAll(new RedisCacheClient(TestConfig.SingleHost));
		}

		[Test]
		public void Memcached_GetAll_returns_missing_keys()
		{
			AssertGetAll(new MemcachedClientCache(TestConfig.MasterHosts));
		}

	}
}