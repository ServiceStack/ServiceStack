using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using RedisWebServices.ServiceModel.Operations.List;

namespace RedisWebServices.Tests
{
	public class ListOperationsTests
		: TestBase
	{
		private const string ListId = "testlist";
		private const string ListId2 = "testlist2";
		private List<string> stringList;

		public override void OnBeforeEachTest()
		{
			base.OnBeforeEachTest();

			stringList = new List<string> { "one", "two", "three", "four" };
		}

		[Test]
		public void Test_AddItemToList()
		{
			var response = base.Send<AddItemToListResponse>(
				new AddItemToList { Id = ListId, Item = TestValue }, x => x.ResponseStatus);

			var value = RedisExec(r => r.GetItemFromList(ListId, 0));

			Assert.That(value, Is.EqualTo(TestValue));
		}

		[Test]
		public void Test_BlockingDequeueItemFromList()
		{
			stringList.ForEach(x =>
				RedisExec(r => r.AddItemToList(ListId, x)));

			var response = base.Send<BlockingDequeueItemFromListResponse>(
				new BlockingDequeueItemFromList { Id = ListId }, x => x.ResponseStatus);

			Assert.That(response.Item, Is.EqualTo(stringList.First()));
		}

		[Test]
		public void Test_BlockingPopItemFromList()
		{
			stringList.ForEach(x =>
				RedisExec(r => r.AddItemToList(ListId, x)));

			var response = base.Send<BlockingPopItemFromListResponse>(
				new BlockingPopItemFromList { Id = ListId }, x => x.ResponseStatus);

			Assert.That(response.Item, Is.EqualTo(stringList.Last()));
		}

		[Test]
		public void Test_BlockingRemoveStartFromList()
		{
			stringList.ForEach(x =>
				RedisExec(r => r.AddItemToList(ListId, x)));

			var response = base.Send<BlockingRemoveStartFromListResponse>(
				new BlockingRemoveStartFromList { Id = ListId }, x => x.ResponseStatus);

			Assert.That(response.Item, Is.EqualTo(stringList.First()));
		}

		[Test]
		public void Test_DequeueItemFromList()
		{
			stringList.ForEach(x =>
				RedisExec(r => r.AddItemToList(ListId, x)));

			var response = base.Send<DequeueItemFromListResponse>(
				new DequeueItemFromList { Id = ListId }, x => x.ResponseStatus);

			Assert.That(response.Item, Is.EqualTo(stringList.First()));
		}

		[Test]
		public void Test_EnqueueItemOnList()
		{
			var response = base.Send<EnqueueItemOnListResponse>(
				new EnqueueItemOnList { Id = ListId, Item = TestValue }, x => x.ResponseStatus);

			var value = RedisExec(r => r.GetItemFromList(ListId, 0));

			Assert.That(value, Is.EqualTo(TestValue));
		}

		[Test]
		public void Test_GetAllItemsFromList()
		{
			var a = new Dictionary<string, string>();
			stringList.ForEach(x =>
				RedisExec(r => r.AddItemToList(ListId, x)));

			var response = base.Send<GetAllItemsFromListResponse>(
				new GetAllItemsFromList { Id = ListId }, x => x.ResponseStatus);

			Assert.That(response.Items, Is.EquivalentTo(stringList));
		}

		[Test]
		public void Test_GetItemFromList()
		{
			stringList.ForEach(x =>
				RedisExec(r => r.AddItemToList(ListId, x)));

			var response = base.Send<GetItemFromListResponse>(
				new GetItemFromList { Id = ListId, Index = 0 }, x => x.ResponseStatus);

			Assert.That(response.Item, Is.EqualTo(stringList[0]));
		}

		[Test]
		public void Test_GetListCount()
		{
			stringList.ForEach(x =>
				RedisExec(r => r.AddItemToList(ListId, x)));

			var response = base.Send<GetListCountResponse>(
				new GetListCount { Id = ListId }, x => x.ResponseStatus);

			Assert.That(response.Count, Is.EqualTo(stringList.Count));
		}

		[Test]
		public void Test_GetRangeFromList()
		{
			stringList.ForEach(x =>
				RedisExec(r => r.AddItemToList(ListId, x)));

			var response = base.Send<GetRangeFromListResponse>(
				new GetRangeFromList { Id = ListId, StartingFrom = 0, EndingAt = 2 }, x => x.ResponseStatus);

			Assert.That(response.Items, Is.EquivalentTo(stringList.Take(3).ToList()));
		}

		[Test]
		public void Test_GetRangeFromSortedList()
		{
			stringList.ForEach(x =>
				RedisExec(r => r.AddItemToList(ListId, x)));

			var response = base.Send<GetRangeFromSortedListResponse>(
				new GetRangeFromSortedList { Id = ListId, StartingFrom = 0, EndingAt = 2 }, x => x.ResponseStatus);

			stringList.Sort((x,y) => x.CompareTo(y));

			Assert.That(response.Items, Is.EquivalentTo(stringList.Take(2).ToList()));
		}

		[Test]
		public void Test_PopAndPushItemBetweenLists()
		{
			stringList.ForEach(x =>
				RedisExec(r => r.AddItemToList(ListId, x)));

			var response = base.Send<PopAndPushItemBetweenListsResponse>(
				new PopAndPushItemBetweenLists { FromListId = ListId, ToListId = ListId2 }, x => x.ResponseStatus);

			var expectedPop = stringList.Last();
			Assert.That(response.Item, Is.EqualTo(expectedPop));
		}

		[Test]
		public void Test_PopItemFromList()
		{
			stringList.ForEach(x =>
				RedisExec(r => r.AddItemToList(ListId, x)));

			var response = base.Send<PopItemFromListResponse>(
				new PopItemFromList { Id = ListId }, x => x.ResponseStatus);

			var expectedPop = stringList.Last();
			Assert.That(response.Item, Is.EqualTo(expectedPop));
		}

		[Test]
		public void Test_PrependItemToList()
		{
			stringList.ForEach(x =>
				RedisExec(r => r.AddItemToList(ListId, x)));

			var response = base.Send<PrependItemToListResponse>(
				new PrependItemToList { Id = ListId, Item = TestValue }, x => x.ResponseStatus);

			stringList.Insert(0, TestValue);

			var items = RedisExec(r => r.GetAllItemsFromList(ListId));
			Assert.That(items, Is.EquivalentTo(stringList));
		}

		[Test]
		public void Test_PushItemToList()
		{
			stringList.ForEach(x =>
				RedisExec(r => r.AddItemToList(ListId, x)));

			var response = base.Send<PushItemToListResponse>(
				new PushItemToList { Id = ListId, Item = TestValue }, x => x.ResponseStatus);

			stringList.Add(TestValue);

			var items = RedisExec(r => r.GetAllItemsFromList(ListId));
			Assert.That(items, Is.EquivalentTo(stringList));
		}

		[Test]
		public void Test_RemoveAllFromList()
		{
			stringList.ForEach(x =>
				RedisExec(r => r.AddItemToList(ListId, x)));

			var response = base.Send<RemoveAllFromListResponse>(
				new RemoveAllFromList { Id = ListId }, x => x.ResponseStatus);

			var items = RedisExec(r => r.GetAllItemsFromList(ListId));
			Assert.That(items, Is.Empty);
		}

		[Test]
		public void Test_RemoveEndFromList()
		{
			stringList.ForEach(x =>
				RedisExec(r => r.AddItemToList(ListId, x)));

			var response = base.Send<RemoveEndFromListResponse>(
				new RemoveEndFromList { Id = ListId }, x => x.ResponseStatus);

			var expected = stringList.Last();
			Assert.That(response.Item, Is.EqualTo(expected));
		}

		[Test]
		public void Test_RemoveItemFromList()
		{
			stringList.ForEach(x =>
				RedisExec(r => r.AddItemToList(ListId, x)));

			var expected = stringList.Last();

			var response = base.Send<RemoveItemFromListResponse>(
				new RemoveItemFromList { Id = ListId, Item = expected }, x => x.ResponseStatus);

			Assert.That(response.ItemsRemovedCount, Is.EqualTo(1));

			stringList.Remove(expected);
			var items = RedisExec(r => r.GetAllItemsFromList(ListId));
			Assert.That(items, Is.EquivalentTo(stringList));
		}

		[Test]
		public void Test_RemoveStartFromList()
		{
			stringList.ForEach(x =>
				RedisExec(r => r.AddItemToList(ListId, x)));

			var response = base.Send<RemoveStartFromListResponse>(
				new RemoveStartFromList { Id = ListId }, x => x.ResponseStatus);

			var expected = stringList.First();
			Assert.That(response.Item, Is.EqualTo(expected));
		}

		[Test]
		public void Test_SetItemInList()
		{
			stringList.ForEach(x =>
				RedisExec(r => r.AddItemToList(ListId, x)));

			var response = base.Send<SetItemInListResponse>(
				new SetItemInList { Id = ListId, Index = 1, Item = TestValue }, x => x.ResponseStatus);

			stringList[1] = TestValue;
			var items = RedisExec(r => r.GetAllItemsFromList(ListId));
			Assert.That(items, Is.EquivalentTo(stringList));
		}

		[Test]
		public void Test_TrimList()
		{
			stringList.ForEach(x =>
				RedisExec(r => r.AddItemToList(ListId, x)));

			var response = base.Send<TrimListResponse>(
				new TrimList { Id = ListId, KeepStartingFrom = 0, KeepEndingAt = 2 }, x => x.ResponseStatus);

			var items = RedisExec(r => r.GetAllItemsFromList(ListId));
			Assert.That(items, Is.EquivalentTo(stringList.Take(3).ToList()));
		}
	}
}