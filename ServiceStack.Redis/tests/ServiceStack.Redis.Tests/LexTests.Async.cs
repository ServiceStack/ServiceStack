using NUnit.Framework;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class LexTestsAsync
        : RedisClientTestsBaseAsync
    {
        readonly string[] values = "a,b,c,d,e,f,g".Split(',');

        [SetUp]
        public async Task SetUp()
        {
            await RedisAsync.FlushAllAsync();
            foreach(var x in values)
            {
                await NativeAsync.ZAddAsync("zset", 0, x.ToUtf8Bytes());
            }
        }

        [Test]
        public async Task Can_ZRangeByLex_all_entries()
        {
            var results = await NativeAsync.ZRangeByLexAsync("zset", "-", "+");

            Assert.That(results.Map(x => x.FromUtf8Bytes()), Is.EquivalentTo(values));

            results = await NativeAsync.ZRangeByLexAsync("zset", "-", "+", 1, 3);
            Assert.That(results.Map(x => x.FromUtf8Bytes()), Is.EquivalentTo(new[] { "b", "c", "d" }));
        }

        [Test]
        public async Task Can_ZRangeByLex_Desc()
        {
            var descInclusive = await NativeAsync.ZRangeByLexAsync("zset", "-", "[c");
            Assert.That(descInclusive.Map(x => x.FromUtf8Bytes()), Is.EquivalentTo(new[] { "a", "b", "c" }));

            var descExclusive = await NativeAsync.ZRangeByLexAsync("zset", "-", "(c");
            Assert.That(descExclusive.Map(x => x.FromUtf8Bytes()), Is.EquivalentTo(new[] { "a", "b" }));
        }

        [Test]
        public async Task Can_ZRangeByLex_Min_and_Max()
        {
            var range = await NativeAsync.ZRangeByLexAsync("zset", "[aaa", "(g");
            Assert.That(range.Map(x => x.FromUtf8Bytes()),
                Is.EquivalentTo(new[] { "b", "c", "d", "e", "f" }));
        }

        [Test]
        public async Task Can_ZlexCount()
        {
            var total = await NativeAsync.ZLexCountAsync("zset", "-", "+");
            Assert.That(total, Is.EqualTo(values.Length));

            Assert.That(await NativeAsync.ZLexCountAsync("zset", "-", "[c"), Is.EqualTo(3));
            Assert.That(await NativeAsync.ZLexCountAsync("zset", "-", "(c"), Is.EqualTo(2));
        }

        [Test]
        public async Task Can_ZRemRangeByLex()
        {
            var removed = await NativeAsync.ZRemRangeByLexAsync("zset", "[aaa", "(g");
            Assert.That(removed, Is.EqualTo(5));

            var remainder = await NativeAsync.ZRangeByLexAsync("zset", "-", "+");
            Assert.That(remainder.Map(x => x.FromUtf8Bytes()), Is.EqualTo(new[] { "a", "g" }));
        }

        [Test]
        public async Task Can_SearchSortedSet()
        {
            Assert.That(await RedisAsync.SearchSortedSetAsync("zset"), Is.EquivalentTo(values));
            Assert.That(await RedisAsync.SearchSortedSetAsync("zset", start: "-"), Is.EquivalentTo(values));
            Assert.That(await RedisAsync.SearchSortedSetAsync("zset", end: "+"), Is.EquivalentTo(values));

            Assert.That((await RedisAsync.SearchSortedSetAsync("zset", start: "[aaa")).Count, Is.EqualTo(values.Length - 1));
            Assert.That((await RedisAsync.SearchSortedSetAsync("zset", end: "(g")).Count, Is.EqualTo(values.Length - 1));
            Assert.That((await RedisAsync.SearchSortedSetAsync("zset", "[aaa", "(g")).Count, Is.EqualTo(values.Length - 2));

            Assert.That((await RedisAsync.SearchSortedSetAsync("zset", "a", "c")).Count, Is.EqualTo(3));
            Assert.That((await RedisAsync.SearchSortedSetAsync("zset", "[a", "[c")).Count, Is.EqualTo(3));
            Assert.That((await RedisAsync.SearchSortedSetAsync("zset", "a", "(c")).Count, Is.EqualTo(2));
            Assert.That((await RedisAsync.SearchSortedSetAsync("zset", "(a", "(c")).Count, Is.EqualTo(1));
        }

        [Test]
        public async Task Can_SearchSortedSetCount()
        {
            Assert.That(await RedisAsync.SearchSortedSetAsync("zset"), Is.EquivalentTo(values));
            Assert.That(await RedisAsync.SearchSortedSetCountAsync("zset", start: "-"), Is.EqualTo(values.Length));
            Assert.That(await RedisAsync.SearchSortedSetCountAsync("zset", end: "+"), Is.EqualTo(values.Length));

            Assert.That(await RedisAsync.SearchSortedSetCountAsync("zset", start: "[aaa"), Is.EqualTo(values.Length - 1));
            Assert.That(await RedisAsync.SearchSortedSetCountAsync("zset", end: "(g"), Is.EqualTo(values.Length - 1));
            Assert.That(await RedisAsync.SearchSortedSetCountAsync("zset", "[aaa", "(g"), Is.EqualTo(values.Length - 2));

            Assert.That(await RedisAsync.SearchSortedSetCountAsync("zset", "a", "c"), Is.EqualTo(3));
            Assert.That(await RedisAsync.SearchSortedSetCountAsync("zset", "[a", "[c"), Is.EqualTo(3));
            Assert.That(await RedisAsync.SearchSortedSetCountAsync("zset", "a", "(c"), Is.EqualTo(2));
            Assert.That(await RedisAsync.SearchSortedSetCountAsync("zset", "(a", "(c"), Is.EqualTo(1));
        }

        [Test]
        public async Task Can_RemoveRangeFromSortedSetBySearch()
        {
            var removed = await RedisAsync.RemoveRangeFromSortedSetBySearchAsync("zset", "[aaa", "(g");
            Assert.That(removed, Is.EqualTo(5));

            var remainder = await RedisAsync.SearchSortedSetAsync("zset");
            Assert.That(remainder, Is.EqualTo(new[] { "a", "g" }));
        }
    }
}