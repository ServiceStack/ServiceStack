using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class RedisClientSetTestsAsync
        : RedisClientTestsBaseAsync
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
            RedisRaw.NamespacePrefix = "RedisClientSetTests";
            storeMembers = new List<string> { "one", "two", "three", "four" };
        }

        [Test]
        public async Task Can_AddToSet_and_GetAllFromSet()
        {
            await storeMembers.ForEachAsync(x => RedisAsync.AddItemToSetAsync(SetId, x));

            var members = await RedisAsync.GetAllItemsFromSetAsync(SetId);
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public async Task Can_AddRangeToSet_and_GetAllFromSet()
        {
            await RedisAsync.AddRangeToSetAsync(SetId, storeMembers);

            var members = await RedisAsync.GetAllItemsFromSetAsync(SetId);
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public async Task Can_RemoveFromSet()
        {
            const string removeMember = "two";

            await storeMembers.ForEachAsync(x => RedisAsync.AddItemToSetAsync(SetId, x));

            await RedisAsync.RemoveItemFromSetAsync(SetId, removeMember);

            storeMembers.Remove(removeMember);

            var members = await RedisAsync.GetAllItemsFromSetAsync(SetId);
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public async Task Can_PopFromSet()
        {
            await storeMembers.ForEachAsync(x => RedisAsync.AddItemToSetAsync(SetId, x));

            var member = await RedisAsync.PopItemFromSetAsync(SetId);

            Assert.That(storeMembers.Contains(member), Is.True);
        }

        [Test]
        public async Task Can_MoveBetweenSets()
        {
            string fromSetId = PrefixedKey("testmovefromset");
            string toSetId = PrefixedKey("testmovetoset");
            const string moveMember = "four";
            var fromSetIdMembers = new List<string> { "one", "two", "three", "four" };
            var toSetIdMembers = new List<string> { "five", "six", "seven" };

            await fromSetIdMembers.ForEachAsync(x => RedisAsync.AddItemToSetAsync(fromSetId, x));
            await toSetIdMembers.ForEachAsync(x => RedisAsync.AddItemToSetAsync(toSetId, x));

            await RedisAsync.MoveBetweenSetsAsync(fromSetId, toSetId, moveMember);

            fromSetIdMembers.Remove(moveMember);
            toSetIdMembers.Add(moveMember);

            var readFromSetId = await RedisAsync.GetAllItemsFromSetAsync(fromSetId);
            var readToSetId = await RedisAsync.GetAllItemsFromSetAsync(toSetId);

            Assert.That(readFromSetId, Is.EquivalentTo(fromSetIdMembers));
            Assert.That(readToSetId, Is.EquivalentTo(toSetIdMembers));
        }

        [Test]
        public async Task Can_GetSetCount()
        {
            await storeMembers.ForEachAsync(x => RedisAsync.AddItemToSetAsync(SetId, x));

            var setCount = await RedisAsync.GetSetCountAsync(SetId);

            Assert.That(setCount, Is.EqualTo(storeMembers.Count));
        }

        [Test]
        public async Task Does_SetContainsValue()
        {
            const string existingMember = "two";
            const string nonExistingMember = "five";

            await storeMembers.ForEachAsync(x => RedisAsync.AddItemToSetAsync(SetId, x));

            Assert.That(await RedisAsync.SetContainsItemAsync(SetId, existingMember), Is.True);
            Assert.That(await RedisAsync.SetContainsItemAsync(SetId, nonExistingMember), Is.False);
        }

        [Test]
        public async Task Can_IntersectBetweenSets()
        {
            string set1Name = PrefixedKey("testintersectset1");
            string set2Name = PrefixedKey("testintersectset2");
            var set1Members = new List<string> { "one", "two", "three", "four", "five" };
            var set2Members = new List<string> { "four", "five", "six", "seven" };

            await set1Members.ForEachAsync(x => RedisAsync.AddItemToSetAsync(set1Name, x));
            await set2Members.ForEachAsync(x => RedisAsync.AddItemToSetAsync(set2Name, x));

            var intersectingMembers = await RedisAsync.GetIntersectFromSetsAsync(new[] { set1Name, set2Name });

            Assert.That(intersectingMembers, Is.EquivalentTo(new List<string> { "four", "five" }));
        }

        [Test]
        public async Task Can_Store_IntersectBetweenSets()
        {
            string set1Name = PrefixedKey("testintersectset1");
            string set2Name = PrefixedKey("testintersectset2");
            string storeSetName = PrefixedKey("testintersectsetstore");
            var set1Members = new List<string> { "one", "two", "three", "four", "five" };
            var set2Members = new List<string> { "four", "five", "six", "seven" };

            await set1Members.ForEachAsync(x => RedisAsync.AddItemToSetAsync(set1Name, x));
            await set2Members.ForEachAsync(x => RedisAsync.AddItemToSetAsync(set2Name, x));

            await RedisAsync.StoreIntersectFromSetsAsync(storeSetName, new[] { set1Name, set2Name });

            var intersectingMembers = await RedisAsync.GetAllItemsFromSetAsync(storeSetName);

            Assert.That(intersectingMembers, Is.EquivalentTo(new List<string> { "four", "five" }));
        }

        [Test]
        public async Task Can_UnionBetweenSets()
        {
            string set1Name = PrefixedKey("testunionset1");
            string set2Name = PrefixedKey("testunionset2");
            var set1Members = new List<string> { "one", "two", "three", "four", "five" };
            var set2Members = new List<string> { "four", "five", "six", "seven" };

            await set1Members.ForEachAsync(x => RedisAsync.AddItemToSetAsync(set1Name, x));
            await set2Members.ForEachAsync(x => RedisAsync.AddItemToSetAsync(set2Name, x));

            var unionMembers = await RedisAsync.GetUnionFromSetsAsync(new[] { set1Name, set2Name });

            Assert.That(unionMembers, Is.EquivalentTo(
                new List<string> { "one", "two", "three", "four", "five", "six", "seven" }));
        }

        [Test]
        public async Task Can_Store_UnionBetweenSets()
        {
            string set1Name = PrefixedKey("testunionset1");
            string set2Name = PrefixedKey("testunionset2");
            string storeSetName = PrefixedKey("testunionsetstore");
            var set1Members = new List<string> { "one", "two", "three", "four", "five" };
            var set2Members = new List<string> { "four", "five", "six", "seven" };

            await set1Members.ForEachAsync(x => RedisAsync.AddItemToSetAsync(set1Name, x));
            await set2Members.ForEachAsync(x => RedisAsync.AddItemToSetAsync(set2Name, x));

            await RedisAsync.StoreUnionFromSetsAsync(storeSetName, new[] { set1Name, set2Name });

            var unionMembers = await RedisAsync.GetAllItemsFromSetAsync(storeSetName);

            Assert.That(unionMembers, Is.EquivalentTo(
                new List<string> { "one", "two", "three", "four", "five", "six", "seven" }));
        }

        [Test]
        public async Task Can_DiffBetweenSets()
        {
            string set1Name = PrefixedKey("testdiffset1");
            string set2Name = PrefixedKey("testdiffset2");
            string set3Name = PrefixedKey("testdiffset3");
            var set1Members = new List<string> { "one", "two", "three", "four", "five" };
            var set2Members = new List<string> { "four", "five", "six", "seven" };
            var set3Members = new List<string> { "one", "five", "seven", "eleven" };

            await set1Members.ForEachAsync(x => RedisAsync.AddItemToSetAsync(set1Name, x));
            await set2Members.ForEachAsync(x => RedisAsync.AddItemToSetAsync(set2Name, x));
            await set3Members.ForEachAsync(x => RedisAsync.AddItemToSetAsync(set3Name, x));

            var diffMembers = await RedisAsync.GetDifferencesFromSetAsync(set1Name, new[] { set2Name, set3Name });

            Assert.That(diffMembers, Is.EquivalentTo(
                new List<string> { "two", "three" }));
        }

        [Test]
        public async Task Can_Store_DiffBetweenSets()
        {
            string set1Name = PrefixedKey("testdiffset1");
            string set2Name = PrefixedKey("testdiffset2");
            string set3Name = PrefixedKey("testdiffset3");
            string storeSetName = PrefixedKey("testdiffsetstore");
            var set1Members = new List<string> { "one", "two", "three", "four", "five" };
            var set2Members = new List<string> { "four", "five", "six", "seven" };
            var set3Members = new List<string> { "one", "five", "seven", "eleven" };

            await set1Members.ForEachAsync(x => RedisAsync.AddItemToSetAsync(set1Name, x));
            await set2Members.ForEachAsync(x => RedisAsync.AddItemToSetAsync(set2Name, x));
            await set3Members.ForEachAsync(x => RedisAsync.AddItemToSetAsync(set3Name, x));

            await RedisAsync.StoreDifferencesFromSetAsync(storeSetName, set1Name, new[] { set2Name, set3Name });

            var diffMembers = await RedisAsync.GetAllItemsFromSetAsync(storeSetName);

            Assert.That(diffMembers, Is.EquivalentTo(
                new List<string> { "two", "three" }));
        }

        [Test]
        public async Task Can_GetRandomEntryFromSet()
        {
            await storeMembers.ForEachAsync(x => RedisAsync.AddItemToSetAsync(SetId, x));

            var randomEntry = await RedisAsync.GetRandomItemFromSetAsync(SetId);

            Assert.That(storeMembers.Contains(randomEntry), Is.True);
        }


        [Test]
        public async Task Can_enumerate_small_ICollection_Set()
        {
            await storeMembers.ForEachAsync(x => RedisAsync.AddItemToSetAsync(SetId, x));

            var members = new List<string>();
            await foreach (var item in RedisAsync.Sets[SetId])
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
                await RedisAsync.AddItemToSetAsync(SetId, x.ToString());
                storeMembers.Add(x.ToString());
            });

            var members = new List<string>();
            await foreach (var item in RedisAsync.Sets[SetId])
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
            var list = RedisAsync.Sets[SetId];
            await storeMembers.ForEachAsync(x => list.AddAsync(x));

            var members = await list.ToListAsync();
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public async Task Can_Clear_ICollection_Set()
        {
            var list = RedisAsync.Sets[SetId];
            await storeMembers.ForEachAsync(x => list.AddAsync(x));

            Assert.That(await list.CountAsync(), Is.EqualTo(storeMembers.Count));

            await list.ClearAsync();

            Assert.That(await list.CountAsync(), Is.EqualTo(0));
        }

        [Test]
        public async Task Can_Test_Contains_in_ICollection_Set()
        {
            var list = RedisAsync.Sets[SetId];
            await storeMembers.ForEachAsync(x => list.AddAsync(x));

            Assert.That(await list.ContainsAsync("two"), Is.True);
            Assert.That(await list.ContainsAsync("five"), Is.False);
        }

        [Test]
        public async Task Can_Remove_value_from_ICollection_Set()
        {
            var list = RedisAsync.Sets[SetId];
            await storeMembers.ForEachAsync(x => list.AddAsync(x));

            storeMembers.Remove("two");
            await list.RemoveAsync("two");

            var members = await list.ToListAsync();

            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

    }

}