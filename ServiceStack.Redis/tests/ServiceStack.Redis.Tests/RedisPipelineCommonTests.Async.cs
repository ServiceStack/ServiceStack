using NUnit.Framework;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class RedisPipelineCommonTestsAsync
        : RedisClientTestsBaseAsync
    {
        [Test]
        public async Task Can_Set_and_Expire_key_in_atomic_transaction()
        {
            var oneSec = TimeSpan.FromSeconds(1);

            Assert.That(await RedisAsync.GetValueAsync("key"), Is.Null);
            await using (var trans = RedisAsync.CreatePipeline())              //Calls 'MULTI'
            {
                trans.QueueCommand(r => r.SetValueAsync("key", "a"));      //Queues 'SET key a'
                trans.QueueCommand(r => r.ExpireEntryInAsync("key", oneSec)); //Queues 'EXPIRE key 1'

                await trans.FlushAsync();                                        //Calls 'EXEC'

            }                                                          //Calls 'DISCARD' if 'EXEC' wasn't called

            Assert.That(await RedisAsync.GetValueAsync("key"), Is.EqualTo("a"));
            await Task.Delay(TimeSpan.FromSeconds(2));
            Assert.That(await RedisAsync.GetValueAsync("key"), Is.Null);
        }

        [Test]
        public async Task Can_SetAll_and_Publish_in_atomic_transaction()
        {
            var messages = new Dictionary<string, string> { { "a", "a" }, { "b", "b" } };
            await using var pipeline = RedisAsync.CreatePipeline();
            pipeline.QueueCommand(c => c.SetAllAsync(messages.ToDictionary(t => t.Key, t => t.Value)));
            pipeline.QueueCommand(c => c.PublishMessageAsync("uc", "b"));

            await pipeline.FlushAsync();
        }

        [Test]
        public async Task Can_Pop_priority_message_from_SortedSet_and_Add_to_workq_in_atomic_transaction()
        {
            var messages = new List<string> { "message4", "message3", "message2" };

            await RedisAsync.AddItemToListAsync("workq", "message1");

            var priority = 1;
            await messages.ForEachAsync(async x => await RedisAsync.AddItemToSortedSetAsync("prioritymsgs", x, priority++));

            var highestPriorityMessage = await RedisAsync.PopItemWithHighestScoreFromSortedSetAsync("prioritymsgs");

            await using (var trans = RedisAsync.CreatePipeline())
            {
                trans.QueueCommand(r => r.RemoveItemFromSortedSetAsync("prioritymsgs", highestPriorityMessage));
                trans.QueueCommand(r => r.AddItemToListAsync("workq", highestPriorityMessage));

                await trans.FlushAsync();
            }

            Assert.That(await RedisAsync.GetAllItemsFromListAsync("workq"),
                Is.EquivalentTo(new List<string> { "message1", "message2" }));
            Assert.That(await RedisAsync.GetAllItemsFromSortedSetAsync("prioritymsgs"),
                Is.EquivalentTo(new List<string> { "message3", "message4" }));
        }

    }
}