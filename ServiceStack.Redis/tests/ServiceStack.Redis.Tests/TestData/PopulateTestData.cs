using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.Redis.Tests.TestData
{
    /// <summary>
    /// Simple class to populate redis with some test data
    /// </summary>
    [TestFixture, Category("Integration")]
    public class PopulateTestData
        : RedisClientTestsBase
    {
        const string StringId = "urn:populatetest:string";
        const string ListId = "urn:populatetest:list";
        const string SetId = "urn:populatetest:set";
        const string SortedSetId = "urn:populatetest:zset";
        const string HashId = "urn:populatetest:hash";

        public PopulateTestData()
        {
            CleanMask = "urn:populatetest:*";
        }

        private readonly List<string> items = new List<string> { "one", "two", "three", "four" };
        private readonly Dictionary<string, string> map = new Dictionary<string, string> {
            {"A","one"},
            {"B","two"},
            {"C","three"},
            {"D","four"},
        };

        [Test]
        public void Populate_Strings()
        {
            items.ForEach(x => Redis.Set(StringId + ":" + x, x));
        }

        [Test]
        public void Populate_List()
        {
            items.ForEach(x => Redis.AddItemToList(ListId, x));
        }

        [Test]
        public void Populate_Set()
        {
            items.ForEach(x => Redis.AddItemToSet(SetId, x));
        }

        [Test]
        public void Populate_SortedSet()
        {
            var i = 0;
            items.ForEach(x => Redis.AddItemToSortedSet(SortedSetId, x, i++));
        }

        [Test]
        public void Populate_Hash()
        {
            Redis.SetRangeInHash(HashId, map);
        }
    }
}