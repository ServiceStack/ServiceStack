using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class RedisPipelineCommonTests
        : RedisClientTestsBase
    {
        [Test]
        public void Can_Set_and_Expire_key_in_atomic_transaction()
        {
            var oneSec = TimeSpan.FromSeconds(1);

            Assert.That(Redis.GetValue("key"), Is.Null);
            using (var trans = Redis.CreatePipeline())              //Calls 'MULTI'
            {
                trans.QueueCommand(r => r.SetValue("key", "a"));      //Queues 'SET key a'
                trans.QueueCommand(r => r.ExpireEntryIn("key", oneSec)); //Queues 'EXPIRE key 1'

                trans.Flush();                                        //Calls 'EXEC'

            }                                                          //Calls 'DISCARD' if 'EXEC' wasn't called

            Assert.That(Redis.GetValue("key"), Is.EqualTo("a"));
            Thread.Sleep(TimeSpan.FromSeconds(2));
            Assert.That(Redis.GetValue("key"), Is.Null);
        }

        [Test]
        public void Can_SetAll_and_Publish_in_atomic_transaction()
        {
            var messages = new Dictionary<string, string> { { "a", "a" }, { "b", "b" } };
            using (var pipeline = Redis.CreatePipeline())
            {
                pipeline.QueueCommand(c => c.SetAll(messages.ToDictionary(t => t.Key, t => t.Value)));
                pipeline.QueueCommand(c => c.PublishMessage("uc", "b"));

                pipeline.Flush();
            }
        }

        [Test]
        public void Can_Pop_priority_message_from_SortedSet_and_Add_to_workq_in_atomic_transaction()
        {
            var messages = new List<string> { "message4", "message3", "message2" };

            Redis.AddItemToList("workq", "message1");

            var priority = 1;
            messages.ForEach(x => Redis.AddItemToSortedSet("prioritymsgs", x, priority++));

            var highestPriorityMessage = Redis.PopItemWithHighestScoreFromSortedSet("prioritymsgs");

            using (var trans = Redis.CreatePipeline())
            {
                trans.QueueCommand(r => r.RemoveItemFromSortedSet("prioritymsgs", highestPriorityMessage));
                trans.QueueCommand(r => r.AddItemToList("workq", highestPriorityMessage));

                trans.Flush();
            }

            Assert.That(Redis.GetAllItemsFromList("workq"),
                Is.EquivalentTo(new List<string> { "message1", "message2" }));
            Assert.That(Redis.GetAllItemsFromSortedSet("prioritymsgs"),
                Is.EquivalentTo(new List<string> { "message3", "message4" }));
        }

    }
}