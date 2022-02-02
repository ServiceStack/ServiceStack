//
// https://github.com/ServiceStack/ServiceStack.Redis
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2013 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
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
	internal partial class RedisClientSet
		: IRedisSet
	{
		private readonly RedisClient client;
		private readonly string setId;
		private const int PageLimit = 1000;

		public RedisClientSet(RedisClient client, string setId)
		{
			this.client = client;
			this.setId = setId;
		}

		public IEnumerator<string> GetEnumerator()
		{
			return this.Count <= PageLimit
				? client.GetAllItemsFromSet(setId).GetEnumerator()
				: GetPagingEnumerator();
		}

		public IEnumerator<string> GetPagingEnumerator()
		{
			var skip = 0;
			List<string> pageResults;
			do
			{
				pageResults = client.GetSortedEntryValues(setId, skip, skip + PageLimit - 1);
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
			client.AddItemToSet(setId, item);
		}

		public void Clear()
		{
			client.Remove(setId);
		}

		public bool Contains(string item)
		{
			return client.SetContainsItem(setId, item);
		}

		public void CopyTo(string[] array, int arrayIndex)
		{
			var allItemsInSet = client.GetAllItemsFromSet(setId);
			allItemsInSet.CopyTo(array, arrayIndex);
		}

		public bool Remove(string item)
		{
			client.RemoveItemFromSet(setId, item);
			return true;
		}

		public int Count
		{
			get
			{
				return (int)client.GetSetCount(setId);
			}
		}

		public bool IsReadOnly { get { return false; } }

		public string Id
		{
			get { return this.setId; }
		}

		public List<string> GetRangeFromSortedSet(int startingFrom, int endingAt)
		{
			return client.GetSortedEntryValues(setId, startingFrom, endingAt);
		}

		public HashSet<string> GetAll()
		{
			return client.GetAllItemsFromSet(setId);
		}

		public string Pop()
		{
			return client.PopItemFromSet(setId);
		}

		public void Move(string value, IRedisSet toSet)
		{
			client.MoveBetweenSets(setId, toSet.Id, value);
		}

		private List<string> MergeSetIds(IRedisSet[] withSets)
		{
			var allSetIds = new List<string> { setId };
			allSetIds.AddRange(withSets.ToList().ConvertAll(x => x.Id));
			return allSetIds;
		}

		public HashSet<string> Intersect(params IRedisSet[] withSets)
		{
			var allSetIds = MergeSetIds(withSets);
			return client.GetIntersectFromSets(allSetIds.ToArray());
		}

		public void StoreIntersect(params IRedisSet[] withSets)
		{
			var withSetIds = withSets.ToList().ConvertAll(x => x.Id).ToArray();
			client.StoreIntersectFromSets(setId, withSetIds);
		}

		public HashSet<string> Union(params IRedisSet[] withSets)
		{
			var allSetIds = MergeSetIds(withSets);
			return client.GetUnionFromSets(allSetIds.ToArray());
		}

		public void StoreUnion(params IRedisSet[] withSets)
		{
			var withSetIds = withSets.ToList().ConvertAll(x => x.Id).ToArray();
			client.StoreUnionFromSets(setId, withSetIds);
		}

		public HashSet<string> Diff(IRedisSet[] withSets)
		{
			var withSetIds = withSets.ToList().ConvertAll(x => x.Id).ToArray();
			return client.GetDifferencesFromSet(setId, withSetIds);
		}

		public void StoreDiff(IRedisSet fromSet, params IRedisSet[] withSets)
		{
			var withSetIds = withSets.ToList().ConvertAll(x => x.Id).ToArray();
			client.StoreDifferencesFromSet(setId, fromSet.Id, withSetIds);
		}

		public string GetRandomEntry()
		{
			return client.GetRandomItemFromSet(setId);
		}
	}
}