using NUnit.Framework;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Tests
{
    [TestFixture, Category("Integration")]
    public class RedisClientSortedSetTestsAsync
        : RedisClientTestsBaseAsync
    {
        private const string SetIdSuffix = "testzset";
        private List<string> storeMembers;

        private string SetId
        {
            get
            {
                return PrefixedKey(SetIdSuffix);
            }
        }

        Dictionary<string, double> stringDoubleMap;

        public override void OnBeforeEachTest()
        {
            base.OnBeforeEachTest();
            RedisRaw.NamespacePrefix = "RedisClientSortedSetTests";
            storeMembers = new List<string> { "one", "two", "three", "four" };

            stringDoubleMap = new Dictionary<string, double> {
                 {"one",1}, {"two",2}, {"three",3}, {"four",4}
             };
        }

        [Test]
        public async Task Can_AddItemToSortedSet_and_GetAllFromSet()
        {
            var i = 0;
            await storeMembers.ForEachAsync(async x => await RedisAsync.AddItemToSortedSetAsync(SetId, x, i++));

            var members = await RedisAsync.GetAllItemsFromSortedSetAsync(SetId);
            Assert.That(members.EquivalentTo(storeMembers), Is.True);
        }

        [Test]
        public async Task Can_AddRangeToSortedSet_and_GetAllFromSet()
        {
            var success = await RedisAsync.AddRangeToSortedSetAsync(SetId, storeMembers, 1);
            Assert.That(success, Is.True);

            var members = await RedisAsync.GetAllItemsFromSortedSetAsync(SetId);
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public async Task AddToSet_without_score_adds_an_implicit_lexical_order_score()
        {
            await storeMembers.ForEachAsync(async x => await RedisAsync.AddItemToSortedSetAsync(SetId, x));

            var members = await RedisAsync.GetAllItemsFromSortedSetAsync(SetId);

            storeMembers.Sort((x, y) => x.CompareTo(y));
            Assert.That(members.EquivalentTo(storeMembers), Is.True);
        }

        [Test]
        public async Task AddToSet_with_same_score_is_still_returned_in_lexical_order_score()
        {
            await storeMembers.ForEachAsync(async x => await RedisAsync.AddItemToSortedSetAsync(SetId, x, 1));

            var members = await RedisAsync.GetAllItemsFromSortedSetAsync(SetId);

            storeMembers.Sort((x, y) => x.CompareTo(y));
            Assert.That(members.EquivalentTo(storeMembers));
        }

        [Test]
        public async Task Can_RemoveFromSet()
        {
            const string removeMember = "two";

            await storeMembers.ForEachAsync(async x => await RedisAsync.AddItemToSortedSetAsync(SetId, x));

            await RedisAsync.RemoveItemFromSortedSetAsync(SetId, removeMember);

            storeMembers.Remove(removeMember);

            var members = await RedisAsync.GetAllItemsFromSortedSetAsync(SetId);
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public async Task Can_RemoveItemsFromSortedSet()
        {
            var removeMembers = new[] { "two" , "four", "six" };

            await storeMembers.ForEachAsync(async x => await RedisAsync.AddItemToSortedSetAsync(SetId, x));

            var removeCount = await RedisAsync.RemoveItemsFromSortedSetAsync(SetId, removeMembers.ToList());
            Assert.That(removeCount, Is.EqualTo(2));

            removeMembers.Each(x => storeMembers.Remove(x));

            var members = await RedisAsync.GetAllItemsFromSortedSetAsync(SetId);
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public async Task Can_PopFromSet()
        {
            var i = 0;
            await storeMembers.ForEachAsync(async x => await RedisAsync.AddItemToSortedSetAsync(SetId, x, i++));

            var member = await RedisAsync.PopItemWithHighestScoreFromSortedSetAsync(SetId);

            Assert.That(member, Is.EqualTo("four"));
        }

        [Test]
        public async Task Can_GetSetCount()
        {
            await storeMembers.ForEachAsync(async x => await RedisAsync.AddItemToSortedSetAsync(SetId, x));

            var setCount = await RedisAsync.GetSortedSetCountAsync(SetId);

            Assert.That(setCount, Is.EqualTo(storeMembers.Count));
        }

        [Test]
        public async Task Can_GetSetCountByScores()
        {
            var scores = new List<double>();

            await storeMembers.ForEachAsync(async x =>
            {
                await RedisAsync.AddItemToSortedSetAsync(SetId, x);
                scores.Add(RedisClient.GetLexicalScore(x));
            });

            Assert.That(await RedisAsync.GetSortedSetCountAsync(SetId, scores.Min(), scores.Max()), Is.EqualTo(storeMembers.Count()));
            Assert.That(await RedisAsync.GetSortedSetCountAsync(SetId, scores.Min(), scores.Min()), Is.EqualTo(1));
        }

        [Test]
        public async Task Does_SortedSetContainsValue()
        {
            const string existingMember = "two";
            const string nonExistingMember = "five";

            await storeMembers.ForEachAsync(async x => await RedisAsync.AddItemToSortedSetAsync(SetId, x));

            Assert.That(await RedisAsync.SortedSetContainsItemAsync(SetId, existingMember), Is.True);
            Assert.That(await RedisAsync.SortedSetContainsItemAsync(SetId, nonExistingMember), Is.False);
        }

        [Test]
        public async Task Can_GetItemIndexInSortedSet_in_Asc_and_Desc()
        {
            var i = 10;
            await storeMembers.ForEachAsync(async x => await RedisAsync.AddItemToSortedSetAsync(SetId, x, i++));

            Assert.That(await RedisAsync.GetItemIndexInSortedSetAsync(SetId, "one"), Is.EqualTo(0));
            Assert.That(await RedisAsync.GetItemIndexInSortedSetAsync(SetId, "two"), Is.EqualTo(1));
            Assert.That(await RedisAsync.GetItemIndexInSortedSetAsync(SetId, "three"), Is.EqualTo(2));
            Assert.That(await RedisAsync.GetItemIndexInSortedSetAsync(SetId, "four"), Is.EqualTo(3));

            Assert.That(await RedisAsync.GetItemIndexInSortedSetDescAsync(SetId, "one"), Is.EqualTo(3));
            Assert.That(await RedisAsync.GetItemIndexInSortedSetDescAsync(SetId, "two"), Is.EqualTo(2));
            Assert.That(await RedisAsync.GetItemIndexInSortedSetDescAsync(SetId, "three"), Is.EqualTo(1));
            Assert.That(await RedisAsync.GetItemIndexInSortedSetDescAsync(SetId, "four"), Is.EqualTo(0));
        }

        [Test]
        public async Task Can_Store_IntersectBetweenSets()
        {
            string set1Name = PrefixedKey("testintersectset1");
            string set2Name = PrefixedKey("testintersectset2");
            string storeSetName = PrefixedKey("testintersectsetstore");
            var set1Members = new List<string> { "one", "two", "three", "four", "five" };
            var set2Members = new List<string> { "four", "five", "six", "seven" };

            await set1Members.ForEachAsync(async x => await RedisAsync.AddItemToSortedSetAsync(set1Name, x));
            await set2Members.ForEachAsync(async x => await RedisAsync.AddItemToSortedSetAsync(set2Name, x));

            await RedisAsync.StoreIntersectFromSortedSetsAsync(storeSetName, new[] { set1Name, set2Name });

            var intersectingMembers = await RedisAsync.GetAllItemsFromSortedSetAsync(storeSetName);

            Assert.That(intersectingMembers, Is.EquivalentTo(new List<string> { "four", "five" }));
        }

        [Test]
        public async Task Can_Store_UnionBetweenSets()
        {
            string set1Name = PrefixedKey("testunionset1");
            string set2Name = PrefixedKey("testunionset2");
            string storeSetName = PrefixedKey("testunionsetstore");
            var set1Members = new List<string> { "one", "two", "three", "four", "five" };
            var set2Members = new List<string> { "four", "five", "six", "seven" };

            await set1Members.ForEachAsync(async x => await RedisAsync.AddItemToSortedSetAsync(set1Name, x));
            await set2Members.ForEachAsync(async x => await RedisAsync.AddItemToSortedSetAsync(set2Name, x));

            await RedisAsync.StoreUnionFromSortedSetsAsync(storeSetName, new[] { set1Name, set2Name });

            var unionMembers = await RedisAsync.GetAllItemsFromSortedSetAsync(storeSetName);

            Assert.That(unionMembers, Is.EquivalentTo(
                new List<string> { "one", "two", "three", "four", "five", "six", "seven" }));
        }

        [Test]
        public async Task Can_pop_items_with_lowest_and_highest_scores_from_sorted_set()
        {
            await storeMembers.ForEachAsync(async x => await RedisAsync.AddItemToSortedSetAsync(SetId, x));

            storeMembers.Sort((x, y) => x.CompareTo(y));

            var lowestScore = await RedisAsync.PopItemWithLowestScoreFromSortedSetAsync(SetId);
            Assert.That(lowestScore, Is.EqualTo(storeMembers.First()));

            var highestScore = await RedisAsync.PopItemWithHighestScoreFromSortedSetAsync(SetId);
            Assert.That(highestScore, Is.EqualTo(storeMembers[storeMembers.Count - 1]));
        }

        [Test, Ignore("seems unstable?")]
        public async Task Can_GetRangeFromSortedSetByLowestScore_from_sorted_set()
        {
            await storeMembers.ForEachAsync(async x => await RedisAsync.AddItemToSortedSetAsync(SetId, x));

            storeMembers.Sort((x, y) => x.CompareTo(y));
            var memberRage = storeMembers.Where(x =>
                x.CompareTo("four") >= 0 && x.CompareTo("three") <= 0).ToList();

            var range = await RedisAsync.GetRangeFromSortedSetByLowestScoreAsync(SetId, "four", "three");
            Assert.That(range.EquivalentTo(memberRage));
        }

        [Test]
        public async Task Can_IncrementItemInSortedSet()
        {
            await stringDoubleMap.ForEachAsync(async (k,v) => await RedisAsync.AddItemToSortedSetAsync(SetId, k, v));

            var currentScore = await RedisAsync.IncrementItemInSortedSetAsync(SetId, "one", 3);
            stringDoubleMap["one"] = stringDoubleMap["one"] + 3;
            Assert.That(currentScore, Is.EqualTo(stringDoubleMap["one"]));

            currentScore = await RedisAsync.IncrementItemInSortedSetAsync(SetId, "four", -3);
            stringDoubleMap["four"] = stringDoubleMap["four"] - 3;
            Assert.That(currentScore, Is.EqualTo(stringDoubleMap["four"]));

            var map = await RedisAsync.GetAllWithScoresFromSortedSetAsync(SetId);

            Assert.That(stringDoubleMap.UnorderedEquivalentTo(map));
        }

        [Test]
        public async Task Can_WorkInSortedSetUnderDifferentCulture()
        {
#if NETCORE
            var prevCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo("ru-RU");
#else
            var prevCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("ru-RU");
#endif
            await RedisAsync.AddItemToSortedSetAsync(SetId, "key", 123.22);

            var map = await RedisAsync.GetAllWithScoresFromSortedSetAsync(SetId);

            Assert.AreEqual(123.22, map["key"]);

#if NETCORE
            CultureInfo.CurrentCulture = prevCulture;
#else
            Thread.CurrentThread.CurrentCulture = prevCulture;
#endif
        }


        [Ignore("Not implemented yet")]
        [Test]
        public async Task Can_GetRangeFromSortedSetByHighestScore_from_sorted_set()
        {
            await storeMembers.ForEachAsync(async x => await RedisAsync.AddItemToSortedSetAsync(SetId, x));

            storeMembers.Sort((x, y) => y.CompareTo(x));
            var memberRage = storeMembers.Where(x =>
                x.CompareTo("four") >= 0 && x.CompareTo("three") <= 0).ToList();

            var range = await RedisAsync.GetRangeFromSortedSetByHighestScoreAsync(SetId, "four", "three");
            Assert.That(range.EquivalentTo(memberRage));
        }

        [Test]
        public async Task Can_get_index_and_score_from_SortedSet()
        {
            storeMembers = new List<string> { "a", "b", "c", "d" };
            const double initialScore = 10d;
            var i = initialScore;
            await storeMembers.ForEachAsync(async x => await RedisAsync.AddItemToSortedSetAsync(SetId, x, i++));

            Assert.That(await RedisAsync.GetItemIndexInSortedSetAsync(SetId, "a"), Is.EqualTo(0));
            Assert.That(await RedisAsync.GetItemIndexInSortedSetDescAsync(SetId, "a"), Is.EqualTo(storeMembers.Count - 1));

            Assert.That(await RedisAsync.GetItemScoreInSortedSetAsync(SetId, "a"), Is.EqualTo(initialScore));
            Assert.That(await RedisAsync.GetItemScoreInSortedSetAsync(SetId, "d"), Is.EqualTo(initialScore + storeMembers.Count - 1));
        }

        [Test]
        public async Task Can_enumerate_small_ICollection_Set()
        {
            await storeMembers.ForEachAsync(async x => await RedisAsync.AddItemToSortedSetAsync(SetId, x));

            var members = new List<string>();
            await foreach (var item in RedisAsync.SortedSets[SetId])
            {
                members.Add(item);
            }
            members.Sort();
            Assert.That(members.Count, Is.EqualTo(storeMembers.Count));
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public async Task Can_enumerate_large_ICollection_Set()
        {
            if (TestConfig.IgnoreLongTests) return;

            const int setSize = 2500;

            storeMembers = new List<string>();
            await setSize.TimesAsync(async x =>
            {
                await RedisAsync.AddItemToSortedSetAsync(SetId, x.ToString());
                storeMembers.Add(x.ToString());
            });

            var members = new List<string>();
            await foreach (var item in RedisAsync.SortedSets[SetId])
            {
                members.Add(item);
            }
            members.Sort((x, y) => int.Parse(x).CompareTo(int.Parse(y)));
            Assert.That(members.Count, Is.EqualTo(storeMembers.Count));
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public async Task Can_Add_to_ICollection_Set()
        {
            var list = RedisAsync.SortedSets[SetId];
            await storeMembers.ForEachAsync(async x => await list.AddAsync(x));

            var members = await list.ToListAsync<string>();
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public async Task Can_Clear_ICollection_Set()
        {
            var list = RedisAsync.SortedSets[SetId];
            await storeMembers.ForEachAsync(async x => await list.AddAsync(x));

            Assert.That(await list.CountAsync(), Is.EqualTo(storeMembers.Count));

            await list.ClearAsync();

            Assert.That(await list.CountAsync(), Is.EqualTo(0));
        }

        [Test]
        public async Task Can_Test_Contains_in_ICollection_Set()
        {
            var list = RedisAsync.SortedSets[SetId];
            await storeMembers.ForEachAsync(async x => await list.AddAsync(x));

            Assert.That(await list.ContainsAsync("two"), Is.True);
            Assert.That(await list.ContainsAsync("five"), Is.False);
        }

        [Test]
        public async Task Can_Remove_value_from_ICollection_Set()
        {
            var list = RedisAsync.SortedSets[SetId];
            await storeMembers.ForEachAsync(async x => await list.AddAsync(x));

            storeMembers.Remove("two");
            await list.RemoveAsync("two");

            var members = await list.ToListAsync();

            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public async Task Score_from_non_existent_item_returns_NaN()
        {
            var score = await RedisAsync.GetItemScoreInSortedSetAsync("nonexistentset", "value");

            Assert.That(score, Is.EqualTo(Double.NaN));
        }

        [Test]
        public async Task Can_add_large_score_to_sortedset()
        {
            await RedisAsync.AddItemToSortedSetAsync(SetId, "value", 12345678901234567890d);
            var score = await RedisAsync.GetItemScoreInSortedSetAsync(SetId, "value");

            Assert.That(score, Is.EqualTo(12345678901234567890d));
        }

        public class Article
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public DateTime ModifiedDate { get; set; }
        }

        [Test]
        public async Task Can_use_SortedIndex_to_store_articles_by_Date()
        {
            var redisArticles = RedisAsync.As<Article>();

            var articles = new[]
            {
                new Article { Id = 1, Title = "Article 1", ModifiedDate = new DateTime(2015, 01, 02) },
                new Article { Id = 2, Title = "Article 2", ModifiedDate = new DateTime(2015, 01, 01) },
                new Article { Id = 3, Title = "Article 3", ModifiedDate = new DateTime(2015, 01, 03) },
            };

            await redisArticles.StoreAllAsync(articles);

            const string LatestArticlesSet = "urn:Article:modified";

            foreach (var article in articles)
            {
                await RedisAsync.AddItemToSortedSetAsync(LatestArticlesSet, article.Id.ToString(), article.ModifiedDate.Ticks);
            }

            var articleIds = await RedisAsync.GetAllItemsFromSortedSetDescAsync(LatestArticlesSet);
            articleIds.PrintDump();

            var latestArticles = await redisArticles.GetByIdsAsync(articleIds);
            latestArticles.PrintDump();
        }
    }

}