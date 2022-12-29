using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Redis.Generic;
using ServiceStack.Redis.Tests.Support;
using System;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Tests.Generic
{
    [TestFixture, Category("Async")]
    public class RedisClientListTestExtraAsync
    {
        const string ListId = "testlist";
        // const string ListId2 = "testlist2";
        private IRedisListAsync<CustomType> List;
        // private IRedisListAsync<CustomType> List2;


        private readonly IModelFactory<CustomType> factory = new CustomTypeFactory();

        protected IModelFactory<CustomType> Factory { get { return factory; } }

        private IRedisClientAsync client;
        private IRedisTypedClientAsync<CustomType> redis;

        [SetUp]
        public async Task SetUp()
        {
            if (client is object)
            {
                await client.DisposeAsync();
                client = null;
            }
            client = new RedisClient(TestConfig.SingleHost);
            await client.FlushAllAsync();

            redis = client.As<CustomType>();

            List = redis.Lists[ListId];
            // List2 = redis.Lists[ListId2];
        }

        [Test]
        public async Task Can_Remove_value_from_IList()
        {
            var storeMembers = Factory.CreateList();
            await storeMembers.ForEachAsync(x => List.AddAsync(x));

            var equalItem = new CustomType() { CustomId = 4 };
            storeMembers.Remove(equalItem);
            await List.RemoveAsync(equalItem);

            var members = await List.ToListAsync();

            Factory.AssertListsAreEqual(members, storeMembers);
        }

    }
}
