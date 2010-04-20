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
using System.Linq;

namespace ServiceStack.Redis
{
	/// <summary>
	/// Wrap the common redis set operations under a ICollection[string] interface.
	/// </summary>
	internal class RedisClientHash
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
			return client.GetAllFromHash(hashId).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Add(KeyValuePair<string, string> item)
		{
			client.SetItemInHash(hashId, item.Key, item.Value);
		}

		public bool AddIfNotExists(KeyValuePair<string, string> item)
		{
			return client.SetItemInHashIfNotExists(hashId, item.Key, item.Value);
		}

		public void AddRange(IEnumerable<KeyValuePair<string, string>> items)
		{
			client.SetRangeInHash(hashId, items);
		}

		public int IncrementValue(string key, int incrementBy)
		{
			return client.IncrementItemInHash(hashId, key, incrementBy);
		}

		public void Clear()
		{
			client.Remove(hashId);
		}

		public bool Contains(KeyValuePair<string, string> item)
		{
			var itemValue = client.GetItemFromHash(hashId, item.Key);
			return itemValue == item.Value;
		}

		public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
		{
			var allItemsInHash = client.GetAllFromHash(hashId);
			
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
				client.RemoveFromHash(hashId, item.Key);
				return true;
			}
			return false;
		}

		public int Count
		{
			get { return client.GetHashCount(hashId); }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool ContainsKey(string key)
		{
			return client.HashContainsKey(hashId, key);
		}

		public void Add(string key, string value)
		{
			client.SetItemInHash(hashId, key, value);
		}

		public bool Remove(string key)
		{
			return client.RemoveFromHash(hashId, key);
		}

		public bool TryGetValue(string key, out string value)
		{
			value = client.GetItemFromHash(hashId, key);
			return value != null;
		}

		public string this[string key]
		{
			get
			{
				return client.GetItemFromHash(hashId, key);
			}
			set
			{
				client.SetItemInHash(hashId, key, value);
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