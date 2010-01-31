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

namespace ServiceStack.Redis
{
	/// <summary>
	/// Wrap the common redis set operations under a ICollection[string] interface.
	/// </summary>
	internal class RedisClientSet
		: ICollection<string>
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
				? client.GetAllFromSet(setId).GetEnumerator()
				: GetPagingEnumerator();
		}

		public IEnumerator<string> GetPagingEnumerator()
		{
			var skip = 0;
			List<string> pageResults;
			do
			{
				pageResults = client.GetRangeFromSortedSet(setId, skip, PageLimit);
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
			client.AddToSet(setId, item);
		}

		public void Clear()
		{
			client.Remove(setId);
		}

		public bool Contains(string item)
		{
			return client.SetContainsValue(setId, item);
		}

		public void CopyTo(string[] array, int arrayIndex)
		{
			var allItemsInSet = client.GetAllFromSet(setId);
			allItemsInSet.CopyTo(array, arrayIndex);
		}

		public bool Remove(string item)
		{
			client.RemoveFromSet(setId, item);
			return true;
		}

		public int Count
		{
			get
			{
				return client.GetSetCount(setId);
			}
		}

		public bool IsReadOnly { get { return false; } }

	}
}