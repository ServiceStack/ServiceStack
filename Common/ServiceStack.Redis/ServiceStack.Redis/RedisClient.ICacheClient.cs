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
using System.Text;
using ServiceStack.CacheAccess;
using ServiceStack.Text;

namespace ServiceStack.Redis
{
	public partial class RedisClient
		: ICacheClient
	{
		public void RemoveAll(IEnumerable<string> keys)
		{
			Remove(keys.ToArray());
		}

		public T Get<T>(string key)
		{
			return typeof(T) == typeof(byte[])
				? (T)(object)base.Get(key)
				: TypeSerializer.DeserializeFromString<T>(GetString(key));
		}

		public long Increment(string key, uint amount)
		{
			return IncrementBy(key, (int)amount);
		}

		public long Decrement(string key, uint amount)
		{
			return DecrementBy(key, (int)amount);
		}

		public bool Add<T>(string key, T value)
		{
			var bytesValue = value as byte[];
			if (bytesValue != null)
			{
				return base.SetNX(key, bytesValue) == Success;
			}

			var valueString = TypeSerializer.SerializeToString(value);
			return SetIfNotExists(key, valueString);
		}

		public bool Set<T>(string key, T value)
		{
			var bytesValue = value as byte[];
			if (bytesValue != null)
			{
				base.Set(key, bytesValue);
				return true;
			}

			var valueString = TypeSerializer.SerializeToString(value);
			SetString(key, valueString);
			return true;
		}

		public bool Replace<T>(string key, T value)
		{
			var exists = ContainsKey(key);
			if (!exists) return false;

			var bytesValue = value as byte[];
			if (bytesValue != null)
			{
				base.Set(key, bytesValue);
				return true;
			}

			SetString(key, TypeSerializer.SerializeToString(value));
			return true;
		}

		public bool Add<T>(string key, T value, DateTime expiresAt)
		{
			if (Add(key, value))
			{
				ExpireKeyAt(key, expiresAt);
				return true;
			}
			return false;
		}

		public bool Set<T>(string key, T value, TimeSpan expiresIn)
		{
			var bytesValue = value as byte[];
			if (bytesValue != null)
			{
				base.SetEx(key, (int)expiresIn.TotalSeconds, bytesValue);
				return true;
			}

			var valueString = TypeSerializer.SerializeToString(value);
			SetString(key, valueString, expiresIn);
			return true;
		}

		public bool Set<T>(string key, T value, DateTime expiresAt)
		{
			Set(key, value);
			ExpireKeyAt(key, expiresAt);
			return true;
		}

		public bool Replace<T>(string key, T value, DateTime expiresAt)
		{
			if (Replace(key, value))
			{
				ExpireKeyAt(key, expiresAt);
				return true;
			}
			return false;
		}

		public bool Add<T>(string key, T value, TimeSpan expiresIn)
		{
			if (Add(key, value))
			{
				ExpireKeyIn(key, expiresIn);
				return true;
			}
			return false;
		}

		public bool Replace<T>(string key, T value, TimeSpan expiresIn)
		{
			if (Replace(key, value))
			{
				ExpireKeyIn(key, expiresIn);
				return true;
			}
			return false;
		}

		public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys)
		{
			var keysArray = keys.ToArray();
			var keyValues = MGet(keysArray);
			var results = new Dictionary<string, T>();
			var isBytes = typeof(T) == typeof(byte[]);

			var i = 0;
			foreach (var keyValue in keyValues)
			{
				var key = keysArray[i++];

				if (keyValue == null)
				{
					results[key] = default(T);
					continue;
				}

				if (isBytes)
				{
					results[key] = (T)(object)keyValue;
				}
				else
				{
					var keyValueString = Encoding.UTF8.GetString(keyValue);
					results[key] = TypeSerializer.DeserializeFromString<T>(keyValueString);
				}
			}
			return results;
		}

		public void SetAll<T>(IDictionary<string, T> values)
		{
			foreach (var entry in values)
			{
				Set(entry.Key, entry.Value);
			}
		}
	}


}