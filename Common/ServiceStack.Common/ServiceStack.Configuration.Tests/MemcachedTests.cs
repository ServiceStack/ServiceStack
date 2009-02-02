using NUnit.Framework;
using ServiceStack.CacheAccess.Providers;

namespace ServiceStack.Configuration.Tests
{
	[TestFixture]
	public class MemcachedTests
	{
		[Test]
		public void Create_MemcachedClientCache_with_ipaddress_strings()
		{
			var ipAddresses = new[] {"172.20.0.98", "172.20.0.99"};
			var client = new MemcachedClientCache(ipAddresses);
			Assert.That(client != null);
		}
	}
}