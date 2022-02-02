using NUnit.Framework;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class LexTests
        : RedisClientTestsBase
    {
        readonly string[] values = "a,b,c,d,e,f,g".Split(',');

        [SetUp]
        public void SetUp()
        {
            Redis.FlushAll();
            values.Each(x => Redis.ZAdd("zset", 0, x.ToUtf8Bytes()));
        }

        [Test]
        public void Can_ZRangeByLex_all_entries()
        {
            var results = Redis.ZRangeByLex("zset", "-", "+");

            Assert.That(results.Map(x => x.FromUtf8Bytes()), Is.EquivalentTo(values));

            results = Redis.ZRangeByLex("zset", "-", "+", 1, 3);
            Assert.That(results.Map(x => x.FromUtf8Bytes()), Is.EquivalentTo(new[] { "b", "c", "d" }));
        }

        [Test]
        public void Can_ZRangeByLex_Desc()
        {
            var descInclusive = Redis.ZRangeByLex("zset", "-", "[c");
            Assert.That(descInclusive.Map(x => x.FromUtf8Bytes()), Is.EquivalentTo(new[] { "a", "b", "c" }));

            var descExclusive = Redis.ZRangeByLex("zset", "-", "(c");
            Assert.That(descExclusive.Map(x => x.FromUtf8Bytes()), Is.EquivalentTo(new[] { "a", "b" }));
        }

        [Test]
        public void Can_ZRangeByLex_Min_and_Max()
        {
            var range = Redis.ZRangeByLex("zset", "[aaa", "(g");
            Assert.That(range.Map(x => x.FromUtf8Bytes()),
                Is.EquivalentTo(new[] { "b", "c", "d", "e", "f" }));
        }

        [Test]
        public void Can_ZlexCount()
        {
            var total = Redis.ZLexCount("zset", "-", "+");
            Assert.That(total, Is.EqualTo(values.Length));

            Assert.That(Redis.ZLexCount("zset", "-", "[c"), Is.EqualTo(3));
            Assert.That(Redis.ZLexCount("zset", "-", "(c"), Is.EqualTo(2));
        }

        [Test]
        public void Can_ZRemRangeByLex()
        {
            var removed = Redis.ZRemRangeByLex("zset", "[aaa", "(g");
            Assert.That(removed, Is.EqualTo(5));

            var remainder = Redis.ZRangeByLex("zset", "-", "+");
            Assert.That(remainder.Map(x => x.FromUtf8Bytes()), Is.EqualTo(new[] { "a", "g" }));
        }

        [Test]
        public void Can_SearchSortedSet()
        {
            Assert.That(Redis.SearchSortedSet("zset"), Is.EquivalentTo(values));
            Assert.That(Redis.SearchSortedSet("zset", start: "-"), Is.EquivalentTo(values));
            Assert.That(Redis.SearchSortedSet("zset", end: "+"), Is.EquivalentTo(values));

            Assert.That(Redis.SearchSortedSet("zset", start: "[aaa").Count, Is.EqualTo(values.Length - 1));
            Assert.That(Redis.SearchSortedSet("zset", end: "(g").Count, Is.EqualTo(values.Length - 1));
            Assert.That(Redis.SearchSortedSet("zset", "[aaa", "(g").Count, Is.EqualTo(values.Length - 2));

            Assert.That(Redis.SearchSortedSet("zset", "a", "c").Count, Is.EqualTo(3));
            Assert.That(Redis.SearchSortedSet("zset", "[a", "[c").Count, Is.EqualTo(3));
            Assert.That(Redis.SearchSortedSet("zset", "a", "(c").Count, Is.EqualTo(2));
            Assert.That(Redis.SearchSortedSet("zset", "(a", "(c").Count, Is.EqualTo(1));
        }

        [Test]
        public void Can_SearchSortedSetCount()
        {
            Assert.That(Redis.SearchSortedSet("zset"), Is.EquivalentTo(values));
            Assert.That(Redis.SearchSortedSetCount("zset", start: "-"), Is.EqualTo(values.Length));
            Assert.That(Redis.SearchSortedSetCount("zset", end: "+"), Is.EqualTo(values.Length));

            Assert.That(Redis.SearchSortedSetCount("zset", start: "[aaa"), Is.EqualTo(values.Length - 1));
            Assert.That(Redis.SearchSortedSetCount("zset", end: "(g"), Is.EqualTo(values.Length - 1));
            Assert.That(Redis.SearchSortedSetCount("zset", "[aaa", "(g"), Is.EqualTo(values.Length - 2));

            Assert.That(Redis.SearchSortedSetCount("zset", "a", "c"), Is.EqualTo(3));
            Assert.That(Redis.SearchSortedSetCount("zset", "[a", "[c"), Is.EqualTo(3));
            Assert.That(Redis.SearchSortedSetCount("zset", "a", "(c"), Is.EqualTo(2));
            Assert.That(Redis.SearchSortedSetCount("zset", "(a", "(c"), Is.EqualTo(1));
        }

        [Test]
        public void Can_RemoveRangeFromSortedSetBySearch()
        {
            var removed = Redis.RemoveRangeFromSortedSetBySearch("zset", "[aaa", "(g");
            Assert.That(removed, Is.EqualTo(5));

            var remainder = Redis.SearchSortedSet("zset");
            Assert.That(remainder, Is.EqualTo(new[] { "a", "g" }));
        }
    }
}