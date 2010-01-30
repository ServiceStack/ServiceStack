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

namespace ServiceStack.Redis.Generic
{
	/// <summary>
	/// Wrap the common redis set operations under a ICollection[string] interface.
	/// </summary>
	internal class RedisClientSet<T>
		: ICollection<T>
	{
		private readonly RedisGenericClient<T> client;
		private readonly string setId;
		private const int PageLimit = 1000;

		public RedisClientSet(RedisGenericClient<T> client, string setId)
		{
			this.client = client;
			this.setId = setId;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return this.Count <= PageLimit
					? client.GetAllFromSet(setId).GetEnumerator()
					: GetPagingEnumerator();
		}

		public IEnumerator<T> GetPagingEnumerator()
		{
			var skip = 0;
			List<T> pageResults;
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

		public void Add(T item)
		{
			client.AddToSet(setId, item);
		}

		public void Clear()
		{
			client.Remove(setId);
		}

		public bool Contains(T item)
		{
			return client.SetContainsValue(setId, item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			foreach (var item in array)
			{
				client.AddToSet(setId, item);
			}
		}

		public bool Remove(T item)
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