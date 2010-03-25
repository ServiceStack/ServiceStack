//
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2010 Liquidbit Ltd.
//
// Licensed under the same terms of reddis and ServiceStack: new BSD license.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ServiceStack.Redis
{
	/// <summary>
	/// Wrap the common redis set operations under a ICollection[string] interface.
	/// </summary>
	internal class RedisClientSortedSet
		: IRedisClientSortedSet
	{
		private readonly RedisClient client;
		private readonly string setId;
		private const int PageLimit = 1000;

		public RedisClientSortedSet(RedisClient client, string setId)
		{
			this.client = client;
			this.setId = setId;
		}

		public IEnumerator<string> GetEnumerator()
		{
			return this.Count <= PageLimit
				? client.GetAllFromSortedSet(setId).GetEnumerator()
				: GetPagingEnumerator();
		}

		public IEnumerator<string> GetPagingEnumerator()
		{
			var skip = 0;
			List<string> pageResults;
			do
			{
				pageResults = client.GetRangeFromSortedSet(setId, skip, skip + PageLimit - 1);
				foreach (var result in pageResults)
				{
					yield return result;
				}
				skip += PageLimit;
			} while (pageResults.Count == PageLimit);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Add(string item)
		{
			client.AddToSortedSet(setId, item);
		}

		public void Clear()
		{
			client.Remove(setId);
		}

		public bool Contains(string item)
		{
			return client.SortedSetContainsValue(setId, item);
		}

		public void CopyTo(string[] array, int arrayIndex)
		{
			var allItemsInSet = client.GetAllFromSortedSet(setId);
			allItemsInSet.CopyTo(array, arrayIndex);
		}

		public bool Remove(string item)
		{
			client.RemoveFromSortedSet(setId, item);
			return true;
		}

		public int Count
		{
			get
			{
				return client.GetSortedSetCount(setId);
			}
		}

		public bool IsReadOnly { get { return false; } }

		public string Id
		{
			get { return this.setId; }
		}

		public List<string> GetAll()
		{
			return client.GetAllFromSortedSet(setId);
		}

		public List<string> GetRange(int startingRank, int endingRank)
		{
			return client.GetRangeFromSortedSet(setId, startingRank, endingRank);
		}

		public List<string> GetRangeByScore(double startingFrom, double toScore)
		{
			return GetRangeByScore(startingFrom, toScore, null, null);
		}

		public List<string> GetRangeByScore(double startingFrom, double toScore, int? skip, int? take)
		{
			return client.GetRangeFromSortedSetByLowestScore(setId, startingFrom, toScore, skip, take);
		}

		public void RemoveRange(int startingFrom, int toRank)
		{
			client.RemoveRangeFromSortedSet(setId, startingFrom, toRank);
		}

		public void RemoveRangeByScore(double fromScore, double toScore)
		{
			client.RemoveRangeFromSortedSetByScore(setId, fromScore, toScore);
		}

		public void StoreFromIntersect(params IRedisClientSortedSet[] ofSets)
		{
			client.StoreIntersectFromSets(setId, ofSets.GetIds());
		}

		public void StoreFromUnion(params IRedisClientSortedSet[] ofSets)
		{
			client.StoreUnionFromSets(setId, ofSets.GetIds());
		}

		public int GetItemIndex(string value)
		{
			return client.GetItemIndexInSortedSet(setId, value);
		}

		public double GetItemScore(string value)
		{
			return client.GetItemScoreInSortedSet(setId, value);
		}

		public string PopItemWithLowestScore()
		{
			return client.PopFromSortedSetItemWithLowestScore(setId);
		}

		public string PopItemWithHighestScore()
		{
			return client.PopFromSortedSetItemWithHighestScore(setId);
		}

		public void IncrementItemScore(string value, double incrementByScore)
		{
			client.IncrementItemInSortedSet(setId, incrementByScore, value);
		}

	}
}