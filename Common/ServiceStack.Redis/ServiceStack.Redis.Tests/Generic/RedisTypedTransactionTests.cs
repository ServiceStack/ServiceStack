using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Redis.Generic;

namespace ServiceStack.Redis.Tests.Generic
{
	[TestFixture]
	public class RedisTypedTransactionTests
		: RedisClientTestsBase
	{
		private const string Key = "multitest";
		private const string ListKey = "multitest-list";
		private const string SetKey = "multitest-set";
		private const string SortedSetKey = "multitest-sortedset";

		readonly ShipperFactory modelFactory = new ShipperFactory();
		private IRedisTypedClient<Shipper> typedClient;
		private Shipper model;

		public override void OnBeforeEachTest()
		{
			base.OnBeforeEachTest();

			typedClient = Redis.GetTypedClient<Shipper>();			
			model = modelFactory.CreateInstance(1);
		}


		[Test]
		public void Can_call_single_operation_in_transaction()
		{
			Assert.That(typedClient.Get(Key), Is.Null);

			using (var trans = typedClient.CreateTransaction())
			{
				trans.QueueCommand(r => r.Set(Key, model));

				trans.Commit();
			}

			modelFactory.AssertIsEqual(typedClient.Get(Key), model);
		}

		[Test]
		public void No_commit_of_atomic_transactions_discards_all_commands()
		{
			Assert.That(typedClient.Get(Key), Is.Null);

			using (var trans = typedClient.CreateTransaction())
			{
				trans.QueueCommand(r => r.Set(Key, model));
			}

			Assert.That(typedClient.Get(Key), Is.Null);
		}

		[Test]
		public void Exception_in_atomic_transactions_discards_all_commands()
		{
			Assert.That(typedClient.Get(Key), Is.Null);
			try
			{
				using (var trans = typedClient.CreateTransaction())
				{
					trans.QueueCommand(r => r.Set(Key, model));
					throw new NotSupportedException();
				}
			}
			catch (NotSupportedException ignore)
			{
				Assert.That(typedClient.Get(Key), Is.Null);
			}
		}

		[Test]
		public void Can_call_single_operation_3_Times_in_transaction()
		{
			var typedList = typedClient.Lists[ListKey];
			Assert.That(typedList.Count, Is.EqualTo(0));

			using (var trans = typedClient.CreateTransaction())
			{
				trans.QueueCommand(r => r.AddToList(typedList, modelFactory.CreateInstance(1)));
				trans.QueueCommand(r => r.AddToList(typedList, modelFactory.CreateInstance(2)));
				trans.QueueCommand(r => r.AddToList(typedList, modelFactory.CreateInstance(3)));

				trans.Commit();
			}

			Assert.That(typedList.Count, Is.EqualTo(3));
		}

		[Test]
		public void Can_call_single_operation_with_callback_3_Times_in_transaction()
		{
			var results = new List<int>();

			var typedList = typedClient.Lists[ListKey];
			Assert.That(typedList.Count, Is.EqualTo(0));

			using (var trans = typedClient.CreateTransaction())
			{
				trans.QueueCommand(r => r.AddToList(typedList, modelFactory.CreateInstance(1)), () => results.Add(1));
				trans.QueueCommand(r => r.AddToList(typedList, modelFactory.CreateInstance(2)), () => results.Add(2));
				trans.QueueCommand(r => r.AddToList(typedList, modelFactory.CreateInstance(3)), () => results.Add(3));

				trans.Commit();
			}

			Assert.That(typedList.Count, Is.EqualTo(3));
			Assert.That(results, Is.EquivalentTo(new List<int> { 1, 2, 3 }));
		}

		[Test]
		public void Supports_different_operation_types_in_same_transaction()
		{
			var incrementResults = new List<int>();
			var collectionCounts = new List<int>();
			var containsItem = false;

			var typedList = typedClient.Lists[ListKey];
			var typedSet = typedClient.Sets[SetKey];
			var typedSortedSet = typedClient.SortedSets[SortedSetKey];

			Assert.That(typedClient.Get(Key), Is.Null);
			using (var trans = typedClient.CreateTransaction())
			{
				trans.QueueCommand(r => r.Increment(Key), intResult => incrementResults.Add(intResult));
				trans.QueueCommand(r => r.AddToList(typedList, modelFactory.CreateInstance(1)));
				trans.QueueCommand(r => r.AddToList(typedList, modelFactory.CreateInstance(2)));
				trans.QueueCommand(r => r.AddToSet(typedSet, modelFactory.CreateInstance(3)));
				trans.QueueCommand(r => r.SetContainsValue(typedSet, modelFactory.CreateInstance(3)), b => containsItem = b);
				trans.QueueCommand(r => r.AddToSortedSet(typedSortedSet, modelFactory.CreateInstance(4)));
				trans.QueueCommand(r => r.AddToSortedSet(typedSortedSet, modelFactory.CreateInstance(5)));
				trans.QueueCommand(r => r.AddToSortedSet(typedSortedSet, modelFactory.CreateInstance(6)));
				trans.QueueCommand(r => r.GetListCount(typedList), intResult => collectionCounts.Add(intResult));
				trans.QueueCommand(r => r.GetSetCount(typedSet), intResult => collectionCounts.Add(intResult));
				trans.QueueCommand(r => r.GetSortedSetCount(typedSortedSet), intResult => collectionCounts.Add(intResult));
				trans.QueueCommand(r => r.Increment(Key), intResult => incrementResults.Add(intResult));

				trans.Commit();
			}

			Assert.That(containsItem, Is.True);
			Assert.That(Redis.GetString(Key), Is.EqualTo("2"));
			Assert.That(incrementResults, Is.EquivalentTo(new List<int> { 1, 2 }));
			Assert.That(collectionCounts, Is.EquivalentTo(new List<int> { 2, 1, 3 }));

			modelFactory.AssertListsAreEqual(typedList.GetAll(), new List<Shipper>
        		{
	        		modelFactory.CreateInstance(1), modelFactory.CreateInstance(2)
        		});

			Assert.That(typedSet.GetAll(), Is.EquivalentTo(new List<Shipper>
   				{
   					modelFactory.CreateInstance(3)
   				}));

			modelFactory.AssertListsAreEqual(typedSortedSet.GetAll(), new List<Shipper>
        		{
	        		modelFactory.CreateInstance(4), modelFactory.CreateInstance(5), modelFactory.CreateInstance(6)
        		});
		}

		[Test]
		public void Can_call_multi_string_operations_in_transaction()
		{
			Shipper item1 = null;
			Shipper item4 = null;

			var results = new List<Shipper>();
		
			var typedList = typedClient.Lists[ListKey];
			Assert.That(typedList.Count, Is.EqualTo(0));

			using (var trans = typedClient.CreateTransaction())
			{
				trans.QueueCommand(r => r.AddToList(typedList, modelFactory.CreateInstance(1)));
				trans.QueueCommand(r => r.AddToList(typedList, modelFactory.CreateInstance(2)));
				trans.QueueCommand(r => r.AddToList(typedList, modelFactory.CreateInstance(3)));
				trans.QueueCommand(r => r.GetAllFromList(typedList), x => results = x);
				trans.QueueCommand(r => r.GetItemFromList(typedList, 0), x => item1 = x);
				trans.QueueCommand(r => r.GetItemFromList(typedList, 4), x => item4 = x);

				trans.Commit();
			}

			Assert.That(typedList.Count, Is.EqualTo(3));

			modelFactory.AssertListsAreEqual(results, new List<Shipper>
        		{
	        		modelFactory.CreateInstance(1), modelFactory.CreateInstance(2), modelFactory.CreateInstance(3)
        		});

			modelFactory.AssertIsEqual(item1, modelFactory.CreateInstance(1));
			Assert.That(item4, Is.Null);
		}
	
	}
}