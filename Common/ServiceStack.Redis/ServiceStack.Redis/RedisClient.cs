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
using System.Text;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Utils;
using ServiceStack.Redis.Generic;
using ServiceStack.Text;

namespace ServiceStack.Redis
{
	/// <summary>
	/// The client wraps the native redis operations into a more readable c# API.
	/// 
	/// Where possible these operations are also exposed in common c# interfaces, 
	/// e.g. RedisClient.Lists => IList[string]
	///		 RedisClient.Sets => ICollection[string]
	/// </summary>
	public partial class RedisClient
		: RedisNativeClient, IRedisClient
	{
		public RedisClient()
		{
			Init();
		}

		public RedisClient(string host)
			: base(host)
		{
			Init();
		}

		public RedisClient(string host, int port)
			: base(host, port)
		{
			Init();
		}

		public void Init()
		{
			this.Lists = new RedisClientLists(this);
			this.Sets = new RedisClientSets(this);
			this.SortedSets = new RedisClientSortedSets(this);
			this.Hashes = new RedisClientHashes(this);
		}

		public string this[string key]
		{
			get { return GetString(key); }
			set { SetString(key, value); }
		}

		public string GetTypeSequenceKey<T>()
		{
			return "seq:" + typeof(T).Name;
		}

		public string GetTypeIdsSetKey<T>()
		{
			return "ids:" + typeof(T).Name;
		}

		public List<string> AllKeys
		{
			get
			{
				return GetKeys("*");
			}
		}

		public void SetString(string key, string value)
		{
			var bytesValue = value != null
				? value.ToUtf8Bytes()
				: null;

			Set(key, bytesValue);
		}

		public bool SetIfNotExists(string key, string value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			return SetNX(key, value.ToUtf8Bytes()) == Success;
		}

		public string GetString(string key)
		{
			var bytes = Get(key);
			return bytes == null
				? null
				: bytes.FromUtf8Bytes();
		}

		public string GetAndSetString(string key, string value)
		{
			return GetSet(key, value.ToUtf8Bytes()).FromUtf8Bytes();
		}

		public bool ContainsKey(string key)
		{
			return Exists(key) == Success;
		}

		public bool Remove(string key)
		{
			return Del(key) == Success;
		}

		public bool Remove(params string[] keys)
		{
			if (keys.Length == 0) return false;

			return Del(keys) == Success;
		}

		public int Increment(string key)
		{
			return Incr(key);
		}

		public int IncrementBy(string key, int count)
		{
			return IncrBy(key, count);
		}

		public int Decrement(string key)
		{
			return Decr(key);
		}

		public int DecrementBy(string key, int count)
		{
			return DecrBy(key, count);
		}

		public string NewRandomKey()
		{
			return RandomKey();
		}

		public bool ExpireKeyIn(string key, TimeSpan expireIn)
		{
			return Expire(key, (int)expireIn.TotalSeconds) == Success;
		}

		public bool ExpireKeyAt(string key, DateTime dateTime)
		{
			return ExpireAt(key, dateTime.ToUnixTime()) == Success;
		}

		public TimeSpan GetTimeToLive(string key)
		{
			return TimeSpan.FromSeconds(Ttl(key));
		}

		public IRedisTypedClient<T> GetTypedClient<T>()
		{
			return new RedisTypedClient<T>(this);
		}

		public IRedisTransaction CreateTransaction()
		{
			return new RedisTransaction(this);
		}

		public List<string> GetKeys(string pattern)
		{
			var hasBug = this.ServerVersion.CompareTo("1.2.6") <= 0;
			if (hasBug)
			{
				var spaceDelimitedKeys = KeysV126(pattern).FromUtf8Bytes();
				return spaceDelimitedKeys.IsNullOrEmpty()
					? new List<string>()
					: new List<string>(spaceDelimitedKeys.Split(' '));
			}

			var multiDataList = Keys(pattern);
			return multiDataList.ToStringList();
		}

		public List<string> GetKeyValues(List<string> keys)
		{
			var resultBytesArray = MGet(keys.ToArray());

			var results = new List<string>();
			foreach (var resultBytes in resultBytesArray)
			{
				if (resultBytes == null) continue;

				var resultString = resultBytes.FromUtf8Bytes();
				results.Add(resultString);
			}

			return results;
		}

		public List<T> GetKeyValues<T>(List<string> keys)
		{
			if (keys == null) throw new ArgumentNullException("keys");
			if (keys.Count == 0) return new List<T>();

			var resultBytesArray = MGet(keys.ToArray());

			var results = new List<T>();
			foreach (var resultBytes in resultBytesArray)
			{
				if (resultBytes == null) continue;

				var resultString = resultBytes.FromUtf8Bytes();
				var result = TypeSerializer.DeserializeFromString<T>(resultString);
				results.Add(result);
			}

			return results;
		}


		#region IBasicPersistenceProvider

		public T GetById<T>(object id) where T : class, new()
		{
			var key = IdUtils.CreateUrn<T>(id);
			var valueString = this.GetString(key);
			var value = TypeSerializer.DeserializeFromString<T>(valueString);
			return value;
		}

		public IList<T> GetByIds<T>(ICollection ids)
			where T : class, new()
		{
			var keys = new List<string>();
			foreach (var id in ids)
			{
				var key = IdUtils.CreateUrn<T>(id);
				keys.Add(key);
			}

			return GetKeyValues<T>(keys);
		}

		public IList<T> GetAll<T>()
			where T : class, new()
		{
			var typeIdsSetKy = this.GetTypeIdsSetKey<T>();
			var allKeys = this.GetAllFromSet(typeIdsSetKy);
			return GetKeyValues<T>(allKeys.ToList());
		}

		public T Store<T>(T entity)
			where T : class, new()
		{
			var urnKey = entity.CreateUrn();
			var valueString = TypeSerializer.SerializeToString(entity);

			var typeIdsSetKy = this.GetTypeIdsSetKey<T>();
			this.AddToSet(typeIdsSetKy, urnKey);

			this.SetString(urnKey, valueString);

			return entity;
		}

		public void StoreAll<TEntity>(IEnumerable<TEntity> entities)
			where TEntity : class, new()
		{
			if (entities == null) return;

			foreach (var entity in entities)
			{
				Store(entity);
			}
		}

		public void Delete<T>(T entity)
			where T : class, new()
		{
			var urnKey = entity.CreateUrn();
			this.Remove(urnKey);
		}

		public void DeleteById<T>(object id) where T : class, new()
		{
			var urnKey = IdUtils.CreateUrn<T>(id);

			var typeIdsSetKy = this.GetTypeIdsSetKey<T>();
			this.RemoveFromSet(typeIdsSetKy, urnKey);

			this.Remove(urnKey);
		}

		public void DeleteByIds<T>(ICollection ids) where T : class, new()
		{
			if (ids == null) return;

			var keysLength = ids.Count;
			var keys = new string[keysLength];

			var i = 0;
			foreach (var id in ids)
			{
				var urnKey = IdUtils.CreateUrn<T>(id);
				keys[i++] = urnKey;

				var typeIdsSetKy = this.GetTypeIdsSetKey<T>();
				this.RemoveFromSet(typeIdsSetKy, urnKey);
			}

			this.Remove(keys);
		}

		public void DeleteAll<T>() where T : class, new()
		{
			var typeIdsSetKey = this.GetTypeIdsSetKey<T>();
			var urnKeys = this.GetAllFromSet(typeIdsSetKey);
			this.Remove(urnKeys.ToArray());
			this.Remove(typeIdsSetKey);
		}

		#endregion
	}

}