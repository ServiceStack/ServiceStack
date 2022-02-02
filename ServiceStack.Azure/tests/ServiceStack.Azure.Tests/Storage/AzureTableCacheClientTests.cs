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
    public class AzureTableCacheClientTests : CacheClientTestsBase
    {
        public override ICacheClient CreateClient()
        {
            string connStr = "UseDevelopmentStorage=true;";
            return new AzureTableCacheClient(connStr);
        }

        [Test]
        public void Can_Increment_In_Parallel()
        {
            var cache = CreateClient();
            int count = 10;
            var fns = count.Times(i => (Action)(() =>
            {
                cache.Increment("concurrent-inc-test", 1);
            }));

            Parallel.Invoke(fns.ToArray());

            var entry = cache.Get<long>("concurrent-inc-test");
            Assert.That(entry, Is.EqualTo(10));
        }
    }
}
