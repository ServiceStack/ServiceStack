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
	internal partial class RedisClientHash
		: IRedisHash
	{
		private readonly RedisClient client;
		private readonly string hashId;

		public RedisClientHash(RedisClient client, string hashId)
		{
			this.client = client;
			this.hashId = hashId;
		}

		public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
		{
			return client.GetAllEntriesFromHash(hashId).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Add(KeyValuePair<string, string> item)
		{
			client.SetEntryInHash(hashId, item.Key, item.Value);
		}

		public bool AddIfNotExists(KeyValuePair<string, string> item)
		{
			return client.SetEntryInHashIfNotExists(hashId, item.Key, item.Value);
		}

		public void AddRange(IEnumerable<KeyValuePair<string, string>> items)
		{
			client.SetRangeInHash(hashId, items);
		}

		public long IncrementValue(string key, int incrementBy)
		{
			return client.IncrementValueInHash(hashId, key, incrementBy);
		}

		public void Clear()
		{
			client.Remove(hashId);
		}

		public bool Contains(KeyValuePair<string, string> item)
		{
			var itemValue = client.GetValueFromHash(hashId, item.Key);
			return itemValue == item.Value;
		}

		public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
		{
			var allItemsInHash = client.GetAllEntriesFromHash(hashId);
			
			var i = arrayIndex;
			foreach (var item in allItemsInHash)
			{
				if (i >= array.Length) return;
				array[i++] = item;
			}
		}

		public bool Remove(KeyValuePair<string, string> item)
		{
			if (Contains(item))
			{
				client.RemoveEntryFromHash(hashId, item.Key);
				return true;
			}
			return false;
		}

		public int Count
		{
			get { return (int)client.GetHashCount(hashId); }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool ContainsKey(string key)
		{
			return client.HashContainsEntry(hashId, key);
		}

		public void Add(string key, string value)
		{
			client.SetEntryInHash(hashId, key, value);
		}

		public bool Remove(string key)
		{
			return client.RemoveEntryFromHash(hashId, key);
		}

		public bool TryGetValue(string key, out string value)
		{
			value = client.GetValueFromHash(hashId, key);
			return value != null;
		}

		public string this[string key]
		{
			get
			{
				return client.GetValueFromHash(hashId, key);
			}
			set
			{
				client.SetEntryInHash(hashId, key, value);
			}
		}

		public ICollection<string> Keys
		{
			get
			{
				return client.GetHashKeys(hashId);
			}
		}

		public ICollection<string> Values
		{
			get
			{
				return client.GetHashValues(hashId);
			}
		}

		public string Id
		{
			get { return hashId; }
		}
	}
}