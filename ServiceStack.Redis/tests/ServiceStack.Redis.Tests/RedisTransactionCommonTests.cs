using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class RedisTransactionCommonTests
        : RedisClientTestsBase
    {
        private const string Prefix = "tran";

        public override void OnAfterEachTest()
        {
            CleanMask = Prefix + "*";
            base.OnAfterEachTest();
        }

        [Test]
        public void Can_Set_and_Expire_key_in_atomic_transaction()
        {
            var oneSec = TimeSpan.FromSeconds(1);

            Assert.That(Redis.GetValue(Prefix + "key"), Is.Null);
            using (var trans = Redis.CreateTransaction())              //Calls 'MULTI'
            {
                trans.QueueCommand(r => r.SetValue(Prefix + "key", "a"));      //Queues 'SET key a'
                trans.QueueCommand(r => r.ExpireEntryIn(Prefix + "key", oneSec)); //Queues 'EXPIRE key 1'

                trans.Commit();                                        //Calls 'EXEC'

            }                                                          //Calls 'DISCARD' if 'EXEC' wasn't called

            Assert.That(Redis.GetValue(Prefix + "key"), Is.EqualTo("a"));
            Thread.Sleep(TimeSpan.FromSeconds(2));
            Assert.That(Redis.GetValue(Prefix + "key"), Is.Null);
        }

        [Test]
        public void Can_Pop_priority_message_from_SortedSet_and_Add_to_workq_in_atomic_transaction()
        {
            var messages = new List<string> { "message4", "message3", "message2" };

            Redis.AddItemToList(Prefix + "workq", "message1");

            var priority = 1;
            messages.ForEach(x => Redis.AddItemToSortedSet(Prefix + "prioritymsgs", x, priority++));

            var highestPriorityMessage = Redis.PopItemWithHighestScoreFromSortedSet(Prefix + "prioritymsgs");

            using (var trans = Redis.CreateTransaction())
            {
                trans.QueueCommand(r => r.RemoveItemFromSortedSet(Prefix + "prioritymsgs", highestPriorityMessage));
                trans.QueueCommand(r => r.AddItemToList(Prefix + "workq", highestPriorityMessage));

                trans.Commit();
            }

            Assert.That(Redis.GetAllItemsFromList(Prefix + "workq"),
                Is.EquivalentTo(new List<string> { "message1", "message2" }));
            Assert.That(Redis.GetAllItemsFromSortedSet(Prefix + "prioritymsgs"),
                Is.EquivalentTo(new List<string> { "message3", "message4" }));
        }

    }
}