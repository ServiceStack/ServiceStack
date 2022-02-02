using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Redis.Generic;
using System.Linq;

namespace ServiceStack.Redis.Tests.Generic
{
    public abstract class RedisClientSetTestsBase<T>
    {
        private const string SetId = "testset";
        private const string SetId2 = "testset2";
        private const string SetId3 = "testset3";
        protected abstract IModelFactory<T> Factory { get; }

        private RedisClient client;
        private IRedisTypedClient<T> redis;
        private IRedisSet<T> Set;
        private IRedisSet<T> Set2;
        private IRedisSet<T> Set3;

        [SetUp]
        public void SetUp()
        {
            if (client != null)
            {
                client.Dispose();
                client = null;
            }
            client = new RedisClient(TestConfig.SingleHost);
            client.FlushAll();

            redis = client.As<T>();

            Set = redis.Sets[SetId];
            Set2 = redis.Sets[SetId2];
            Set3 = redis.Sets[SetId3];
        }

        [Test]
        public void Can_AddToSet_and_GetAllFromSet()
        {
            var storeMembers = Factory.CreateList();
            storeMembers.ForEach(x => redis.AddItemToSet(Set, x));

            var members = redis.GetAllItemsFromSet(Set);
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public void Can_RemoveFromSet()
        {
            var storeMembers = Factory.CreateList();

            storeMembers.ForEach(x => redis.AddItemToSet(Set, x));

            redis.RemoveItemFromSet(Set, Factory.ExistingValue);

            storeMembers.Remove(Factory.ExistingValue);

            var members = redis.GetAllItemsFromSet(Set);
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public void Can_PopFromSet()
        {
            var storeMembers = Factory.CreateList();
            storeMembers.ForEach(x => redis.AddItemToSet(Set, x));

            var member = redis.PopItemFromSet(Set);

            Assert.That(storeMembers.Contains(member), Is.True);
        }

        [Test]
        public void Can_MoveBetweenSets()
        {
            var fromSetMembers = Factory.CreateList();
            var toSetMembers = Factory.CreateList2();

            fromSetMembers.ForEach(x => redis.AddItemToSet(Set, x));
            toSetMembers.ForEach(x => redis.AddItemToSet(Set2, x));

            redis.MoveBetweenSets(Set, Set2, Factory.ExistingValue);

            fromSetMembers.Remove(Factory.ExistingValue);
            toSetMembers.Add(Factory.ExistingValue);

            var readFromSetId = redis.GetAllItemsFromSet(Set);
            var readToSetId = redis.GetAllItemsFromSet(Set2);

            Assert.That(readFromSetId, Is.EquivalentTo(fromSetMembers));
            Assert.That(readToSetId, Is.EquivalentTo(toSetMembers));
        }

        [Test]
        public void Can_GetSetCount()
        {
            var storeMembers = Factory.CreateList();
            storeMembers.ForEach(x => redis.AddItemToSet(Set, x));

            var setCount = redis.GetSetCount(Set);

            Assert.That(setCount, Is.EqualTo(storeMembers.Count));
        }

        [Test]
        public void Does_SetContainsValue()
        {
            var storeMembers = Factory.CreateList();
            storeMembers.ForEach(x => redis.AddItemToSet(Set, x));

            Assert.That(redis.SetContainsItem(Set, Factory.ExistingValue), Is.True);
            Assert.That(redis.SetContainsItem(Set, Factory.NonExistingValue), Is.False);
        }

        [Test]
        public void Can_IntersectBetweenSets()
        {
            var storeMembers = Factory.CreateList();
            var storeMembers2 = Factory.CreateList2();

            storeMembers.Add(storeMembers2.First());
            storeMembers2.Add(storeMembers.First());

            storeMembers.ForEach(x => redis.AddItemToSet(Set, x));
            storeMembers2.ForEach(x => redis.AddItemToSet(Set2, x));

            var intersectingMembers = redis.GetIntersectFromSets(Set, Set2);

            var intersect = Set.ToList().Intersect(Set2.ToList()).ToList();

            Assert.That(intersectingMembers, Is.EquivalentTo(intersect));
        }

        [Test]
        public void Can_Store_IntersectBetweenSets()
        {
            var storeMembers = Factory.CreateList();
            var storeMembers2 = Factory.CreateList2();

            storeMembers.ForEach(x => redis.AddItemToSet(Set, x));
            storeMembers2.ForEach(x => redis.AddItemToSet(Set2, x));

            redis.StoreIntersectFromSets(Set3, Set, Set2);

            var intersect = Set.ToList().Intersect(Set2).ToList();

            Assert.That(Set3, Is.EquivalentTo(intersect));
        }

        [Test]
        public void Can_UnionBetweenSets()
        {
            var storeMembers = Factory.CreateList();
            var storeMembers2 = Factory.CreateList2();

            storeMembers.ForEach(x => redis.AddItemToSet(Set, x));
            storeMembers2.ForEach(x => redis.AddItemToSet(Set2, x));

            var unionMembers = redis.GetUnionFromSets(Set, Set2);

            var union = Set.Union(Set2).ToList();

            Assert.That(unionMembers, Is.EquivalentTo(union));
        }

        [Test]
        public void Can_Store_UnionBetweenSets()
        {
            var storeMembers = Factory.CreateList();
            var storeMembers2 = Factory.CreateList2();

            storeMembers.ForEach(x => redis.AddItemToSet(Set, x));
            storeMembers2.ForEach(x => redis.AddItemToSet(Set2, x));

            redis.StoreUnionFromSets(Set3, Set, Set2);

            var union = Set.ToList().Union(Set2).ToList();

            Assert.That(Set3, Is.EquivalentTo(union));
        }

        [Test]
        public void Can_DiffBetweenSets()
        {
            var storeMembers = Factory.CreateList();
            storeMembers.Add(Factory.CreateInstance(1));

            var storeMembers2 = Factory.CreateList2();
            storeMembers2.Insert(0, Factory.CreateInstance(4));

            var storeMembers3 = new List<T> {
                Factory.CreateInstance(1),
                Factory.CreateInstance(5),
                Factory.CreateInstance(7),
                Factory.CreateInstance(11),
            };

            storeMembers.ForEach(x => redis.AddItemToSet(Set, x));
            storeMembers2.ForEach(x => redis.AddItemToSet(Set2, x));
            storeMembers3.ForEach(x => redis.AddItemToSet(Set3, x));

            var diffMembers = redis.GetDifferencesFromSet(Set, Set2, Set3);

            Assert.That(diffMembers, Is.EquivalentTo(
                new List<T> { Factory.CreateInstance(2), Factory.CreateInstance(3) }));
        }

        [Test]
        public void Can_Store_DiffBetweenSets()
        {
            var storeMembers = Factory.CreateList();
            storeMembers.Add(Factory.CreateInstance(1));

            var storeMembers2 = Factory.CreateList2();
            storeMembers2.Insert(0, Factory.CreateInstance(4));

            var storeMembers3 = new List<T> {
                Factory.CreateInstance(1),
                Factory.CreateInstance(5),
                Factory.CreateInstance(7),
                Factory.CreateInstance(11),
            };

            storeMembers.ForEach(x => redis.AddItemToSet(Set, x));
            storeMembers2.ForEach(x => redis.AddItemToSet(Set2, x));
            storeMembers3.ForEach(x => redis.AddItemToSet(Set3, x));

            var storeSet = redis.Sets["testdiffsetstore"];

            redis.StoreDifferencesFromSet(storeSet, Set, Set2, Set3);

            Assert.That(storeSet, Is.EquivalentTo(
                new List<T> { Factory.CreateInstance(2), Factory.CreateInstance(3) }));

        }

        [Test]
        public void Can_GetRandomEntryFromSet()
        {
            var storeMembers = Factory.CreateList();
            storeMembers.ForEach(x => redis.AddItemToSet(Set, x));

            var randomEntry = redis.GetRandomItemFromSet(Set);

            Assert.That(storeMembers.Contains(randomEntry), Is.True);
        }


        [Test]
        public void Can_enumerate_small_ICollection_Set()
        {
            var storeMembers = Factory.CreateList();
            storeMembers.ForEach(x => redis.AddItemToSet(Set, x));

            var members = new List<T>();
            foreach (var item in Set)
            {
                members.Add(item);
            }

            Assert.That(members.Count, Is.EqualTo(storeMembers.Count));
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public void Can_enumerate_large_ICollection_Set()
        {
            if (TestConfig.IgnoreLongTests) return;

            const int setSize = 2500;

            var storeMembers = new List<T>();
            setSize.Times(x =>
            {
                redis.AddItemToSet(Set, Factory.CreateInstance(x));
                storeMembers.Add(Factory.CreateInstance(x));
            });

            var members = new List<T>();
            foreach (var item in Set)
            {
                members.Add(item);
            }
            members.Sort((x, y) => x.GetId().ToString().CompareTo(y.GetId().ToString()));
            Assert.That(members.Count, Is.EqualTo(storeMembers.Count));
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public void Can_Add_to_ICollection_Set()
        {
            var storeMembers = Factory.CreateList();
            storeMembers.ForEach(x => redis.AddItemToSet(Set, x));

            var members = Set.ToList();
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public void Can_Clear_ICollection_Set()
        {
            var storeMembers = Factory.CreateList();
            storeMembers.ForEach(x => redis.AddItemToSet(Set, x));

            Assert.That(Set.Count, Is.EqualTo(storeMembers.Count));

            Set.Clear();

            Assert.That(Set.Count, Is.EqualTo(0));
        }

        [Test]
        public void Can_Test_Contains_in_ICollection_Set()
        {
            var storeMembers = Factory.CreateList();
            storeMembers.ForEach(x => redis.AddItemToSet(Set, x));

            Assert.That(Set.Contains(Factory.ExistingValue), Is.True);
            Assert.That(Set.Contains(Factory.NonExistingValue), Is.False);
        }

        [Test]
        public void Can_Remove_value_from_ICollection_Set()
        {
            var storeMembers = Factory.CreateList();
            storeMembers.ForEach(x => redis.AddItemToSet(Set, x));

            storeMembers.Remove(Factory.ExistingValue);
            Set.Remove(Factory.ExistingValue);

            var members = Set.ToList();

            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

    }

}