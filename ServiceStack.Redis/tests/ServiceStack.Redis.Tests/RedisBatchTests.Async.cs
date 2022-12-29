using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class RedisBatchTestsAsync
        : RedisClientTestsBaseAsync
    {
        public class Message
        {
            public long Id { get; set; }
            public string Key { get; set; }
            public string Value { get; set; }
            public string Description { get; set; }
        }

        [Test]
        public async Task Store_batch_items_in_List()
        {
            var redisMessages = RedisAsync.As<Message>();
            const int batchSize = 500;
            var nextIds = await redisMessages.GetNextSequenceAsync(batchSize);

            var msgBatch = batchSize.Times(i =>
                new Message
                {
                    Id = nextIds - (batchSize - i) + 1,
                    Key = i.ToString(),
                    Value = Guid.NewGuid().ToString(),
                    Description = "Description"
                });

            await redisMessages.Lists["listName"].AddRangeAsync(msgBatch);

            var msgs = await redisMessages.Lists["listName"].GetAllAsync();
            Assert.That(msgs.Count, Is.EqualTo(batchSize));

            Assert.That(msgs.First().Id, Is.EqualTo(1));
            Assert.That(msgs.Last().Id, Is.EqualTo(500));
        }
    }
}