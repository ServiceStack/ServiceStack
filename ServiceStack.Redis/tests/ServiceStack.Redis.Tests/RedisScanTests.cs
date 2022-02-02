using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class RedisScanTests
        : RedisClientTestsBase
    {
        [Test]
        public void Can_scan_10_collection()
        {
            Redis.FlushAll();
            var keys = 10.Times(x => "KEY" + x);
            Redis.SetAll(keys.ToSafeDictionary(x => x));

            var ret = Redis.Scan(0);

            Assert.That(ret.Cursor, Is.GreaterThanOrEqualTo(0));
            Assert.That(ret.AsStrings(), Is.EquivalentTo(keys));
        }

        [Test]
        public void Can_scan_100_collection_over_cursor()
        {
            var allKeys = new HashSet<string>();
            Redis.FlushAll();
            var keys = 100.Times(x => "KEY" + x);
            Redis.SetAll(keys.ToSafeDictionary(x => x));

            var i = 0;
            var ret = new ScanResult();
            while (true)
            {
                ret = Redis.Scan(ret.Cursor, 10);
                i++;
                ret.AsStrings().ForEach(x => allKeys.Add(x));
                if (ret.Cursor == 0) break;
            }

            Assert.That(i, Is.GreaterThanOrEqualTo(2));
            Assert.That(allKeys.Count, Is.EqualTo(keys.Count));
            Assert.That(allKeys, Is.EquivalentTo(keys));
        }

        [Test]
        public void Can_scan_and_search_10_collection()
        {
            Redis.FlushAll();
            var keys = 11.Times(x => "KEY" + x);
            Redis.SetAll(keys.ToSafeDictionary(x => x));

            var ret = Redis.Scan(0, 11, match: "KEY1*");

            Assert.That(ret.Cursor, Is.GreaterThanOrEqualTo(0));
            Assert.That(ret.AsStrings(), Is.EquivalentTo(new[] { "KEY1", "KEY10" }));
        }

        [Test]
        public void Can_SScan_10_sets()
        {
            Redis.FlushAll();
            var items = 10.Times(x => "item" + x);
            items.ForEach(x => Redis.AddItemToSet("scanset", x));

            var ret = Redis.SScan("scanset", 0);

            Assert.That(ret.Cursor, Is.GreaterThanOrEqualTo(0));
            Assert.That(ret.AsStrings(), Is.EquivalentTo(items));
        }

        [Test]
        public void Can_ZScan_10_sortedsets()
        {
            Redis.FlushAll();
            var items = 10.Times(x => "item" + x);
            var i = 0;
            items.ForEach(x => Redis.AddItemToSortedSet("scanzset", x, i++));

            var ret = Redis.ZScan("scanzset", 0);
            var itemsWithScore = ret.AsItemsWithScores();

            Assert.That(itemsWithScore.Keys, Is.EqualTo(items));
            Assert.That(itemsWithScore.Values, Is.EqualTo(10.Times(x => (double)x)));
        }

        [Test]
        public void Can_HScan_10_hashes()
        {
            Redis.FlushAll();
            var values = 10.Times(x => "VALUE" + x);
            Redis.SetRangeInHash("scanhash", values.ToSafeDictionary(x => x.Replace("VALUE", "KEY")));

            var ret = Redis.HScan("scanhash", 0);

            var keyValues = ret.AsKeyValues();

            Assert.That(ret.Cursor, Is.GreaterThanOrEqualTo(0));
            Assert.That(keyValues.Keys, Is.EquivalentTo(values.ConvertAll(x => x.Replace("VALUE", "KEY"))));
            Assert.That(keyValues.Values, Is.EquivalentTo(values));
        }

        [Test]
        public void Does_lazy_scan_all_keys()
        {
            Redis.FlushAll();
            var keys = 100.Times(x => "KEY" + x);
            Redis.SetAll(keys.ToSafeDictionary(x => x));

            var scanAllKeys = Redis.ScanAllKeys(pageSize: 10);
            var tenKeys = scanAllKeys.Take(10).ToList();

            Assert.That(tenKeys.Count, Is.EqualTo(10));

            Assert.That(scanAllKeys.Count(), Is.EqualTo(100));
        }

        [Test]
        public void Does_lazy_scan_all_set_items()
        {
            Redis.FlushAll();
            var items = 100.Times(x => "item" + x);
            items.ForEach(x => Redis.AddItemToSet("scanset", x));

            var scanAllItems = Redis.ScanAllSetItems("scanset", pageSize: 10);
            var tenKeys = scanAllItems.Take(10).ToList();

            Assert.That(tenKeys.Count, Is.EqualTo(10));

            Assert.That(scanAllItems.Count(), Is.EqualTo(100));
        }

        [Test]
        public void Does_lazy_scan_all_sortedset_items()
        {
            Redis.FlushAll();
            var items = 100.Times(x => "item" + x);
            var i = 0;
            items.ForEach(x => Redis.AddItemToSortedSet("scanzset", x, i++));

            var scanAllItems = Redis.ScanAllSortedSetItems("scanzset", pageSize: 10);
            var tenKeys = scanAllItems.Take(10).ToList();

            Assert.That(tenKeys.Count, Is.EqualTo(10));

            Assert.That(scanAllItems.Count(), Is.EqualTo(100));

            var map = scanAllItems.ToDictionary(x => x.Key, x => x.Value);
            Assert.That(map.Keys, Is.EquivalentTo(items));
        }

        [Test]
        public void Does_lazy_scan_all_hash_items()
        {
            Redis.FlushAll();
            var values = 100.Times(x => "VALUE" + x);
            Redis.SetRangeInHash("scanhash", values.ToSafeDictionary(x => x.Replace("VALUE", "KEY")));

            var scanAllItems = Redis.ScanAllHashEntries("scanhash", pageSize: 10);
            var tenKeys = scanAllItems.Take(10).ToList();

            Assert.That(tenKeys.Count, Is.EqualTo(10));

            Assert.That(scanAllItems.Count(), Is.EqualTo(100));

            var map = scanAllItems.ToDictionary(x => x.Key, x => x.Value);
            Assert.That(map.Values, Is.EquivalentTo(values));
        }
    }
}