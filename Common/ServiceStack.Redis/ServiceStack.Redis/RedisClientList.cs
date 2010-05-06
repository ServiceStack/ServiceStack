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

namespace ServiceStack.Redis
{
	/// <summary>
	/// Wrap the common redis list operations under a IList[string] interface.
	/// </summary>
	internal class RedisClientList
		: IRedisList
	{
		private readonly RedisClient client;
		private readonly string listId;
		private const int PageLimit = 1000;

		public RedisClientList(RedisClient client, string listId)
		{
			this.listId = listId;
			this.client = client;
		}

		public string Id
		{
			get { return listId; }
		}

		public IEnumerator<string> GetEnumerator()
		{
			return this.Count <= PageLimit
				? client.GetAllFromList(listId).GetEnumerator()
				: GetPagingEnumerator();
		}

		public IEnumerator<string> GetPagingEnumerator()
		{
			var skip = 0;
			List<string> pageResults;
			do
			{
				pageResults = client.GetRangeFromList(listId, skip, skip + PageLimit - 1);
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
			client.AddToList(listId, item);
		}

		public void Clear()
		{
			client.RemoveAllFromList(listId);
		}

		public bool Contains(string item)
		{
			//TODO: replace with native implementation when exists
			foreach (var existingItem in this)
			{
				if (existingItem == item) return true;
			}
			return false;
		}

		public void CopyTo(string[] array, int arrayIndex)
		{
			var allItemsInList = client.GetAllFromList(listId);
			allItemsInList.CopyTo(array, arrayIndex);
		}

		public bool Remove(string item)
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

		public int IndexOf(string item)
		{
			//TODO: replace with native implementation when exists
			var i = 0;
			foreach (var existingItem in this)
			{
				if (existingItem == item) return i;
				i++;
			}
			return -1;
		}

		public void Insert(int index, string item)
		{
			//TODO: replace with implementation involving creating on new temp list then replacing
			//otherwise wait for native implementation
			throw new NotImplementedException();
		}

		public void RemoveAt(int index)
		{
			//TODO: replace with native implementation when one exists
			var markForDelete = Guid.NewGuid().ToString();
			client.SetItemInList(listId, index, markForDelete);
			client.RemoveValueFromList(listId, markForDelete);
		}

		public string this[int index]
		{
			get { return client.GetItemFromList(listId, index); }
			set { client.SetItemInList(listId, index, value); }
		}

		public List<string> GetAll()
		{
			return client.GetAllFromList(listId);
		}

		public List<string> GetRange(int startingFrom, int endingAt)
		{
			return client.GetRangeFromList(listId, startingFrom, endingAt);
		}

		public List<string> GetRangeFromSortedList(int startingFrom, int endingAt)
		{
			return client.GetRangeFromSortedList(listId, startingFrom, endingAt);
		}

		public void RemoveAll()
		{
			client.RemoveAllFromList(listId);
		}

		public void Trim(int keepStartingFrom, int keepEndingAt)
		{
			client.TrimList(listId, keepStartingFrom, keepEndingAt);
		}

		public int RemoveValue(string value)
		{
			return client.RemoveValueFromList(listId, value);
		}

		public int RemoveValue(string value, int noOfMatches)
		{
			return client.RemoveValueFromList(listId, value, noOfMatches);
		}

		public void Append(string value)
		{
			Add(value);
		}

		public string RemoveStart()
		{
			return client.RemoveStartFromList(listId);
		}

		public string BlockingRemoveStart(TimeSpan? timeOut)
		{
			return client.BlockingRemoveStartFromList(listId, timeOut);
		}

		public string RemoveEnd()
		{
			return client.RemoveEndFromList(listId);
		}

		public void Enqueue(string value)
		{
			client.EnqueueOnList(listId, value);
		}

		public void Prepend(string value)
		{
			client.PrependToList(listId, value);
		}

		public void Push(string value)
		{
			client.PushToList(listId, value);
		}

		public string Pop()
		{
			return client.PopFromList(listId);
		}

		public string BlockingPop(TimeSpan? timeOut)
		{
			return client.BlockingPopFromList(listId, timeOut);
		}

		public string Dequeue()
		{
			return client.DequeueFromList(listId);
		}

		public string BlockingDequeue(TimeSpan? timeOut)
		{
			return client.BlockingDequeueFromList(listId, timeOut);
		}

		public string PopAndPush(IRedisList toList)
		{
			return client.PopAndPushBetweenLists(listId, toList.Id);
		}
	}
}