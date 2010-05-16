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
using ServiceStack.DesignPatterns.Model;
using ServiceStack.Text;

namespace ServiceStack.Redis
{
	public partial class RedisClient
		: IRedisClient
	{
		public IHasNamed<IRedisHash> Hashes { get; set; }

		internal class RedisClientHashes
			: IHasNamed<IRedisHash>
		{
			private readonly RedisClient client;

			public RedisClientHashes(RedisClient client)
			{
				this.client = client;
			}

			public IRedisHash this[string hashId]
			{
				get
				{
					return new RedisClientHash(client, hashId);
				}
				set
				{
					var hash = this[hashId];
					hash.Clear();
					hash.CopyTo(value.ToArray(), 0);
				}
			}
		}

		public bool SetEntryInHash(string hashId, string key, string value)
		{
			return base.HSet(hashId, key.ToUtf8Bytes(), value.ToUtf8Bytes()) == Success;
		}

		public bool SetEntryInHashIfNotExists(string hashId, string key, string value)
		{
			return base.HSetNX(hashId, key.ToUtf8Bytes(), value.ToUtf8Bytes()) == Success;
		}

		public void SetRangeInHash(string hashId, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
		{
			var keyValuePairsList = keyValuePairs.ToList();
			var keys = new byte[keyValuePairsList.Count][];
			var values = new byte[keyValuePairsList.Count][];

			for (var i = 0; i < keyValuePairsList.Count; i++)
			{
				var kvp = keyValuePairsList[i];
				keys[i] = kvp.Key.ToUtf8Bytes();
				values[i] = kvp.Value.ToUtf8Bytes();
			}

			base.HMSet(hashId, keys, values);
		}

		public int IncrementValueInHash(string hashId, string key, int incrementBy)
		{
			return base.HIncrby(hashId, key.ToUtf8Bytes(), incrementBy);
		}

		public string GetValueFromHash(string hashId, string key)
		{
			return base.HGet(hashId, key.ToUtf8Bytes()).FromUtf8Bytes();
		}

		public bool HashContainsEntry(string hashId, string key)
		{
			return base.HExists(hashId, key.ToUtf8Bytes()) == Success;
		}

		public bool RemoveEntryFromHash(string hashId, string key)
		{
			return base.HDel(hashId, key.ToUtf8Bytes()) == Success;
		}

		public int GetHashCount(string hashId)
		{
			return base.HLen(hashId);
		}

		public List<string> GetHashKeys(string hashId)
		{
			var multiDataList = base.HKeys(hashId);
			return multiDataList.ToStringList();
		}

		public List<string> GetHashValues(string hashId)
		{
			var multiDataList = base.HVals(hashId);
			return multiDataList.ToStringList();
		}

		public Dictionary<string, string> GetAllEntriesFromHash(string hashId)
		{
			var multiDataList = base.HGetAll(hashId);
			var map = new Dictionary<string, string>();

			for (var i = 0; i < multiDataList.Length; i += 2)
			{
				var key = multiDataList[i].FromUtf8Bytes();
				map[key] = multiDataList[i + 1].FromUtf8Bytes();
			}

			return map;
		}

		public List<string> GetValuesFromHash(string hashId, params string[] keys)
		{
			var keyBytes = ConvertToBytes(keys);
			var multiDataList = base.HMGet(hashId, keyBytes);
			return multiDataList.ToStringList();
		}
	}
}