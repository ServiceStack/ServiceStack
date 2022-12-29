using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [TestFixture, Category("Integration")]
    public class RedisClientSortedSetTests
        : RedisClientTestsBase
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
            Redis.NamespacePrefix = "RedisClientSortedSetTests";
            storeMembers = new List<string> { "one", "two", "three", "four" };

            stringDoubleMap = new Dictionary<string, double> {
                 {"one",1}, {"two",2}, {"three",3}, {"four",4}
             };
        }

        [Test]
        public void Can_AddItemToSortedSet_and_GetAllFromSet()
        {
            var i = 0;
            storeMembers.ForEach(x => Redis.AddItemToSortedSet(SetId, x, i++));

            var members = Redis.GetAllItemsFromSortedSet(SetId);
            Assert.That(members.EquivalentTo(storeMembers), Is.True);
        }

        [Test]
        public void Can_AddRangeToSortedSet_and_GetAllFromSet()
        {
            var success = Redis.AddRangeToSortedSet(SetId, storeMembers, 1);
            Assert.That(success, Is.True);

            var members = Redis.GetAllItemsFromSortedSet(SetId);
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public void AddToSet_without_score_adds_an_implicit_lexical_order_score()
        {
            storeMembers.ForEach(x => Redis.AddItemToSortedSet(SetId, x));

            var members = Redis.GetAllItemsFromSortedSet(SetId);

            storeMembers.Sort((x, y) => x.CompareTo(y));
            Assert.That(members.EquivalentTo(storeMembers), Is.True);
        }

        [Test]
        public void AddToSet_with_same_score_is_still_returned_in_lexical_order_score()
        {
            storeMembers.ForEach(x => Redis.AddItemToSortedSet(SetId, x, 1));

            var members = Redis.GetAllItemsFromSortedSet(SetId);

            storeMembers.Sort((x, y) => x.CompareTo(y));
            Assert.That(members.EquivalentTo(storeMembers));
        }

        [Test]
        public void Can_RemoveFromSet()
        {
            const string removeMember = "two";

            storeMembers.ForEach(x => Redis.AddItemToSortedSet(SetId, x));

            Redis.RemoveItemFromSortedSet(SetId, removeMember);

            storeMembers.Remove(removeMember);

            var members = Redis.GetAllItemsFromSortedSet(SetId);
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public void Can_RemoveItemsFromSortedSet()
        {
            var removeMembers = new[] { "two" , "four", "six" };

            storeMembers.ForEach(x => Redis.AddItemToSortedSet(SetId, x));

            var removeCount = Redis.RemoveItemsFromSortedSet(SetId, removeMembers.ToList());
            Assert.That(removeCount, Is.EqualTo(2));

            removeMembers.Each(x => storeMembers.Remove(x));

            var members = Redis.GetAllItemsFromSortedSet(SetId);
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public void Can_PopFromSet()
        {
            var i = 0;
            storeMembers.ForEach(x => Redis.AddItemToSortedSet(SetId, x, i++));

            var member = Redis.PopItemWithHighestScoreFromSortedSet(SetId);

            Assert.That(member, Is.EqualTo("four"));
        }

        [Test]
        public void Can_GetSetCount()
        {
            storeMembers.ForEach(x => Redis.AddItemToSortedSet(SetId, x));

            var setCount = Redis.GetSortedSetCount(SetId);

            Assert.That(setCount, Is.EqualTo(storeMembers.Count));
        }

        [Test]
        public void Can_GetSetCountByScores()
        {
            var scores = new List<double>();

            storeMembers.ForEach(x =>
            {
                Redis.AddItemToSortedSet(SetId, x);
                scores.Add(RedisClient.GetLexicalScore(x));
            });

            Assert.That(Redis.GetSortedSetCount(SetId, scores.Min(), scores.Max()), Is.EqualTo(storeMembers.Count()));
            Assert.That(Redis.GetSortedSetCount(SetId, scores.Min(), scores.Min()), Is.EqualTo(1));
        }

        [Test]
        public void Does_SortedSetContainsValue()
        {
            const string existingMember = "two";
            const string nonExistingMember = "five";

            storeMembers.ForEach(x => Redis.AddItemToSortedSet(SetId, x));

            Assert.That(Redis.SortedSetContainsItem(SetId, existingMember), Is.True);
            Assert.That(Redis.SortedSetContainsItem(SetId, nonExistingMember), Is.False);
        }

        [Test]
        public void Can_GetItemIndexInSortedSet_in_Asc_and_Desc()
        {
            var i = 10;
            storeMembers.ForEach(x => Redis.AddItemToSortedSet(SetId, x, i++));

            Assert.That(Redis.GetItemIndexInSortedSet(SetId, "one"), Is.EqualTo(0));
            Assert.That(Redis.GetItemIndexInSortedSet(SetId, "two"), Is.EqualTo(1));
            Assert.That(Redis.GetItemIndexInSortedSet(SetId, "three"), Is.EqualTo(2));
            Assert.That(Redis.GetItemIndexInSortedSet(SetId, "four"), Is.EqualTo(3));

            Assert.That(Redis.GetItemIndexInSortedSetDesc(SetId, "one"), Is.EqualTo(3));
            Assert.That(Redis.GetItemIndexInSortedSetDesc(SetId, "two"), Is.EqualTo(2));
            Assert.That(Redis.GetItemIndexInSortedSetDesc(SetId, "three"), Is.EqualTo(1));
            Assert.That(Redis.GetItemIndexInSortedSetDesc(SetId, "four"), Is.EqualTo(0));
        }

        [Test]
        public void Can_Store_IntersectBetweenSets()
        {
            string set1Name = PrefixedKey("testintersectset1");
            string set2Name = PrefixedKey("testintersectset2");
            string storeSetName = PrefixedKey("testintersectsetstore");
            var set1Members = new List<string> { "one", "two", "three", "four", "five" };
            var set2Members = new List<string> { "four", "five", "six", "seven" };

            set1Members.ForEach(x => Redis.AddItemToSortedSet(set1Name, x));
            set2Members.ForEach(x => Redis.AddItemToSortedSet(set2Name, x));

            Redis.StoreIntersectFromSortedSets(storeSetName, set1Name, set2Name);

            var intersectingMembers = Redis.GetAllItemsFromSortedSet(storeSetName);

            Assert.That(intersectingMembers, Is.EquivalentTo(new List<string> { "four", "five" }));
        }

        [Test]
        public void Can_Store_UnionBetweenSets()
        {
            string set1Name = PrefixedKey("testunionset1");
            string set2Name = PrefixedKey("testunionset2");
            string storeSetName = PrefixedKey("testunionsetstore");
            var set1Members = new List<string> { "one", "two", "three", "four", "five" };
            var set2Members = new List<string> { "four", "five", "six", "seven" };

            set1Members.ForEach(x => Redis.AddItemToSortedSet(set1Name, x));
            set2Members.ForEach(x => Redis.AddItemToSortedSet(set2Name, x));

            Redis.StoreUnionFromSortedSets(storeSetName, set1Name, set2Name);

            var unionMembers = Redis.GetAllItemsFromSortedSet(storeSetName);

            Assert.That(unionMembers, Is.EquivalentTo(
                new List<string> { "one", "two", "three", "four", "five", "six", "seven" }));
        }

        [Test]
        public void Can_pop_items_with_lowest_and_highest_scores_from_sorted_set()
        {
            storeMembers.ForEach(x => Redis.AddItemToSortedSet(SetId, x));

            storeMembers.Sort((x, y) => x.CompareTo(y));

            var lowestScore = Redis.PopItemWithLowestScoreFromSortedSet(SetId);
            Assert.That(lowestScore, Is.EqualTo(storeMembers.First()));

            var highestScore = Redis.PopItemWithHighestScoreFromSortedSet(SetId);
            Assert.That(highestScore, Is.EqualTo(storeMembers[storeMembers.Count - 1]));
        }

        [Test, Ignore("seems unstable?")]
        public void Can_GetRangeFromSortedSetByLowestScore_from_sorted_set()
        {
            storeMembers.ForEach(x => Redis.AddItemToSortedSet(SetId, x));

            storeMembers.Sort((x, y) => x.CompareTo(y));
            var memberRage = storeMembers.Where(x =>
                x.CompareTo("four") >= 0 && x.CompareTo("three") <= 0).ToList();

            var range = Redis.GetRangeFromSortedSetByLowestScore(SetId, "four", "three");
            Assert.That(range.EquivalentTo(memberRage));
        }

        [Test]
        public void Can_IncrementItemInSortedSet()
        {
            stringDoubleMap.Each(x => Redis.AddItemToSortedSet(SetId, x.Key, x.Value));

            var currentScore = Redis.IncrementItemInSortedSet(SetId, "one", 3);
            stringDoubleMap["one"] = stringDoubleMap["one"] + 3;
            Assert.That(currentScore, Is.EqualTo(stringDoubleMap["one"]));

            currentScore = Redis.IncrementItemInSortedSet(SetId, "four", -3);
            stringDoubleMap["four"] = stringDoubleMap["four"] - 3;
            Assert.That(currentScore, Is.EqualTo(stringDoubleMap["four"]));

            var map = Redis.GetAllWithScoresFromSortedSet(SetId);

            Assert.That(stringDoubleMap.UnorderedEquivalentTo(map));
        }

        [Test]
        public void Can_WorkInSortedSetUnderDifferentCulture()
        {
#if NETCORE
            var prevCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo("ru-RU");
#else
            var prevCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("ru-RU");
#endif
            Redis.AddItemToSortedSet(SetId, "key", 123.22);

            var map = Redis.GetAllWithScoresFromSortedSet(SetId);

            Assert.AreEqual(123.22, map["key"]);

#if NETCORE
            CultureInfo.CurrentCulture = prevCulture;
#else
            Thread.CurrentThread.CurrentCulture = prevCulture;
#endif
        }


        [Ignore("Not implemented yet")]
        [Test]
        public void Can_GetRangeFromSortedSetByHighestScore_from_sorted_set()
        {
            storeMembers.ForEach(x => Redis.AddItemToSortedSet(SetId, x));

            storeMembers.Sort((x, y) => y.CompareTo(x));
            var memberRage = storeMembers.Where(x =>
                x.CompareTo("four") >= 0 && x.CompareTo("three") <= 0).ToList();

            var range = Redis.GetRangeFromSortedSetByHighestScore(SetId, "four", "three");
            Assert.That(range.EquivalentTo(memberRage));
        }

        [Test]
        public void Can_get_index_and_score_from_SortedSet()
        {
            storeMembers = new List<string> { "a", "b", "c", "d" };
            const double initialScore = 10d;
            var i = initialScore;
            storeMembers.ForEach(x => Redis.AddItemToSortedSet(SetId, x, i++));

            Assert.That(Redis.GetItemIndexInSortedSet(SetId, "a"), Is.EqualTo(0));
            Assert.That(Redis.GetItemIndexInSortedSetDesc(SetId, "a"), Is.EqualTo(storeMembers.Count - 1));

            Assert.That(Redis.GetItemScoreInSortedSet(SetId, "a"), Is.EqualTo(initialScore));
            Assert.That(Redis.GetItemScoreInSortedSet(SetId, "d"), Is.EqualTo(initialScore + storeMembers.Count - 1));
        }

        [Test]
        public void Can_enumerate_small_ICollection_Set()
        {
            storeMembers.ForEach(x => Redis.AddItemToSortedSet(SetId, x));

            var members = new List<string>();
            foreach (var item in Redis.SortedSets[SetId])
            {
                members.Add(item);
            }
            members.Sort();
            Assert.That(members.Count, Is.EqualTo(storeMembers.Count));
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public void Can_enumerate_large_ICollection_Set()
        {
            if (TestConfig.IgnoreLongTests) return;

            const int setSize = 2500;

            storeMembers = new List<string>();
            setSize.Times(x =>
            {
                Redis.AddItemToSortedSet(SetId, x.ToString());
                storeMembers.Add(x.ToString());
            });

            var members = new List<string>();
            foreach (var item in Redis.SortedSets[SetId])
            {
                members.Add(item);
            }
            members.Sort((x, y) => int.Parse(x).CompareTo(int.Parse(y)));
            Assert.That(members.Count, Is.EqualTo(storeMembers.Count));
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public void Can_Add_to_ICollection_Set()
        {
            var list = Redis.SortedSets[SetId];
            storeMembers.ForEach(list.Add);

            var members = list.ToList<string>();
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public void Can_Clear_ICollection_Set()
        {
            var list = Redis.SortedSets[SetId];
            storeMembers.ForEach(list.Add);

            Assert.That(list.Count, Is.EqualTo(storeMembers.Count));

            list.Clear();

            Assert.That(list.Count, Is.EqualTo(0));
        }

        [Test]
        public void Can_Test_Contains_in_ICollection_Set()
        {
            var list = Redis.SortedSets[SetId];
            storeMembers.ForEach(list.Add);

            Assert.That(list.Contains("two"), Is.True);
            Assert.That(list.Contains("five"), Is.False);
        }

        [Test]
        public void Can_Remove_value_from_ICollection_Set()
        {
            var list = Redis.SortedSets[SetId];
            storeMembers.ForEach(list.Add);

            storeMembers.Remove("two");
            list.Remove("two");

            var members = list.ToList();

            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public void Score_from_non_existent_item_returns_NaN()
        {
            var score = Redis.GetItemScoreInSortedSet("nonexistentset", "value");

            Assert.That(score, Is.EqualTo(Double.NaN));
        }

        [Test]
        public void Can_add_large_score_to_sortedset()
        {
            Redis.AddItemToSortedSet(SetId, "value", 12345678901234567890d);
            var score = Redis.GetItemScoreInSortedSet(SetId, "value");

            Assert.That(score, Is.EqualTo(12345678901234567890d));
        }

        public class Article
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public DateTime ModifiedDate { get; set; }
        }

        [Test]
        public void Can_use_SortedIndex_to_store_articles_by_Date()
        {
            var redisArticles = Redis.As<Article>();

            var articles = new[]
            {
                new Article { Id = 1, Title = "Article 1", ModifiedDate = new DateTime(2015, 01, 02) },
                new Article { Id = 2, Title = "Article 2", ModifiedDate = new DateTime(2015, 01, 01) },
                new Article { Id = 3, Title = "Article 3", ModifiedDate = new DateTime(2015, 01, 03) },
            };

            redisArticles.StoreAll(articles);

            const string LatestArticlesSet = "urn:Article:modified";

            foreach (var article in articles)
            {
                Redis.AddItemToSortedSet(LatestArticlesSet, article.Id.ToString(), article.ModifiedDate.Ticks);
            }

            var articleIds = Redis.GetAllItemsFromSortedSetDesc(LatestArticlesSet);
            articleIds.PrintDump();

            var latestArticles = redisArticles.GetByIds(articleIds);
            latestArticles.PrintDump();
        }
    }

}