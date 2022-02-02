using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Redis.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace ServiceStack.Redis.Tests.Generic
{
    [Category("Async")]
    public abstract class RedisClientSetTestsBaseAsync<T>
    {
        private const string SetId = "testset";
        private const string SetId2 = "testset2";
        private const string SetId3 = "testset3";
        protected abstract IModelFactory<T> Factory { get; }

        private IRedisClientAsync client;
        private IRedisTypedClientAsync<T> redis;
        private IRedisSetAsync<T> Set;
        private IRedisSetAsync<T> Set2;
        private IRedisSetAsync<T> Set3;

        [SetUp]
        public async Task SetUp()
        {
            if (client is object)
            {
                await client.DisposeAsync();
                client = null;
            }
            client = new RedisClient(TestConfig.SingleHost).ForAsyncOnly();
            await client.FlushAllAsync();

            redis = client.As<T>();

            Set = redis.Sets[SetId];
            Set2 = redis.Sets[SetId2];
            Set3 = redis.Sets[SetId3];
        }

        [Test]
        public async Task Can_AddToSet_and_GetAllFromSet()
        {
            var storeMembers = Factory.CreateList();
            await storeMembers.ForEachAsync(async x => await redis.AddItemToSetAsync(Set, x));

            var members = await redis.GetAllItemsFromSetAsync(Set);
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public async Task Can_RemoveFromSet()
        {
            var storeMembers = Factory.CreateList();

            await storeMembers.ForEachAsync(async x => await redis.AddItemToSetAsync(Set, x));

            await redis.RemoveItemFromSetAsync(Set, Factory.ExistingValue);

            storeMembers.Remove(Factory.ExistingValue);

            var members = await redis.GetAllItemsFromSetAsync(Set);
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public async Task Can_PopFromSet()
        {
            var storeMembers = Factory.CreateList();
            await storeMembers.ForEachAsync(async x => await redis.AddItemToSetAsync(Set, x));

            var member = await redis.PopItemFromSetAsync(Set);

            Assert.That(storeMembers.Contains(member), Is.True);
        }

        [Test]
        public async Task Can_MoveBetweenSets()
        {
            var fromSetMembers = Factory.CreateList();
            var toSetMembers = Factory.CreateList2();

            await fromSetMembers.ForEachAsync(x => redis.AddItemToSetAsync(Set, x));
            await toSetMembers.ForEachAsync(x => redis.AddItemToSetAsync(Set2, x));

            await redis.MoveBetweenSetsAsync(Set, Set2, Factory.ExistingValue);

            fromSetMembers.Remove(Factory.ExistingValue);
            toSetMembers.Add(Factory.ExistingValue);

            var readFromSetId = await redis.GetAllItemsFromSetAsync(Set);
            var readToSetId = await redis.GetAllItemsFromSetAsync(Set2);

            Assert.That(readFromSetId, Is.EquivalentTo(fromSetMembers));
            Assert.That(readToSetId, Is.EquivalentTo(toSetMembers));
        }

        [Test]
        public async Task Can_GetSetCount()
        {
            var storeMembers = Factory.CreateList();
            await storeMembers.ForEachAsync(async x => await redis.AddItemToSetAsync(Set, x));

            var setCount = await redis.GetSetCountAsync(Set);

            Assert.That(setCount, Is.EqualTo(storeMembers.Count));
        }

        [Test]
        public async Task Does_SetContainsValue()
        {
            var storeMembers = Factory.CreateList();
            await storeMembers.ForEachAsync(async x => await redis.AddItemToSetAsync(Set, x));

            Assert.That(await redis.SetContainsItemAsync(Set, Factory.ExistingValue), Is.True);
            Assert.That(await redis.SetContainsItemAsync(Set, Factory.NonExistingValue), Is.False);
        }

        [Test]
        public async Task Can_IntersectBetweenSets()
        {
            var storeMembers = Factory.CreateList();
            var storeMembers2 = Factory.CreateList2();

            storeMembers.Add(storeMembers2.First());
            storeMembers2.Add(storeMembers.First());

            await storeMembers.ForEachAsync(async x => await redis.AddItemToSetAsync(Set, x));
            await storeMembers2.ForEachAsync(x => redis.AddItemToSetAsync(Set2, x));

            var intersectingMembers = await redis.GetIntersectFromSetsAsync(new[] { Set, Set2 });

            var intersect = (await Set.ToListAsync()).Intersect((await Set2.ToListAsync())).ToList();

            Assert.That(intersectingMembers, Is.EquivalentTo(intersect));
        }

        [Test]
        public async Task Can_Store_IntersectBetweenSets()
        {
            var storeMembers = Factory.CreateList();
            var storeMembers2 = Factory.CreateList2();

            await storeMembers.ForEachAsync(async x => await redis.AddItemToSetAsync(Set, x));
            await storeMembers2.ForEachAsync(x => redis.AddItemToSetAsync(Set2, x));

            await redis.StoreIntersectFromSetsAsync(Set3, new[] { Set, Set2 });

            var intersect = (await Set.ToListAsync()).Intersect(await Set2.ToListAsync()).ToList();

            Assert.That(await Set3.ToListAsync(), Is.EquivalentTo(intersect));
        }

        [Test]
        public async Task Can_UnionBetweenSets()
        {
            var storeMembers = Factory.CreateList();
            var storeMembers2 = Factory.CreateList2();

            await storeMembers.ForEachAsync(async x => await redis.AddItemToSetAsync(Set, x));
            await storeMembers2.ForEachAsync(x => redis.AddItemToSetAsync(Set2, x));

            var unionMembers = await redis.GetUnionFromSetsAsync(new[] { Set, Set2 });
            
            var union = (await Set.ToListAsync()).Union(await Set2.ToListAsync()).ToList();

            Assert.That(unionMembers, Is.EquivalentTo(union));
        }

        [Test]
        public async Task Can_Store_UnionBetweenSets()
        {
            var storeMembers = Factory.CreateList();
            var storeMembers2 = Factory.CreateList2();

            await storeMembers.ForEachAsync(async x => await redis.AddItemToSetAsync(Set, x));
            await storeMembers2.ForEachAsync(x => redis.AddItemToSetAsync(Set2, x));

            await redis.StoreUnionFromSetsAsync(Set3, new[] { Set, Set2 });

            var union = (await Set.ToListAsync()).Union((await Set2.ToListAsync())).ToList();

            Assert.That(await Set3.ToListAsync(), Is.EquivalentTo(union));
        }

        [Test]
        public async Task Can_DiffBetweenSets()
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

            await storeMembers.ForEachAsync(async x => await redis.AddItemToSetAsync(Set, x));
            await storeMembers2.ForEachAsync(x => redis.AddItemToSetAsync(Set2, x));
            await storeMembers3.ForEachAsync(x => redis.AddItemToSetAsync(Set3, x));

            var diffMembers = await redis.GetDifferencesFromSetAsync(Set, new[] { Set2, Set3 });

            Assert.That(diffMembers, Is.EquivalentTo(
                new List<T> { Factory.CreateInstance(2), Factory.CreateInstance(3) }));
        }

        [Test]
        public async Task Can_Store_DiffBetweenSets()
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

            await storeMembers.ForEachAsync(async x => await redis.AddItemToSetAsync(Set, x));
            await storeMembers2.ForEachAsync(x => redis.AddItemToSetAsync(Set2, x));
            await storeMembers3.ForEachAsync(x => redis.AddItemToSetAsync(Set3, x));

            var storeSet = redis.Sets["testdiffsetstore"];

            await redis.StoreDifferencesFromSetAsync(storeSet, Set, new[] { Set2, Set3 });

            Assert.That(await storeSet.ToListAsync(), Is.EquivalentTo(
                new List<T> { Factory.CreateInstance(2), Factory.CreateInstance(3) }));

        }

        [Test]
        public async Task Can_GetRandomEntryFromSet()
        {
            var storeMembers = Factory.CreateList();
            await storeMembers.ForEachAsync(async x => await redis.AddItemToSetAsync(Set, x));

            var randomEntry = await redis.GetRandomItemFromSetAsync(Set);

            Assert.That(storeMembers.Contains(randomEntry), Is.True);
        }


        [Test]
        public async Task Can_enumerate_small_ICollection_Set()
        {
            var storeMembers = Factory.CreateList();
            await storeMembers.ForEachAsync(async x => await redis.AddItemToSetAsync(Set, x));

            var members = new List<T>();
            await foreach (var item in Set)
            {
                members.Add(item);
            }

            Assert.That(members.Count, Is.EqualTo(storeMembers.Count));
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public async Task Can_enumerate_large_ICollection_Set()
        {
            if (TestConfig.IgnoreLongTests) return;

            const int setSize = 2500;

            var storeMembers = new List<T>();
            await setSize.TimesAsync(async x =>
            {
                await redis.AddItemToSetAsync(Set, Factory.CreateInstance(x));
                storeMembers.Add(Factory.CreateInstance(x));
            });

            var members = new List<T>();
            await foreach (var item in Set)
            {
                members.Add(item);
            }
            members.Sort((x, y) => x.GetId().ToString().CompareTo(y.GetId().ToString()));
            Assert.That(members.Count, Is.EqualTo(storeMembers.Count));
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public async Task Can_Add_to_ICollection_Set()
        {
            var storeMembers = Factory.CreateList();
            await storeMembers.ForEachAsync(async x => await redis.AddItemToSetAsync(Set, x));

            var members = await Set.ToListAsync();
            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

        [Test]
        public async Task Can_Clear_ICollection_Set()
        {
            var storeMembers = Factory.CreateList();
            await storeMembers.ForEachAsync(async x => await redis.AddItemToSetAsync(Set, x));

            Assert.That(await Set.CountAsync(), Is.EqualTo(storeMembers.Count));

            await Set.ClearAsync();

            Assert.That(await Set.CountAsync(), Is.EqualTo(0));
        }

        [Test]
        public async Task Can_Test_Contains_in_ICollection_Set()
        {
            var storeMembers = Factory.CreateList();
            await storeMembers.ForEachAsync(async x => await redis.AddItemToSetAsync(Set, x));

            Assert.That(await Set.ContainsAsync(Factory.ExistingValue), Is.True);
            Assert.That(await Set.ContainsAsync(Factory.NonExistingValue), Is.False);
        }

        [Test]
        public async Task Can_Remove_value_from_ICollection_Set()
        {
            var storeMembers = Factory.CreateList();
            await storeMembers.ForEachAsync(async x => await redis.AddItemToSetAsync(Set, x));

            storeMembers.Remove(Factory.ExistingValue);
            await Set.RemoveAsync(Factory.ExistingValue);

            var members = await Set.ToListAsync();

            Assert.That(members, Is.EquivalentTo(storeMembers));
        }

    }

}