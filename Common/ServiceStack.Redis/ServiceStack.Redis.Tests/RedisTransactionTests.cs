using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace ServiceStack.Redis.Tests
{
	[TestFixture]
	public class RedisTransactionTests
		: RedisClientTestsBase
	{
		private const string Key = "multitest";
		private const string ListKey = "multitest-list";
		private const string SetKey = "multitest-set";
		private const string SortedSetKey = "multitest-sortedset";

		[Test]
		public void Can_call_single_operation_in_transaction()
		{
			Assert.That(Redis.GetValue(Key), Is.Null);
			using (var trans = Redis.CreateTransaction())
			{
				trans.QueueCommand(r => r.IncrementValue(Key));

				trans.Commit();
			}

			Assert.That(Redis.GetValue(Key), Is.EqualTo("1"));
		}

		[Test]
		public void No_commit_of_atomic_transactions_discards_all_commands()
		{
			Assert.That(Redis.GetValue(Key), Is.Null);
			using (var trans = Redis.CreateTransaction())
			{
				trans.QueueCommand(r => r.IncrementValue(Key));
			}
			Assert.That(Redis.GetValue(Key), Is.Null);
		}

		[Test]
		public void Exception_in_atomic_transactions_discards_all_commands()
		{
			Assert.That(Redis.GetValue(Key), Is.Null);
			try
			{
				using (var trans = Redis.CreateTransaction())
				{
					trans.QueueCommand(r => r.IncrementValue(Key));
					throw new NotSupportedException();
				}
			}
			catch (NotSupportedException ignore)
			{
				Assert.That(Redis.GetValue(Key), Is.Null);
			}
		}

		[Test]
		public void Can_call_single_operation_3_Times_in_transaction()
		{
			Assert.That(Redis.GetValue(Key), Is.Null);
			using (var trans = Redis.CreateTransaction())
			{
				trans.QueueCommand(r => r.IncrementValue(Key));
				trans.QueueCommand(r => r.IncrementValue(Key));
				trans.QueueCommand(r => r.IncrementValue(Key));

				trans.Commit();
			}

			Assert.That(Redis.GetValue(Key), Is.EqualTo("3"));
		}

		[Test]
		public void Can_call_single_operation_with_callback_3_Times_in_transaction()
		{
			var results = new List<int>();
			Assert.That(Redis.GetValue(Key), Is.Null);
			using (var trans = Redis.CreateTransaction())
			{
				trans.QueueCommand(r => r.IncrementValue(Key), results.Add);
				trans.QueueCommand(r => r.IncrementValue(Key), results.Add);
				trans.QueueCommand(r => r.IncrementValue(Key), results.Add);

				trans.Commit();
			}

			Assert.That(Redis.GetValue(Key), Is.EqualTo("3"));
			Assert.That(results, Is.EquivalentTo(new List<int> { 1, 2, 3 }));
		}

		[Test]
		public void Supports_different_operation_types_in_same_transaction()
		{
			var incrementResults = new List<int>();
			var collectionCounts = new List<int>();
			var containsItem = false;

			Assert.That(Redis.GetValue(Key), Is.Null);
			using (var trans = Redis.CreateTransaction())
			{
				trans.QueueCommand(r => r.IncrementValue(Key), intResult => incrementResults.Add(intResult));
				trans.QueueCommand(r => r.AddItemToList(ListKey, "listitem1"));
				trans.QueueCommand(r => r.AddItemToList(ListKey, "listitem2"));
				trans.QueueCommand(r => r.AddItemToSet(SetKey, "setitem"));
				trans.QueueCommand(r => r.SetContainsItem(SetKey, "setitem"), b => containsItem = b);
				trans.QueueCommand(r => r.AddItemToSortedSet(SortedSetKey, "sortedsetitem1"));
				trans.QueueCommand(r => r.AddItemToSortedSet(SortedSetKey, "sortedsetitem2"));
				trans.QueueCommand(r => r.AddItemToSortedSet(SortedSetKey, "sortedsetitem3"));
				trans.QueueCommand(r => r.GetListCount(ListKey), intResult => collectionCounts.Add(intResult));
				trans.QueueCommand(r => r.GetSetCount(SetKey), intResult => collectionCounts.Add(intResult));
				trans.QueueCommand(r => r.GetSortedSetCount(SortedSetKey), intResult => collectionCounts.Add(intResult));
				trans.QueueCommand(r => r.IncrementValue(Key), intResult => incrementResults.Add(intResult));

				trans.Commit();
			}

			Assert.That(containsItem, Is.True);
			Assert.That(Redis.GetValue(Key), Is.EqualTo("2"));
			Assert.That(incrementResults, Is.EquivalentTo(new List<int> { 1, 2 }));
			Assert.That(collectionCounts, Is.EquivalentTo(new List<int> { 2, 1, 3 }));
			Assert.That(Redis.GetAllItemsFromList(ListKey), Is.EquivalentTo(new List<string> { "listitem1", "listitem2" }));
			Assert.That(Redis.GetAllItemsFromSet(SetKey), Is.EquivalentTo(new List<string> { "setitem" }));
			Assert.That(Redis.GetAllItemsFromSortedSet(SortedSetKey), Is.EquivalentTo(new List<string> { "sortedsetitem1", "sortedsetitem2", "sortedsetitem3" }));
		}

		[Test]
		public void Can_call_multi_string_operations_in_transaction()
		{
			string item1 = null;
			string item4 = null;

			var results = new List<string>();
			Assert.That(Redis.GetListCount(ListKey), Is.EqualTo(0));
			using (var trans = Redis.CreateTransaction())
			{
				trans.QueueCommand(r => r.AddItemToList(ListKey, "listitem1"));
				trans.QueueCommand(r => r.AddItemToList(ListKey, "listitem2"));
				trans.QueueCommand(r => r.AddItemToList(ListKey, "listitem3"));
				trans.QueueCommand(r => r.GetAllItemsFromList(ListKey), x => results = x);
				trans.QueueCommand(r => r.GetItemFromList(ListKey, 0), x => item1 = x);
				trans.QueueCommand(r => r.GetItemFromList(ListKey, 4), x => item4 = x);

				trans.Commit();
			}

			Assert.That(Redis.GetListCount(ListKey), Is.EqualTo(3));
			Assert.That(results, Is.EquivalentTo(new List<string> { "listitem1", "listitem2", "listitem3" }));
			Assert.That(item1, Is.EqualTo("listitem1"));
			Assert.That(item4, Is.Null);
		}
	
	}
}