using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using RedisWebServices.ServiceModel.Operations.SortedSet;
using RedisWebServices.ServiceModel.Types;
using ServiceStack.Common.Extensions;

namespace RedisWebServices.Tests
{
	public class SortedSetOperationsTests
		: TestBase
	{
		private const string SetId = "testzset";
		private const string SetId2 = "testzset2";
		private const string SetId3 = "testzset3";
		private List<string> stringList;
		private List<string> stringList2;

		Dictionary<string, double> stringDoubleMap;

		public override void OnBeforeEachTest()
		{
			base.OnBeforeEachTest();
			stringList = new List<string> { "one", "two", "three", "four" };
			stringList2 = new List<string> { "four", "five", "six", "seven" };

			stringDoubleMap = new Dictionary<string, double> {
     			{"one",1}, {"two",2}, {"three",3}, {"four",4}
     		};
		}

		[Test]
		public void Test_AddItemToSortedSet()
		{
			var response = base.Send<AddItemToSortedSetResponse>(
				new AddItemToSortedSet { Id = SetId, Item = TestValue }, x => x.ResponseStatus);

			var value = RedisExec(r => r.PopItemWithHighestScoreFromSortedSet(SetId));

			Assert.That(value, Is.EqualTo(TestValue));
		}

		[Test]
		public void Test_GetAllItemsFromSortedSet()
		{
			stringList.ForEach(x =>
				RedisExec(r => r.AddItemToSortedSet(SetId, x)));

			var response = base.Send<GetAllItemsFromSortedSetResponse>(
				new GetAllItemsFromSortedSet { Id = SetId }, x => x.ResponseStatus);

			Assert.That(response.Items, Is.EquivalentTo(stringList));
		}

		[Test]
		public void Test_GetAllItemsFromSortedSetDesc()
		{
			stringList.ForEach(x =>
				RedisExec(r => r.AddItemToSortedSet(SetId, x)));

			var response = base.Send<GetAllItemsFromSortedSetDescResponse>(
				new GetAllItemsFromSortedSetDesc { Id = SetId }, x => x.ResponseStatus);

			Assert.That(response.Items, Is.EquivalentTo(stringList));
		}

		[Test]
		public void Test_GetItemIndexInSortedSet()
		{
			stringDoubleMap.ForEach(x =>
				RedisExec(r => r.AddItemToSortedSet(SetId, x.Key, x.Value)));

			var lastItem = stringDoubleMap.Last();
			var response = base.Send<GetItemIndexInSortedSetResponse>(
				new GetItemIndexInSortedSet { Id = SetId, Item = lastItem.Key }, x => x.ResponseStatus);

			Assert.That(response.Index, Is.EqualTo(stringDoubleMap.Count - 1));
		}

		[Test]
		public void Test_GetItemScoreInSortedSet()
		{
			stringDoubleMap.ForEach(x =>
				RedisExec(r => r.AddItemToSortedSet(SetId, x.Key, x.Value)));

			var lastItem = stringDoubleMap.Last();
			var response = base.Send<GetItemScoreInSortedSetResponse>(
				new GetItemScoreInSortedSet { Id = SetId, Item = lastItem.Key }, x => x.ResponseStatus);

			Assert.That(response.Score, Is.EqualTo(lastItem.Value));
		}

		[Test]
		[Ignore("Not Implemented in Redis yet")]
		public void Test_GetRangeFromSortedSetByHighestScore()
		{
			stringDoubleMap.ForEach(x =>
				RedisExec(r => r.AddItemToSortedSet(SetId, x.Key, x.Value)));

			var response = base.Send<GetRangeFromSortedSetByHighestScoreResponse>(
				new GetRangeFromSortedSetByHighestScore { Id = SetId, FromScore = 2, ToScore = 3 }, x => x.ResponseStatus);

			var expectedItems = stringDoubleMap.Where(x => x.Value >= 2 && x.Value <= 3)
				.OrderByDescending(x => x.Value).ConvertAll(x => x.Value);

			Assert.That(response.Items, Is.EquivalentTo(expectedItems));
		}

		[Test]
		public void Test_GetRangeFromSortedSetByLowestScore()
		{
			stringDoubleMap.ForEach(x =>
				RedisExec(r => r.AddItemToSortedSet(SetId, x.Key, x.Value)));

			var response = base.Send<GetRangeFromSortedSetByLowestScoreResponse>(
				new GetRangeFromSortedSetByLowestScore { Id = SetId, FromScore = 2, ToScore = 3 }, x => x.ResponseStatus);

			var expectedItems = stringDoubleMap.Where(x => x.Value >= 2 && x.Value <= 3)
				.OrderBy(x => x.Value).ConvertAll(x => x.Key);

			Assert.That(response.Items, Is.EquivalentTo(expectedItems));
		}

		[Test]
		public void Test_GetRangeWithScoresFromSortedSet()
		{
			stringDoubleMap.ForEach(x =>
				RedisExec(r => r.AddItemToSortedSet(SetId, x.Key, x.Value)));

			var response = base.Send<GetRangeWithScoresFromSortedSetResponse>(
				new GetRangeWithScoresFromSortedSet { Id = SetId, FromRank = 0, ToRank = 2 }, x => x.ResponseStatus);

			var expectedItems = stringDoubleMap.Take(3)
				.OrderBy(x => x.Value).ConvertAll(x => new ItemWithScore(x.Key, x.Value));

			Assert.That(response.ItemsWithScores, Is.EquivalentTo(expectedItems));
		}

		[Test]
		[Ignore("Not Implemented in Redis yet")]
		public void Test_GetRangeWithScoresFromSortedSetByHighestScore()
		{
			stringDoubleMap.ForEach(x =>
				RedisExec(r => r.AddItemToSortedSet(SetId, x.Key, x.Value)));

			var response = base.Send<GetRangeWithScoresFromSortedSetByHighestScoreResponse>(
				new GetRangeWithScoresFromSortedSetByHighestScore { Id = SetId, FromScore = 2, ToScore = 3 }, x => x.ResponseStatus);

			var expectedItems = stringDoubleMap.Where(x => x.Value >= 2 && x.Value <= 3)
				.OrderByDescending(x => x.Value).ConvertAll(x => new ItemWithScore(x.Key, x.Value));

			Assert.That(response.ItemsWithScores, Is.EquivalentTo(expectedItems));
		}

		[Test]
		public void Test_GetRangeWithScoresFromSortedSetByLowestScore()
		{
			stringDoubleMap.ForEach(x =>
				RedisExec(r => r.AddItemToSortedSet(SetId, x.Key, x.Value)));

			var response = base.Send<GetRangeWithScoresFromSortedSetByLowestScoreResponse>(
				new GetRangeWithScoresFromSortedSetByLowestScore { Id = SetId, FromScore = 2, ToScore = 3 }, x => x.ResponseStatus);

			var expectedItems = stringDoubleMap.Where(x => x.Value >= 2 && x.Value <= 3)
				.OrderBy(x => x.Value).ConvertAll(x => new ItemWithScore(x.Key, x.Value));

			Assert.That(response.ItemsWithScores, Is.EquivalentTo(expectedItems));
		}

		[Test]
		public void Test_GetSortedSetCount()
		{
			stringDoubleMap.ForEach(x =>
				RedisExec(r => r.AddItemToSortedSet(SetId, x.Key, x.Value)));

			var lastItem = stringDoubleMap.Last();
			var response = base.Send<GetSortedSetCountResponse>(
				new GetSortedSetCount { Id = SetId }, x => x.ResponseStatus);

			Assert.That(response.Count, Is.EqualTo(stringDoubleMap.Count));
		}

		[Test]
		public void Test_IncrementItemInSortedSet()
		{
			stringDoubleMap.ForEach(x =>
				RedisExec(r => r.AddItemToSortedSet(SetId, x.Key, x.Value)));

			var lastItem = stringDoubleMap.Last();
			var response = base.Send<IncrementItemInSortedSetResponse>(
				new IncrementItemInSortedSet
				{
					Id = SetId,
					Item = lastItem.Key,
					IncrementBy = 2
				}, x => x.ResponseStatus);

			Assert.That(response.Score, Is.EqualTo(lastItem.Value + 2));
		}

		[Test]
		public void Test_PopItemWithHighestScoreFromSortedSet()
		{
			stringDoubleMap.ForEach(x =>
				RedisExec(r => r.AddItemToSortedSet(SetId, x.Key, x.Value)));

			var lastItem = stringDoubleMap.Last();
			var response = base.Send<PopItemWithHighestScoreFromSortedSetResponse>(
				new PopItemWithHighestScoreFromSortedSet { Id = SetId }, x => x.ResponseStatus);

			Assert.That(response.Item, Is.EqualTo(lastItem.Key));
		}

		[Test]
		public void Test_PopItemWithLowestScoreFromSortedSet()
		{
			stringDoubleMap.ForEach(x =>
				RedisExec(r => r.AddItemToSortedSet(SetId, x.Key, x.Value)));

			var firstItem = stringDoubleMap.First();
			var response = base.Send<PopItemWithLowestScoreFromSortedSetResponse>(
				new PopItemWithLowestScoreFromSortedSet { Id = SetId }, x => x.ResponseStatus);

			Assert.That(response.Item, Is.EqualTo(firstItem.Key));
		}

		[Test]
		public void Test_RemoveItemFromSortedSet()
		{
			stringDoubleMap.ForEach(x =>
				RedisExec(r => r.AddItemToSortedSet(SetId, x.Key, x.Value)));

			var lastItem = stringDoubleMap.Last();
			var response = base.Send<RemoveItemFromSortedSetResponse>(
				new RemoveItemFromSortedSet { Id = SetId, Item = lastItem.Key }, x => x.ResponseStatus);
			Assert.That(response.Result, Is.True);

			stringDoubleMap.Remove(lastItem.Key);

			var items = RedisExec(r => r.GetAllItemsFromSortedSet(SetId));

			Assert.That(items, Is.EquivalentTo(stringDoubleMap.Keys.ToList()));
		}

		[Test]
		public void Test_RemoveRangeFromSortedSet()
		{
			stringDoubleMap.ForEach(x =>
				RedisExec(r => r.AddItemToSortedSet(SetId, x.Key, x.Value)));

			var lastItem = stringDoubleMap.Last();
			var response = base.Send<RemoveRangeFromSortedSetResponse>(
				new RemoveRangeFromSortedSet { Id = SetId, FromRank = 0, ToRank = 2 }, x => x.ResponseStatus);
			Assert.That(response.ItemsRemovedCount, Is.EqualTo(3));

			var expectedItems = stringDoubleMap.Skip(3).ConvertAll(x => x.Key);
			var items = RedisExec(r => r.GetAllItemsFromSortedSet(SetId));

			Assert.That(items, Is.EquivalentTo(expectedItems));
		}

		[Test]
		public void Test_RemoveRangeFromSortedSetByScore()
		{
			stringDoubleMap.ForEach(x =>
				RedisExec(r => r.AddItemToSortedSet(SetId, x.Key, x.Value)));

			var response = base.Send<RemoveRangeFromSortedSetByScoreResponse>(
				new RemoveRangeFromSortedSetByScore { Id = SetId, FromScore = 1, ToScore = 2 }, x => x.ResponseStatus);
			Assert.That(response.ItemsRemovedCount, Is.EqualTo(2));

			var expectedItems = stringDoubleMap.Skip(2).ConvertAll(x => x.Key);
			var items = RedisExec(r => r.GetAllItemsFromSortedSet(SetId));

			Assert.That(items, Is.EquivalentTo(expectedItems));
		}

		[Test]
		public void Test_SortedSetContainsItem()
		{
			stringDoubleMap.ForEach(x =>
				RedisExec(r => r.AddItemToSortedSet(SetId, x.Key, x.Value)));

			var lastItem = stringDoubleMap.Last();
			var response = base.Send<SortedSetContainsItemResponse>(
				new SortedSetContainsItem { Id = SetId, Item = lastItem.Key }, x => x.ResponseStatus);

			Assert.That(response.Result, Is.True);
		}

		[Test]
		public void Can_StoreIntersectFromSortedSets()
		{
			stringList.ForEach(x => RedisExec(r => r.AddItemToSortedSet(SetId, x)));
			stringList2.ForEach(x => RedisExec(r => r.AddItemToSortedSet(SetId2, x)));

			var response = base.Send<StoreIntersectFromSortedSetsResponse>(
				new StoreIntersectFromSortedSets { Id = SetId3, FromSetIds = { SetId, SetId2 } }, x => x.ResponseStatus);

			var items = RedisExec(r => r.GetAllItemsFromSortedSet(SetId3));

			Assert.That(items, Is.EquivalentTo(new List<string> { "four" }));
		}

		[Test]
		public void Can_StoreUnionFromSortedSets()
		{
			stringList.ForEach(x => RedisExec(r => r.AddItemToSortedSet(SetId, x)));
			stringList2.ForEach(x => RedisExec(r => r.AddItemToSortedSet(SetId2, x)));

			var response = base.Send<StoreUnionFromSortedSetsResponse>(
				new StoreUnionFromSortedSets { Id = SetId3, FromSetIds = { SetId, SetId2 } }, x => x.ResponseStatus);

			var unionList = new List<string>(stringList);
			stringList2.ForEach(x => { if (!unionList.Contains(x)) unionList.Add(x); });

			var items = RedisExec(r => r.GetAllItemsFromSortedSet(SetId3));

			Assert.That(items, Is.EquivalentTo(unionList));
		}

	}
}