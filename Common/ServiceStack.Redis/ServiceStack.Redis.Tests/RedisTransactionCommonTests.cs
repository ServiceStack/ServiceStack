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
		private const string Key = "multitest";

		[Test]
		public void Can_Set_and_Expire_key_in_atomic_transaction()
		{
			var oneSec = TimeSpan.FromSeconds(1);

			Assert.That(Redis.GetString(Key), Is.Null);
			using (var trans = Redis.CreateTransaction())            //Calls 'MULTI'
			{
				trans.QueueCommand(r => r.SetString(Key, "a"));      //Queues 'SET a'
				trans.QueueCommand(r => r.ExpireKeyIn(Key, oneSec)); //Queues 'EXPIRESIN a 1'

				trans.Commit();                                      //Calls 'EXEC'

			}                                                        //Calls 'DISCARD' if 'EXEC' wasn't called

			Assert.That(Redis.GetString(Key), Is.EqualTo("a"));
			Thread.Sleep(TimeSpan.FromSeconds(2));
			Assert.That(Redis.GetString(Key), Is.Null);
		}

		[Test]
		public void Can_Pop_priority_message_from_SortedSet_and_Add_to_workq_in_atomic_transaction()
		{
			var messages = new List<string> { "message4", "message3", "message2" };

			Redis.AddToList("workq", "message1");
			
			var priority = 1;
			messages.ForEach(x => Redis.AddToSortedSet("prioritymsgs", x, priority++));

			var highestPriorityMessage = Redis.PopFromSortedSetItemWithHighestScore("prioritymsgs");

			using (var trans = Redis.CreateTransaction())
			{
				trans.QueueCommand(r => r.RemoveFromSortedSet("prioritymsgs", highestPriorityMessage));
				trans.QueueCommand(r => r.AddToList("workq", highestPriorityMessage));	

				trans.Commit();											
			}

			Assert.That(Redis.GetAllFromList("workq"), 
				Is.EquivalentTo(new List<string> { "message1", "message2" }));
			Assert.That(Redis.GetAllFromSortedSet("prioritymsgs"), 
				Is.EquivalentTo(new List<string> { "message3", "message4" }));
		}

	}
}