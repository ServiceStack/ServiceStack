using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Redis.Tests.Issues
{
	[TestFixture]
	public class TransactionIssueTests
		: RedisClientTestsBase
	{
		[Test]
		public void Can_Get_and_Remove_multiple_keys_in_same_transaction()
		{
			5.Times(x => Redis.Set("foo" + x, x));

			var keys = Redis.SearchKeys("foo*");
			Assert.That(keys, Has.Count.EqualTo(5));

			var dict = new Dictionary<string, int>();
			using (var transaction = Redis.CreateTransaction())
			{
				foreach (var key in keys)
				{
					var y = key;
					transaction.QueueCommand(x => x.Get<int>(y), val => dict.Add(y, val));
				}
				transaction.QueueCommand(x => x.RemoveAll(keys));
				transaction.Commit();
			}

			Assert.That(dict, Has.Count.EqualTo(5));
			keys = Redis.SearchKeys("foo*");
			Assert.That(keys, Has.Count.EqualTo(0));
		}

		[Test]
		public void Can_GetValues_and_Remove_multiple_keys_in_same_transaction()
		{
			5.Times(x => Redis.Set("foo" + x, x));

			var keys = Redis.SearchKeys("foo*");
			Assert.That(keys, Has.Count.EqualTo(5));

			var values = new List<string>();
			using (var transaction = Redis.CreateTransaction())
			{
				transaction.QueueCommand(x => x.GetValues(keys), val => values = val);
				transaction.QueueCommand(x => x.RemoveAll(keys));
				transaction.Commit();
			}

			Assert.That(values, Has.Count.EqualTo(5));
			keys = Redis.SearchKeys("foo*");
			Assert.That(keys, Has.Count.EqualTo(0));
		}

	}
}