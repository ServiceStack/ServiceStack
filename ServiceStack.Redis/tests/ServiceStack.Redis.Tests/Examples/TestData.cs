using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.Redis.Tests.Examples
{
    [TestFixture, Ignore("Integration")]
    public class TestData
        : RedisClientTestsBase
    {
        public class Article
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public DateTime ModifiedDate { get; set; }
        }

        [Test]
        public void Create_test_data_for_all_types()
        {
            AddLists();
            AddSets();
            AddSortedSets();
            AddHashes();
        }

        private void AddLists()
        {
            var storeMembers = new List<string> { "one", "two", "three", "four" };
            storeMembers.ForEach(x => Redis.AddItemToList("testlist", x));
        }

        private void AddSets()
        {
            var storeMembers = new List<string> { "one", "two", "three", "four" };
            storeMembers.ForEach(x => Redis.AddItemToSet("testset", x));
        }

        private void AddHashes()
        {
            var stringMap = new Dictionary<string, string> {
                {"one","a"}, {"two","b"}, {"three","c"}, {"four","d"}
            };
            var stringIntMap = new Dictionary<string, int> {
                {"one",1}, {"two",2}, {"three",3}, {"four",4}
            };

            stringMap.Each(x => Redis.SetEntryInHash("testhash", x.Key, x.Value));

            var hash = Redis.Hashes["testhash"];
            stringIntMap.Each(x => hash.Add(x.Key, x.Value.ToString()));
        }

        private void AddSortedSets()
        {
            var i = 0;
            var storeMembers = new List<string> { "one", "two", "three", "four" };
            storeMembers.ForEach(x => Redis.AddItemToSortedSet("testzset", x, i++));

            var redisArticles = Redis.As<Article>();

            var articles = new[]
            {
                new Article {Id = 1, Title = "Article 1", ModifiedDate = new DateTime(2015, 01, 02)},
                new Article {Id = 2, Title = "Article 2", ModifiedDate = new DateTime(2015, 01, 01)},
                new Article {Id = 3, Title = "Article 3", ModifiedDate = new DateTime(2015, 01, 03)},
            };

            redisArticles.StoreAll(articles);

            const string LatestArticlesSet = "urn:Article:modified";

            foreach (var article in articles)
            {
                Redis.AddItemToSortedSet(LatestArticlesSet, article.Id.ToString(), article.ModifiedDate.Ticks);
            }
        }
    }
}