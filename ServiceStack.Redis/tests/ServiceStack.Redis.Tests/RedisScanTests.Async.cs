using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class RedisScanTestsAsync
        : RedisClientTestsBaseAsync
    {
        [Test]
        public async Task Can_scan_10_collection()
        {
            await RedisAsync.FlushAllAsync();
            var keys = 10.Times(x => "KEY" + x);
            await RedisAsync.SetAllAsync(keys.ToSafeDictionary(x => x));

            var ret = await NativeAsync.ScanAsync(0);

            Assert.That(ret.Cursor, Is.GreaterThanOrEqualTo(0));
            Assert.That(ret.AsStrings(), Is.EquivalentTo(keys));
        }

        [Test]
        public async Task Can_scan_100_collection_over_cursor()
        {
            var allKeys = new HashSet<string>();
            await RedisAsync.FlushAllAsync();
            var keys = 100.Times(x => "KEY" + x);
            await RedisAsync.SetAllAsync(keys.ToSafeDictionary(x => x));

            var i = 0;
            var ret = new ScanResult();
            while (true)
            {
                ret = await NativeAsync.ScanAsync(ret.Cursor, 10);
                i++;
                ret.AsStrings().ForEach(x => allKeys.Add(x));
                if (ret.Cursor == 0) break;
            }

            Assert.That(i, Is.GreaterThanOrEqualTo(2));
            Assert.That(allKeys.Count, Is.EqualTo(keys.Count));
            Assert.That(allKeys, Is.EquivalentTo(keys));
        }

        [Test]
        public async Task Can_scan_and_search_10_collection()
        {
            await RedisAsync.FlushAllAsync();
            var keys = 11.Times(x => "KEY" + x);
            await RedisAsync.SetAllAsync(keys.ToSafeDictionary(x => x));

            var ret = await NativeAsync.ScanAsync(0, 11, match: "KEY1*");

            Assert.That(ret.Cursor, Is.GreaterThanOrEqualTo(0));
            Assert.That(ret.AsStrings(), Is.EquivalentTo(new[] { "KEY1", "KEY10" }));
        }

        [Test]
        public async Task Can_SScan_10_sets()
        {
            await RedisAsync.FlushAllAsync();
            var items = 10.Times(x => "item" + x);
            await items.ForEachAsync(async x => await RedisAsync.AddItemToSetAsync("scanset", x));

            var ret = await NativeAsync.SScanAsync("scanset", 0);

            Assert.That(ret.Cursor, Is.GreaterThanOrEqualTo(0));
            Assert.That(ret.AsStrings(), Is.EquivalentTo(items));
        }

        [Test]
        public async Task Can_ZScan_10_sortedsets()
        {
            await RedisAsync.FlushAllAsync();
            var items = 10.Times(x => "item" + x);
            var i = 0;
            await items.ForEachAsync(async x => await RedisAsync.AddItemToSortedSetAsync("scanzset", x, i++));

            var ret = await NativeAsync.ZScanAsync("scanzset", 0);
            var itemsWithScore = ret.AsItemsWithScores();

            Assert.That(itemsWithScore.Keys, Is.EqualTo(items));
            Assert.That(itemsWithScore.Values, Is.EqualTo(10.Times(x => (double)x)));
        }

        [Test]
        public async Task Can_HScan_10_hashes()
        {
            await RedisAsync.FlushAllAsync();
            var values = 10.Times(x => "VALUE" + x);
            await RedisAsync.SetRangeInHashAsync("scanhash", values.ToSafeDictionary(x => x.Replace("VALUE", "KEY")));

            var ret = await NativeAsync.HScanAsync("scanhash", 0);

            var keyValues = ret.AsKeyValues();

            Assert.That(ret.Cursor, Is.GreaterThanOrEqualTo(0));
            Assert.That(keyValues.Keys, Is.EquivalentTo(values.ConvertAll(x => x.Replace("VALUE", "KEY"))));
            Assert.That(keyValues.Values, Is.EquivalentTo(values));
        }

        [Test]
        public async Task Does_lazy_scan_all_keys()
        {
            await RedisAsync.FlushAllAsync();
            var keys = 100.Times(x => "KEY" + x);
            await RedisAsync.SetAllAsync(keys.ToSafeDictionary(x => x));

            var scanAllKeys = RedisAsync.ScanAllKeysAsync(pageSize: 10);
            var tenKeys = await scanAllKeys.TakeAsync(10).ToListAsync();

            Assert.That(tenKeys.Count, Is.EqualTo(10));

            Assert.That(await scanAllKeys.CountAsync(), Is.EqualTo(100));
        }

        [Test]
        public async Task Does_lazy_scan_all_set_items()
        {
            await RedisAsync.FlushAllAsync();
            var items = 100.Times(x => "item" + x);
            await items.ForEachAsync(async x => await RedisAsync.AddItemToSetAsync("scanset", x));

            var scanAllItems = RedisAsync.ScanAllSetItemsAsync("scanset", pageSize: 10);
            var tenKeys = await scanAllItems.TakeAsync(10).ToListAsync();

            Assert.That(tenKeys.Count, Is.EqualTo(10));

            Assert.That(await scanAllItems.CountAsync(), Is.EqualTo(100));
        }

        [Test]
        public async Task Does_lazy_scan_all_sortedset_items()
        {
            await RedisAsync.FlushAllAsync();
            var items = 100.Times(x => "item" + x);
            var i = 0;
            await items.ForEachAsync(async x => await RedisAsync.AddItemToSortedSetAsync("scanzset", x, i++));

            var scanAllItems = RedisAsync.ScanAllSortedSetItemsAsync("scanzset", pageSize: 10);
            var tenKeys = await scanAllItems.TakeAsync(10).ToListAsync();

            Assert.That(tenKeys.Count, Is.EqualTo(10));

            Assert.That(await scanAllItems.CountAsync(), Is.EqualTo(100));

            var map = await scanAllItems.ToDictionaryAsync(x => x.Key, x => x.Value);
            Assert.That(map.Keys, Is.EquivalentTo(items));
        }

        [Test]
        public async Task Does_lazy_scan_all_hash_items()
        {
            await RedisAsync.FlushAllAsync();
            var values = 100.Times(x => "VALUE" + x);
            await RedisAsync.SetRangeInHashAsync("scanhash", values.ToSafeDictionary(x => x.Replace("VALUE", "KEY")));

            var scanAllItems = RedisAsync.ScanAllHashEntriesAsync("scanhash", pageSize: 10);
            var tenKeys = await scanAllItems.TakeAsync(10).ToListAsync();

            Assert.That(tenKeys.Count, Is.EqualTo(10));

            Assert.That(await scanAllItems.CountAsync(), Is.EqualTo(100));

            var map = await scanAllItems.ToDictionaryAsync(x => x.Key, x => x.Value);
            Assert.That(map.Values, Is.EquivalentTo(values));
        }
    }
}