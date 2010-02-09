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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.CacheAccess;
using ServiceStack.Text;

namespace ServiceStack.Redis
{
	public class RedisCacheClient 
		: RedisClient, ICacheClient 
	{
		public RedisCacheClient(string host, int port) 
			: base(host, port)
		{
		}

		public RedisCacheClient()
		{
		}

		public void RemoveAll(IEnumerable<string> keys)
		{
			Remove(keys.ToArray());
		}

		public new object Get(string key)
		{
			return base.Get(key);
		}

		public T Get<T>(string key)
		{
			return TypeSerializer.DeserializeFromString<T>(GetString(key));
		}

		public long Increment(string key, uint amount)
		{
			return IncrementBy(key, (int) amount);
		}

		public long Decrement(string key, uint amount)
		{
			return DecrementBy(key, (int) amount);
		}

		public bool Add(string key, object value)
		{
			var valueString = TypeSerializer.SerializeToString(value);
			return SetIfNotExists(key, valueString);
		}

		public bool Set(string key, object value)
		{
			var valueString = TypeSerializer.SerializeToString(value);
			SetString(key, valueString);
			return true;
		}

		public bool Replace(string key, object value)
		{
			var exists = ContainsKey(key);
			if (!exists) return false;
			SetString(key, TypeSerializer.SerializeToString(value));
			return true;
		}

		public bool Add(string key, object value, DateTime expiresAt)
		{
			if (Add(key, value))
			{
				ExpireKeyAt(key, expiresAt);
				return true;
			}
			return false;
		}

		public bool Set(string key, object value, DateTime expiresAt)
		{
			Set(key, value);
			ExpireKeyAt(key, expiresAt);
			return true;
		}

		public bool Replace(string key, object value, DateTime expiresAt)
		{
			if (Replace(key, value))
			{
				ExpireKeyAt(key, expiresAt);
				return true;
			}
			return false;
		}

		public IDictionary<string, object> GetAll(IEnumerable<string> keys)
		{
			var keysArray = keys.ToArray();
			var keyValues = MGet(keysArray);
			var results = new Dictionary<string, object>();

			var i = 0;
			foreach (var keyValue in keyValues)
			{
				var key = keysArray[i++];
				results[key] = keyValue;
			}
			return results;
		}

		public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys)
		{
			var keysArray = keys.ToArray();
			var keyValues = MGet(keysArray);
			var results = new Dictionary<string, T>();

			var i = 0;
			foreach (var keyValue in keyValues)
			{
				var key = keysArray[i++];
				var keyValueString = Encoding.UTF8.GetString(keyValue);
				results[key] = TypeSerializer.DeserializeFromString<T>(keyValueString);
			}
			return results;
		}

		public T Get<T>(string key, out ulong lastModifiedValue)
		{
			throw new NotImplementedException();
		}

		public bool CheckAndSet(string key, object value, ulong lastModifiedValue)
		{
			throw new NotImplementedException();
		}

		public bool CheckAndSet(string key, object value, ulong lastModifiedValue, DateTime expiresAt)
		{
			throw new NotImplementedException();
		}

		public IDictionary<string, object> GetAll(IEnumerable<string> keys, out IDictionary<string, ulong> lastModifiedValues)
		{
			throw new NotImplementedException();
		}
	}

}