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
			get { return GetValue(key); }
			set { SetEntry(key, value); }
		}

		public string GetTypeSequenceKey<T>()
		{
			return "seq:" + typeof(T).Name;
		}

		public string GetTypeIdsSetKey<T>()
		{
			return "ids:" + typeof(T).Name;
		}

		public void RewriteAppendOnlyFileAsync()
		{
			base.BgRewriteAof();
		}

		public List<string> GetAllKeys()
		{
			return SearchKeys("*");
		}

		public void SetEntry(string key, string value)
		{
			var bytesValue = value != null
				? value.ToUtf8Bytes()
				: null;

			Set(key, bytesValue);
		}

		public void SetEntry(string key, string value, TimeSpan expireIn)
		{
			var bytesValue = value != null
				? value.ToUtf8Bytes()
				: null;

			SetEx(key, (int)expireIn.TotalSeconds, bytesValue);
		}

		public bool SetEntryIfNotExists(string key, string value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			return SetNX(key, value.ToUtf8Bytes()) == Success;
		}

		public string GetValue(string key)
		{
			var bytes = Get(key);
			return bytes == null
				? null
				: bytes.FromUtf8Bytes();
		}

		public string GetAndSetEntry(string key, string value)
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

		public bool RemoveEntry(params string[] keys)
		{
			if (keys.Length == 0) return false;

			return Del(keys) == Success;
		}

		public int IncrementValue(string key)
		{
			return Incr(key);
		}

		public int IncrementValueBy(string key, int count)
		{
			return IncrBy(key, count);
		}

		public int DecrementValue(string key)
		{
			return Decr(key);
		}

		public int DecrementValueBy(string key, int count)
		{
			return DecrBy(key, count);
		}

		public int AppendToValue(string key, string value)
		{
			return base.Append(key, value.ToUtf8Bytes());
		}

		public string GetSubstring(string key, int fromIndex, int toIndex)
		{
			return base.Substr(key, fromIndex, toIndex).FromUtf8Bytes();
		}

		public string GetRandomKey()
		{
			return RandomKey();
		}

		public bool ExpireEntryIn(string key, TimeSpan expireIn)
		{
			return Expire(key, (int)expireIn.TotalSeconds) == Success;
		}

		public bool ExpireEntryAt(string key, DateTime expireAt)
		{
			return ExpireAt(key, expireAt.ToUnixTime()) == Success;
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

		public IDisposable AcquireLock(string key)
		{
			return new RedisLock(this, key, null);
		}

		public IDisposable AcquireLock(string key, TimeSpan timeOut)
		{
			return new RedisLock(this, key, timeOut);
		}

		public List<string> SearchKeys(string pattern)
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

		public List<string> GetValues(List<string> keys)
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

		public List<T> GetValues<T>(List<string> keys)
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

		public IRedisSubscription CreateSubscription()
		{
			return new RedisSubscription(this);
		}

		public void PublishMessage(string toChannel, string message)
		{
			base.Publish(toChannel, message.ToUtf8Bytes());
		}

		#region IBasicPersistenceProvider


		Dictionary<string, HashSet<string>> registeredTypeIdsWithinTransactionMap = new Dictionary<string, HashSet<string>>();

		internal HashSet<string> GetRegisteredTypeIdsWithinTransaction(string typeIdsSet)
		{
			HashSet<string> registeredTypeIdsWithinTransaction;
			if (!registeredTypeIdsWithinTransactionMap.TryGetValue(typeIdsSet, out registeredTypeIdsWithinTransaction))
			{
				registeredTypeIdsWithinTransaction = new HashSet<string>();
				registeredTypeIdsWithinTransactionMap[typeIdsSet] = registeredTypeIdsWithinTransaction;
			}
			return registeredTypeIdsWithinTransaction;
		}

		internal void RegisterTypeId<T>(T value)
		{
			var typeIdsSetKey = GetTypeIdsSetKey<T>();
			var id = value.GetId().ToString();

			if (this.CurrentTransaction != null)
			{
				var registeredTypeIdsWithinTransaction = GetRegisteredTypeIdsWithinTransaction(typeIdsSetKey);
				registeredTypeIdsWithinTransaction.Add(id);
			}
			else
			{
				this.AddItemToSet(typeIdsSetKey, id);
			}
		}

		internal void RegisterTypeIds<T>(IEnumerable<T> values)
		{
			var typeIdsSetKey = GetTypeIdsSetKey<T>();
			var ids = values.ConvertAll(x => x.GetId().ToString());

			if (this.CurrentTransaction != null)
			{
				var registeredTypeIdsWithinTransaction = GetRegisteredTypeIdsWithinTransaction(typeIdsSetKey);
				ids.ForEach(x => registeredTypeIdsWithinTransaction.Add(x));
			}
			else
			{
				ids.ForEach(x => this.AddItemToSet(typeIdsSetKey, x));
			}
		}

		internal void RemoveTypeIds<T>(params string[] ids)
		{
			var typeIdsSetKey = GetTypeIdsSetKey<T>();
			if (this.CurrentTransaction != null)
			{
				var registeredTypeIdsWithinTransaction = GetRegisteredTypeIdsWithinTransaction(typeIdsSetKey);
				ids.ForEach(x => registeredTypeIdsWithinTransaction.Remove(x));
			}
			else
			{
				ids.ForEach(x => this.RemoveItemFromSet(typeIdsSetKey, x));
			}
		}

		internal void RemoveTypeIds<T>(params T[] values)
		{
			var typeIdsSetKey = GetTypeIdsSetKey<T>();
			if (this.CurrentTransaction != null)
			{
				var registeredTypeIdsWithinTransaction = GetRegisteredTypeIdsWithinTransaction(typeIdsSetKey);
				values.ForEach(x => registeredTypeIdsWithinTransaction.Remove(x.GetId().ToString()));
			}
			else
			{
				values.ForEach(x => this.RemoveItemFromSet(typeIdsSetKey, x.GetId().ToString()));
			}
		}

		internal void AddTypeIdsRegisteredDuringTransaction()
		{
			foreach (var entry in registeredTypeIdsWithinTransactionMap)
			{
				var typeIdsSetKey = entry.Key;
				foreach (var id in entry.Value)
				{
					var registeredTypeIdsWithinTransaction = GetRegisteredTypeIdsWithinTransaction(typeIdsSetKey);
					registeredTypeIdsWithinTransaction.ForEach(x => this.AddItemToSet(typeIdsSetKey, id));
				}
			}
			registeredTypeIdsWithinTransactionMap = new Dictionary<string, HashSet<string>>();
		}

		internal void ClearTypeIdsRegisteredDuringTransaction()
		{
			registeredTypeIdsWithinTransactionMap = new Dictionary<string, HashSet<string>>();
		}


		public T GetById<T>(object id) where T : class, new()
		{
			var key = IdUtils.CreateUrn<T>(id);
			var valueString = this.GetValue(key);
			var value = TypeSerializer.DeserializeFromString<T>(valueString);
			return value;
		}

		public IList<T> GetByIds<T>(ICollection ids)
			where T : class, new()
		{
			if (ids == null || ids.Count == 0)
				return new List<T>();

			var urnKeys = ids.ConvertAll(x => IdUtils.CreateUrn<T>(x));
			return GetValues<T>(urnKeys);
		}

		public IList<T> GetAll<T>()
			where T : class, new()
		{
			var typeIdsSetKy = this.GetTypeIdsSetKey<T>();
			var allTypeIds = this.GetAllItemsFromSet(typeIdsSetKy);
			var urnKeys = allTypeIds.ConvertAll(x => IdUtils.CreateUrn<T>(x));
			return GetValues<T>(urnKeys);
		}

		public T Store<T>(T entity)
			where T : class, new()
		{
			var urnKey = entity.CreateUrn();
			var valueString = TypeSerializer.SerializeToString(entity);

			this.SetEntry(urnKey, valueString);
			RegisterTypeId(entity);

			return entity;
		}

		public void StoreAll<TEntity>(IEnumerable<TEntity> entities)
			where TEntity : class, new()
		{
			if (entities == null) return;

			var entitiesList = entities.ToList();
			var len = entitiesList.Count;

			var keys = new byte[len][];
			var values = new byte[len][];

			for (var i = 0; i < len; i++)
			{
				keys[i] = entitiesList[i].CreateUrn().ToUtf8Bytes();
				values[i] = entitiesList[i].SerializeToUtf8Bytes();
			}

			base.MSet(keys, values);
			RegisterTypeIds(entitiesList);
		}

		public void Delete<T>(T entity)
			where T : class, new()
		{
			var urnKey = entity.CreateUrn();
			this.Remove(urnKey);
			this.RemoveTypeIds(entity);
		}

		public void DeleteById<T>(object id) where T : class, new()
		{
			var urnKey = IdUtils.CreateUrn<T>(id);
			this.Remove(urnKey);
			this.RemoveTypeIds<T>(id.ToString());
		}

		public void DeleteByIds<T>(ICollection ids) where T : class, new()
		{
			if (ids == null) return;

			var urnKeys = ids.ConvertAll(x => IdUtils.CreateUrn<T>(x));
			this.RemoveEntry(urnKeys.ToArray());
			this.RemoveTypeIds<T>(ids.ConvertAll(x => x.ToString()).ToArray());
		}

		public void DeleteAll<T>() where T : class, new()
		{
			var typeIdsSetKey = this.GetTypeIdsSetKey<T>();
			var ids = this.GetAllItemsFromSet(typeIdsSetKey);
			var urnKeys = ids.ConvertAll(x => IdUtils.CreateUrn<T>(x));
			this.RemoveEntry(urnKeys.ToArray());
			this.Remove(typeIdsSetKey);
		}

		#endregion
	}

}