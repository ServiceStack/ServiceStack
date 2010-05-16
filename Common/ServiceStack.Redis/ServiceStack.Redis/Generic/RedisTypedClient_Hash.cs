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
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Text;

namespace ServiceStack.Redis.Generic
{
	internal partial class RedisTypedClient<T>
	{
		public IRedisHash<TKey, T> GetHash<TKey>(string hashId)
		{
			return new RedisClientHash<TKey, T>(this, hashId);
		}

		public bool HashContainsEntry<TKey>(IRedisHash<TKey, T> hash, TKey key)
		{
			return client.HashContainsEntry(hash.Id, key.SerializeToString());
		}

		public bool SetEntryInHash<TKey>(IRedisHash<TKey, T> hash, TKey key, T value)
		{
			return client.SetEntryInHash(hash.Id, key.SerializeToString(), value.SerializeToString());
		}

		public bool SetEntryInHashIfNotExists<TKey>(IRedisHash<TKey, T> hash, TKey key, T value)
		{
			return client.SetEntryInHashIfNotExists(hash.Id, key.SerializeToString(), value.SerializeToString());
		}

		public void SetRangeInHash<TKey>(IRedisHash<TKey, T> hash, IEnumerable<KeyValuePair<TKey, T>> keyValuePairs)
		{
			var stringKeyValuePairs = keyValuePairs.ToList().ConvertAll(
				x => new KeyValuePair<string, string>(x.Key.SerializeToString(), x.Value.SerializeToString()));

			client.SetRangeInHash(hash.Id, stringKeyValuePairs);
		}

		public T GetValueFromHash<TKey>(IRedisHash<TKey, T> hash, TKey key)
		{
			return client.GetValueFromHash(hash.Id, key.SerializeToString())
				.DeserializeFromString<T>();
		}

		public bool RemoveEntryFromHash<TKey>(IRedisHash<TKey, T> hash, TKey key)
		{
			return client.RemoveEntryFromHash(hash.Id, key.SerializeToString());
		}

		public int GetHashCount<TKey>(IRedisHash<TKey, T> hash)
		{
			return client.GetHashCount(hash.Id);
		}

		public List<TKey> GetHashKeys<TKey>(IRedisHash<TKey, T> hash)
		{
			return client.GetHashKeys(hash.Id).ConvertEachTo<TKey>();
		}

		public List<T> GetHashValues<TKey>(IRedisHash<TKey, T> hash)
		{
			return client.GetHashValues(hash.Id).ConvertEachTo<T>();
		}

		public Dictionary<TKey, T> GetAllEntriesFromHash<TKey>(IRedisHash<TKey, T> hash)
		{
			return client.GetAllEntriesFromHash(hash.Id).ConvertEachTo<TKey, T>();
		}

	}
}