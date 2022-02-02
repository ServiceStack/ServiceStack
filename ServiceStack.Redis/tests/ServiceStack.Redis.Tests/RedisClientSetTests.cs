using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class RedisClientSetTests
        : RedisClientTestsBase
    {
        private const string SetIdSuffix = "testset";
        private List<string> storeMembers;

        private string SetId
        {
            get
            {
                return this.PrefixedKey(SetIdSuffix);
            }
        }

        [SetUp]
        public override void OnBeforeEachTest()
        {
            base.OnBeforeEachTest();
            Redis.NamespacePrefix = "RedisClientSetTests";
            storeMembers = new List<string> { "one", "two", "three", "four" };
        }

        [Test]
        public void Can_AddToSet_and_GetAllFromSet()
        {
            storeMembers.ForEach(x => Redis.AddItemToSet(SetId, x));

            var members = Redis.GetAllItemsFromSet(SetId);
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public void Can_AddRangeToSet_and_GetAllFromSet()
        {
            Redis.AddRangeToSet(SetId, storeMembers);

            var members = Redis.GetAllItemsFromSet(SetId);
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public void Can_RemoveFromSet()
        {
            const string removeMember = "two";

            storeMembers.ForEach(x => Redis.AddItemToSet(SetId, x));

            Redis.RemoveItemFromSet(SetId, removeMember);

            storeMembers.Remove(removeMember);

            var members = Redis.GetAllItemsFromSet(SetId);
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public void Can_PopFromSet()
        {
            storeMembers.ForEach(x => Redis.AddItemToSet(SetId, x));

            var member = Redis.PopItemFromSet(SetId);

            Assert.That(storeMembers.Contains(member), Is.True);
        }

        [Test]
        public void Can_MoveBetweenSets()
        {
            string fromSetId = PrefixedKey("testmovefromset");
            string toSetId = PrefixedKey("testmovetoset");
            const string moveMember = "four";
            var fromSetIdMembers = new List<string> { "one", "two", "three", "four" };
            var toSetIdMembers = new List<string> { "five", "six", "seven" };

            fromSetIdMembers.ForEach(x => Redis.AddItemToSet(fromSetId, x));
            toSetIdMembers.ForEach(x => Redis.AddItemToSet(toSetId, x));

            Redis.MoveBetweenSets(fromSetId, toSetId, moveMember);

            fromSetIdMembers.Remove(moveMember);
            toSetIdMembers.Add(moveMember);

            var readFromSetId = Redis.GetAllItemsFromSet(fromSetId);
            var readToSetId = Redis.GetAllItemsFromSet(toSetId);

            Assert.That(readFromSetId, Is.EquivalentTo(fromSetIdMembers));
            Assert.That(readToSetId, Is.EquivalentTo(toSetIdMembers));
        }

        [Test]
        public void Can_GetSetCount()
        {
            storeMembers.ForEach(x => Redis.AddItemToSet(SetId, x));

            var setCount = Redis.GetSetCount(SetId);

            Assert.That(setCount, Is.EqualTo(storeMembers.Count));
        }

        [Test]
        public void Does_SetContainsValue()
        {
            const string existingMember = "two";
            const string nonExistingMember = "five";

            storeMembers.ForEach(x => Redis.AddItemToSet(SetId, x));

            Assert.That(Redis.SetContainsItem(SetId, existingMember), Is.True);
            Assert.That(Redis.SetContainsItem(SetId, nonExistingMember), Is.False);
        }

        [Test]
        public void Can_IntersectBetweenSets()
        {
            string set1Name = PrefixedKey("testintersectset1");
            string set2Name = PrefixedKey("testintersectset2");
            var set1Members = new List<string> { "one", "two", "three", "four", "five" };
            var set2Members = new List<string> { "four", "five", "six", "seven" };

            set1Members.ForEach(x => Redis.AddItemToSet(set1Name, x));
            set2Members.ForEach(x => Redis.AddItemToSet(set2Name, x));

            var intersectingMembers = Redis.GetIntersectFromSets(set1Name, set2Name);

            Assert.That(intersectingMembers, Is.EquivalentTo(new List<string> { "four", "five" }));
        }

        [Test]
        public void Can_Store_IntersectBetweenSets()
        {
            string set1Name = PrefixedKey("testintersectset1");
            string set2Name = PrefixedKey("testintersectset2");
            string storeSetName = PrefixedKey("testintersectsetstore");
            var set1Members = new List<string> { "one", "two", "three", "four", "five" };
            var set2Members = new List<string> { "four", "five", "six", "seven" };

            set1Members.ForEach(x => Redis.AddItemToSet(set1Name, x));
            set2Members.ForEach(x => Redis.AddItemToSet(set2Name, x));

            Redis.StoreIntersectFromSets(storeSetName, set1Name, set2Name);

            var intersectingMembers = Redis.GetAllItemsFromSet(storeSetName);

            Assert.That(intersectingMembers, Is.EquivalentTo(new List<string> { "four", "five" }));
        }

        [Test]
        public void Can_UnionBetweenSets()
        {
            string set1Name = PrefixedKey("testunionset1");
            string set2Name = PrefixedKey("testunionset2");
            var set1Members = new List<string> { "one", "two", "three", "four", "five" };
            var set2Members = new List<string> { "four", "five", "six", "seven" };

            set1Members.ForEach(x => Redis.AddItemToSet(set1Name, x));
            set2Members.ForEach(x => Redis.AddItemToSet(set2Name, x));

            var unionMembers = Redis.GetUnionFromSets(set1Name, set2Name);

            Assert.That(unionMembers, Is.EquivalentTo(
                new List<string> { "one", "two", "three", "four", "five", "six", "seven" }));
        }

        [Test]
        public void Can_Store_UnionBetweenSets()
        {
            string set1Name = PrefixedKey("testunionset1");
            string set2Name = PrefixedKey("testunionset2");
            string storeSetName = PrefixedKey("testunionsetstore");
            var set1Members = new List<string> { "one", "two", "three", "four", "five" };
            var set2Members = new List<string> { "four", "five", "six", "seven" };

            set1Members.ForEach(x => Redis.AddItemToSet(set1Name, x));
            set2Members.ForEach(x => Redis.AddItemToSet(set2Name, x));

            Redis.StoreUnionFromSets(storeSetName, set1Name, set2Name);

            var unionMembers = Redis.GetAllItemsFromSet(storeSetName);

            Assert.That(unionMembers, Is.EquivalentTo(
                new List<string> { "one", "two", "three", "four", "five", "six", "seven" }));
        }

        [Test]
        public void Can_DiffBetweenSets()
        {
            string set1Name = PrefixedKey("testdiffset1");
            string set2Name = PrefixedKey("testdiffset2");
            string set3Name = PrefixedKey("testdiffset3");
            var set1Members = new List<string> { "one", "two", "three", "four", "five" };
            var set2Members = new List<string> { "four", "five", "six", "seven" };
            var set3Members = new List<string> { "one", "five", "seven", "eleven" };

            set1Members.ForEach(x => Redis.AddItemToSet(set1Name, x));
            set2Members.ForEach(x => Redis.AddItemToSet(set2Name, x));
            set3Members.ForEach(x => Redis.AddItemToSet(set3Name, x));

            var diffMembers = Redis.GetDifferencesFromSet(set1Name, set2Name, set3Name);

            Assert.That(diffMembers, Is.EquivalentTo(
                new List<string> { "two", "three" }));
        }

        [Test]
        public void Can_Store_DiffBetweenSets()
        {
            string set1Name = PrefixedKey("testdiffset1");
            string set2Name = PrefixedKey("testdiffset2");
            string set3Name = PrefixedKey("testdiffset3");
            string storeSetName = PrefixedKey("testdiffsetstore");
            var set1Members = new List<string> { "one", "two", "three", "four", "five" };
            var set2Members = new List<string> { "four", "five", "six", "seven" };
            var set3Members = new List<string> { "one", "five", "seven", "eleven" };

            set1Members.ForEach(x => Redis.AddItemToSet(set1Name, x));
            set2Members.ForEach(x => Redis.AddItemToSet(set2Name, x));
            set3Members.ForEach(x => Redis.AddItemToSet(set3Name, x));

            Redis.StoreDifferencesFromSet(storeSetName, set1Name, set2Name, set3Name);

            var diffMembers = Redis.GetAllItemsFromSet(storeSetName);

            Assert.That(diffMembers, Is.EquivalentTo(
                new List<string> { "two", "three" }));
        }

        [Test]
        public void Can_GetRandomEntryFromSet()
        {
            storeMembers.ForEach(x => Redis.AddItemToSet(SetId, x));

            var randomEntry = Redis.GetRandomItemFromSet(SetId);

            Assert.That(storeMembers.Contains(randomEntry), Is.True);
        }


        [Test]
        public void Can_enumerate_small_ICollection_Set()
        {
            storeMembers.ForEach(x => Redis.AddItemToSet(SetId, x));

            var members = new List<string>();
            foreach (var item in Redis.Sets[SetId])
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
                Redis.AddItemToSet(SetId, x.ToString());
                storeMembers.Add(x.ToString());
            });

            var members = new List<string>();
            foreach (var item in Redis.Sets[SetId])
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
            var list = Redis.Sets[SetId];
            storeMembers.ForEach(list.Add);

            var members = list.ToList();
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public void Can_Clear_ICollection_Set()
        {
            var list = Redis.Sets[SetId];
            storeMembers.ForEach(list.Add);

            Assert.That(list.Count, Is.EqualTo(storeMembers.Count));

            list.Clear();

            Assert.That(list.Count, Is.EqualTo(0));
        }

        [Test]
        public void Can_Test_Contains_in_ICollection_Set()
        {
            var list = Redis.Sets[SetId];
            storeMembers.ForEach(list.Add);

            Assert.That(list.Contains("two"), Is.True);
            Assert.That(list.Contains("five"), Is.False);
        }

        [Test]
        public void Can_Remove_value_from_ICollection_Set()
        {
            var list = Redis.Sets[SetId];
            storeMembers.ForEach(list.Add);

            storeMembers.Remove("two");
            list.Remove("two");

            var members = list.ToList();

            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

    }

}