//
// http://code.google.com/p/servicestack/wiki/ServiceStackRedis
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2010 Liquidbit Ltd.
//
// Licensed under the same terms of Redis and ServiceStack: new BSD license.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ServiceStack.Redis.Generic
{
	internal class RedisClientList<T>
		: IRedisList<T>
	{
		private readonly RedisTypedClient<T> client;
		private readonly string listId;
		private const int PageLimit = 1000;

		public RedisClientList(RedisTypedClient<T> client, string listId)
		{
			this.listId = listId;
			this.client = client;
		}

		public string Id
		{
			get { return listId; }
		}

		public IEnumerator<T> GetEnumerator()
		{
			return this.Count <= PageLimit
			       	? client.GetAllFromList(this).GetEnumerator()
			       	: GetPagingEnumerator();
		}

		public IEnumerator<T> GetPagingEnumerator()
		{
			var skip = 0;
			List<T> pageResults;
			do
			{
				pageResults = client.GetRangeFromList(this, skip, PageLimit);
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
			client.AddToList(this, item);
		}

		public void Clear()
		{
			client.RemoveAllFromList(this);
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
			var allItemsInList = client.GetAllFromList(this);
			allItemsInList.CopyTo(array, arrayIndex);
		}

		public bool Remove(T item)
		{
			return client.RemoveValueFromList(this, item) > 0;
		}

		public int Count
		{
			get
			{
				return client.GetListCount(this);
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
			get { return client.GetItemFromList(this, index); }
			set { client.SetItemInList(this, index, value); }
		}

		public List<T> GetAll()
		{
			return client.GetAllFromList(this);
		}

		public List<T> GetRange(int startingFrom, int endingAt)
		{
			return client.GetRangeFromList(this, startingFrom, endingAt);
		}

		public List<T> GetRangeFromSortedList(int startingFrom, int endingAt)
		{
			return client.SortList(this, startingFrom, endingAt);
		}

		public void RemoveAll()
		{
			client.RemoveAllFromList(this);
		}

		public void Trim(int keepStartingFrom, int keepEndingAt)
		{
			client.TrimList(this, keepStartingFrom, keepEndingAt);
		}

		public int RemoveValue(T value)
		{
			return client.RemoveValueFromList(this, value);
		}

		public int RemoveValue(T value, int noOfMatches)
		{
			return client.RemoveValueFromList(this, value, noOfMatches);
		}

		public void Append(T value)
		{
			Add(value);
		}

		public void Prepend(T value)
		{
			client.PrependToList(this, value);
		}

		public T RemoveStart()
		{
			return client.RemoveStartFromList(this);
		}

		public T BlockingRemoveStart(TimeSpan? timeOut)
		{
			return client.BlockingRemoveStartFromList(this, timeOut);
		}

		public T RemoveEnd()
		{
			return client.RemoveEndFromList(this);
		}

		public void Enqueue(T value)
		{
			client.EnqueueOnList(this, value);
		}

		public T Dequeue()
		{
			return client.DequeueFromList(this);
		}

		public T BlockingDequeue(TimeSpan? timeOut)
		{
			return client.BlockingDequeueFromList(this, timeOut);
		}

		public void Push(T value)
		{
			client.PushToList(this, value);
		}

		public T Pop()
		{
			return client.PopFromList(this);
		}

		public T BlockingPop(TimeSpan? timeOut)
		{
			return client.BlockingPopFromList(this, timeOut);
		}

		public T PopAndPush(IRedisList<T> toList)
		{
			return client.PopAndPushBetweenLists(this, toList);
		}
	}
}