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
using System.Text;

namespace ServiceStack.Redis.Generic
{
	/// <summary>
	/// Wrap the common redis list operations under a IList[string] interface.
	/// </summary>
	internal class RedisClientList<T>
		: IList<T>
	{
		private readonly RedisGenericClient<T> client;
		private readonly string listId;
		private const int PageLimit = 1000;

		public RedisClientList(RedisGenericClient<T> client, string listId)
		{
			this.listId = listId;
			this.client = client;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return this.Count <= PageLimit
			       	? client.GetAllFromList(listId).GetEnumerator()
			       	: GetPagingEnumerator();
		}

		public IEnumerator<T> GetPagingEnumerator()
		{
			var skip = 0;
			List<T> pageResults;
			do
			{
				pageResults = client.GetRangeFromList(listId, skip, PageLimit);
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
			client.AddToList(listId, item);
		}

		public void Clear()
		{
			client.RemoveAllFromList(listId);
		}

		public bool Contains(T item)
		{
			//TODO: replace with native implementation when exists
			foreach (var existingItem in this)
			{
				if (Equals(existingItem, item)) return true;
			}
			return false;
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			if (arrayIndex == 0)
			{
				for (var i=array.Length - 1; i >= 0; i--)
				{
					client.PrependToList(listId, array[i]);
				}
			}
			else if (arrayIndex == this.Count)
			{
				foreach (var item in array)
				{
					client.AddToList(listId, item);
				}
			}
			else
			{
				//TODO: replace with implementation involving creating on new temp list then replacing
				//otherwise wait for native implementation
				throw new NotImplementedException();
			}
		}

		public bool Remove(T item)
		{
			return client.RemoveValueFromList(listId, item) > 0;
		}

		public int Count
		{
			get
			{
				return client.GetListCount(listId);
			}
		}

		public bool IsReadOnly { get { return false; } }

		public int IndexOf(T item)
		{
			//TODO: replace with native implementation when exists
			var i = 0;
			foreach (var existingItem in this)
			{
				if (Equals(existingItem, item)) return i;
				i++;
			}
			return -1;
		}

		public void Insert(int index, T item)
		{
			//TODO: replace with implementation involving creating on new temp list then replacing
			//otherwise wait for native implementation
			throw new NotImplementedException();
		}

		public void RemoveAt(int index)
		{
			//TODO: replace with native implementation when one exists
			var markForDelete = Guid.NewGuid().ToString();
			client.NativeClient.LSet(listId, index, Encoding.UTF8.GetBytes(markForDelete));

			const int removeAll = 0;
			client.NativeClient.LRem(listId, removeAll, Encoding.UTF8.GetBytes(markForDelete));
		}

		public T this[int index]
		{
			get { return client.GetItemFromList(listId, index); }
			set { client.SetItemInList(listId, index, value); }
		}
	}
}