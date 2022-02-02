using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class RedisTransactionCommonTestsAsync
        : RedisClientTestsBaseAsync
    {
        private const string Prefix = "tran";

        public override void OnAfterEachTest()
        {
            CleanMask = Prefix + "*";
            base.OnAfterEachTest();
        }

        [Test]
        public async Task Can_Set_and_Expire_key_in_atomic_transaction()
        {
            var oneSec = TimeSpan.FromSeconds(1);

            Assert.That(await RedisAsync.GetValueAsync(Prefix + "key"), Is.Null);
            await using (var trans = await RedisAsync.CreateTransactionAsync())              //Calls 'MULTI'
            {
                trans.QueueCommand(r => r.SetValueAsync(Prefix + "key", "a"));      //Queues 'SET key a'
                trans.QueueCommand(r => r.ExpireEntryInAsync(Prefix + "key", oneSec)); //Queues 'EXPIRE key 1'

                await trans.CommitAsync();                                        //Calls 'EXEC'

            }                                                          //Calls 'DISCARD' if 'EXEC' wasn't called

            Assert.That(await RedisAsync.GetValueAsync(Prefix + "key"), Is.EqualTo("a"));
            await Task.Delay(TimeSpan.FromSeconds(2));
            Assert.That(await RedisAsync.GetValueAsync(Prefix + "key"), Is.Null);
        }

        [Test]
        public async Task Can_Pop_priority_message_from_SortedSet_and_Add_to_workq_in_atomic_transaction()
        {
            var messages = new List<string> { "message4", "message3", "message2" };

            await RedisAsync.AddItemToListAsync(Prefix + "workq", "message1");

            var priority = 1;
            await messages.ForEachAsync(async x => await RedisAsync.AddItemToSortedSetAsync(Prefix + "prioritymsgs", x, priority++));

            var highestPriorityMessage = await RedisAsync.PopItemWithHighestScoreFromSortedSetAsync(Prefix + "prioritymsgs");

            await using (var trans = await RedisAsync.CreateTransactionAsync())
            {
                trans.QueueCommand(r => r.RemoveItemFromSortedSetAsync(Prefix + "prioritymsgs", highestPriorityMessage));
                trans.QueueCommand(r => r.AddItemToListAsync(Prefix + "workq", highestPriorityMessage));

                await trans.CommitAsync();
            }

            Assert.That(await RedisAsync.GetAllItemsFromListAsync(Prefix + "workq"),
                Is.EquivalentTo(new List<string> { "message1", "message2" }));
            Assert.That(await RedisAsync.GetAllItemsFromSortedSetAsync(Prefix + "prioritymsgs"),
                Is.EquivalentTo(new List<string> { "message3", "message4" }));
        }

    }
}