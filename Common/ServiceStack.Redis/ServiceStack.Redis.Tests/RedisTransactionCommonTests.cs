using System;
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
			using (var trans = Redis.CreateTransaction())				//Calls 'MULTI'
			{
				trans.QueueCommand(r => r.SetString(Key, "a"));			//Queues 'SET a'
				trans.QueueCommand(r => r.ExpireKeyIn(Key, oneSec));	//Queues 'EXPIRESIN a 1'

				trans.Commit();											//Calls 'EXEC'

			}															//Calls 'DISCARD' if 'EXEC' wasn't called

			Assert.That(Redis.GetString(Key), Is.EqualTo("a"));
			Thread.Sleep(TimeSpan.FromSeconds(2));
			Assert.That(Redis.GetString(Key), Is.Null);
		}

	}
}