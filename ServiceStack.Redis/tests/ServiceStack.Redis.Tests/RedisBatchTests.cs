using System;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class RedisBatchTests
        : RedisClientTestsBase
    {
        public class Message
        {
            public long Id { get; set; }
            public string Key { get; set; }
            public string Value { get; set; }
            public string Description { get; set; }
        }

        [Test]
        public void Store_batch_items_in_List()
        {
            var redisMessages = Redis.As<Message>();
            const int batchSize = 500;
            var nextIds = redisMessages.GetNextSequence(batchSize);

            var msgBatch = batchSize.Times(i =>
                new Message
                {
                    Id = nextIds - (batchSize - i) + 1,
                    Key = i.ToString(),
                    Value = Guid.NewGuid().ToString(),
                    Description = "Description"
                });

            redisMessages.Lists["listName"].AddRange(msgBatch);

            var msgs = redisMessages.Lists["listName"].GetAll();
            Assert.That(msgs.Count, Is.EqualTo(batchSize));

            Assert.That(msgs.First().Id, Is.EqualTo(1));
            Assert.That(msgs.Last().Id, Is.EqualTo(500));
        }
    }
}