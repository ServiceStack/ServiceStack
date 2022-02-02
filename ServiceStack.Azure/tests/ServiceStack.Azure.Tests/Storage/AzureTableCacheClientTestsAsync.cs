using ServiceStack.Azure.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.Caching;
using ServiceStack.Azure.Storage;
using NUnit.Framework;

namespace ServiceStack.Azure.Tests.Storage
{
    public class AzureTableCacheClientTestsAsync : CacheClientTestsAsyncBase
    {
        public override ICacheClientAsync CreateClient()
        {
            string connStr = "UseDevelopmentStorage=true;";
            return new AzureTableCacheClient(connStr);
        }

        [Test]
        public async Task Can_Increment_In_Parallel()
        {
            var cache = CreateClient();
            int count = 10;
            var fns = count.TimesAsync(async i => 
                await cache.IncrementAsync("concurrent-inc-test", 1));

            await Task.WhenAll(fns);

            var entry = await cache.GetAsync<long>("concurrent-inc-test");
            Assert.That(entry, Is.EqualTo(10));
        }
    }
}
