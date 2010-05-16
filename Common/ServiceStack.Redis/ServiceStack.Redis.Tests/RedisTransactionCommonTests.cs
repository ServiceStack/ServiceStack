using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace ServiceStack.Redis.Tests
{
	[TestFixture]
	public class RedisTransactionCommonTests
		: RedisClientTestsBase
	{
		[Test]
		public void Can_Set_and_Expire_key_in_atomic_transaction()
		{
			var oneSec = TimeSpan.FromSeconds(1);

			Assert.That(Redis.GetValue("key"), Is.Null);
			using (var trans = Redis.CreateTransaction())              //Calls 'MULTI'
			{
				trans.QueueCommand(r => r.SetEntry("key", "a"));      //Queues 'SET key a'
				trans.QueueCommand(r => r.ExpireEntryIn("key", oneSec)); //Queues 'EXPIRE key 1'

				trans.Commit();                                        //Calls 'EXEC'

			}                                                          //Calls 'DISCARD' if 'EXEC' wasn't called

			Assert.That(Redis.GetValue("key"), Is.EqualTo("a"));
			Thread.Sleep(TimeSpan.FromSeconds(2));
			Assert.That(Redis.GetValue("key"), Is.Null);
		}

		[Test]
		public void Can_Pop_priority_message_from_SortedSet_and_Add_to_workq_in_atomic_transaction()
		{
			var messages = new List<string> { "message4", "message3", "message2" };

			Redis.AddItemToList("workq", "message1");
			
			var priority = 1;
			messages.ForEach(x => Redis.AddItemToSortedSet("prioritymsgs", x, priority++));

			var highestPriorityMessage = Redis.PopItemWithHighestScoreFromSortedSet("prioritymsgs");

			using (var trans = Redis.CreateTransaction())
			{
				trans.QueueCommand(r => r.RemoveItemFromSortedSet("prioritymsgs", highestPriorityMessage));
				trans.QueueCommand(r => r.AddItemToList("workq", highestPriorityMessage));	

				trans.Commit();											
			}

			Assert.That(Redis.GetAllItemsFromList("workq"), 
				Is.EquivalentTo(new List<string> { "message1", "message2" }));
			Assert.That(Redis.GetAllItemsFromSortedSet("prioritymsgs"), 
				Is.EquivalentTo(new List<string> { "message3", "message4" }));
		}

	}
}