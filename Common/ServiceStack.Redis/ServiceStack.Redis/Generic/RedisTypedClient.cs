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
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Utils;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.Text;

namespace ServiceStack.Redis.Generic
{
	/// <summary>
	/// Allows you to get Redis value operations to operate against POCO types.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	internal partial class RedisTypedClient<T>
		: IRedisTypedClient<T>
	{
		readonly TypeSerializer<T> serializer = new TypeSerializer<T>();
		private readonly RedisClient client;

		internal IRedisNativeClient NativeClient
		{
			get { return client; }
		}

		/// <summary>
		/// Use this to share the same redis connection with another
		/// </summary>
		/// <param name="client">The client.</param>
		public RedisTypedClient(RedisClient client)
		{
			this.client = client;
			this.Lists = new RedisClientLists(this);
			this.Sets = new RedisClientSets(this);
			this.SortedSets = new RedisClientSortedSets(this);

			this.SequenceKey = client.GetTypeSequenceKey<T>();
			this.TypeIdsSetKey = client.GetTypeIdsSetKey<T>();
			this.TypeLockKey = "lock:" + typeof(T).Name; 
		}

		public string TypeIdsSetKey { get; set; }
		public string TypeLockKey { get; set; }

		public IRedisTypedTransaction<T> CreateTransaction()
		{
			return new RedisTypedTransaction<T>(this);
		}

		public IDisposable AcquireLock()
		{
			return client.AcquireLock(this.TypeLockKey);
		}

		public IDisposable AcquireLock(TimeSpan timeOut)
		{
			return client.AcquireLock(this.TypeLockKey, timeOut);
		}

		public IRedisQueableTransaction CurrentTransaction
		{
			get
			{
				return client.CurrentTransaction;
			}
			set
			{
				client.CurrentTransaction = value;
			}
		}

		public void Multi()
		{
			this.client.Multi();
		}

		public void Discard()
		{
			this.client.Discard();
		}

		public int Exec()
		{
			return this.client.Exec();
		}

		internal void AddTypeIdsRegisteredDuringTransaction()
		{
			client.AddTypeIdsRegisteredDuringTransaction();
		}

		internal void ClearTypeIdsRegisteredDuringTransaction()
		{
			client.ClearTypeIdsRegisteredDuringTransaction();
		}

		public List<string> GetAllKeys()
		{
			return client.GetAllKeys();
		}

		public T this[string key]
		{
			get { return GetValue(key); }
			set { SetEntry(key, value); }
		}

		public byte[] SerializeValue(T value)
		{
			var strValue = serializer.SerializeToString(value);
			return Encoding.UTF8.GetBytes(strValue);
		}

		public T DeserializeValue(byte[] value)
		{
			var strValue = value != null ? Encoding.UTF8.GetString(value) : null;
			return serializer.DeserializeFromString(strValue);
		}

		public void SetEntry(string key, T value)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			client.Set(key, SerializeValue(value));
			client.RegisterTypeId(value);
		}

		public void SetEntry(string key, T value, TimeSpan expireIn)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			client.Set(key, SerializeValue(value), expireIn);
			client.RegisterTypeId(value);
		}

		public bool SetEntryIfNotExists(string key, T value)
		{
			var success = client.SetNX(key, SerializeValue(value)) == RedisNativeClient.Success;
			if (success) client.RegisterTypeId(value);
			return success;
		}

		public T GetValue(string key)
		{
			return DeserializeValue(client.Get(key));
		}

		public T GetAndSetValue(string key, T value)
		{
			return DeserializeValue(client.GetSet(key, SerializeValue(value)));
		}

		public bool ContainsKey(string key)
		{
			return client.Exists(key) == RedisNativeClient.Success;
		}

		public bool RemoveEntry(string key)
		{
			return client.Del(key) == RedisNativeClient.Success;
		}

		public bool RemoveEntry(params string[] keys)
		{
			return client.Del(keys) == RedisNativeClient.Success;
		}

		public bool RemoveEntry(params IHasStringId[] entities)
		{
			var ids = entities.ConvertAll(x => x.Id);
			var success = client.Del(ids.ToArray()) == RedisNativeClient.Success;
			if (success) client.RemoveTypeIds(ids.ToArray());
			return success;
		}

		public int IncrementValue(string key)
		{
			return client.Incr(key);
		}

		public int IncrementValueBy(string key, int count)
		{
			return client.IncrBy(key, count);
		}

		public int DecrementValue(string key)
		{
			return client.Decr(key);
		}

		public int DecrementValueBy(string key, int count)
		{
			return client.DecrBy(key, count);
		}

		public string SequenceKey { get; set; }

		public void SetSequence(int value)
		{
			client.GetSet(SequenceKey, Encoding.UTF8.GetBytes(value.ToString()));
		}

		public int GetNextSequence()
		{
			return IncrementValue(SequenceKey);
		}

		public RedisKeyType GetEntryType(string key)
		{
			return client.GetEntryType(key);
		}

		public string GetRandomKey()
		{
			return client.RandomKey();
		}

		public bool ExpireEntryIn(string key, TimeSpan expireIn)
		{
			return client.Expire(key, (int)expireIn.TotalSeconds) == RedisNativeClient.Success;
		}

		public bool ExpireEntryAt(string key, DateTime expireAt)
		{
			return client.ExpireAt(key, expireAt.ToUnixTime()) == RedisNativeClient.Success;
		}

		public TimeSpan GetTimeToLive(string key)
		{
			return TimeSpan.FromSeconds(client.Ttl(key));
		}

		public void Save()
		{
			client.Save();
		}

		public void SaveAsync()
		{
			client.SaveAsync();
		}

		public void FlushDb()
		{
			client.FlushDb();
		}

		public void FlushAll()
		{
			client.FlushAll();
		}

		public T[] SearchKeys(string pattern)
		{
			var strKeys = client.SearchKeys(pattern);
			var keysCount = strKeys.Count;

			var keys = new T[keysCount];
			for (var i=0; i < keysCount; i++)
			{
				keys[i] = serializer.DeserializeFromString(strKeys[i]);
			}
			return keys;
		}

		public List<T> GetValues(List<string> keys)
		{
			var resultBytesArray = client.MGet(keys.ToArray());

			var results = new List<T>();
			foreach (var resultBytes in resultBytesArray)
			{
				if (resultBytes == null) continue;

				var result = DeserializeValue(resultBytes);
				results.Add(result);
			}

			return results;
		}


		#region Implementation of IBasicPersistenceProvider<T>

		public T GetById(string id)
		{
			var key = IdUtils.CreateUrn<T>(id);
			return this.GetValue(key);
		}

		public IList<T> GetByIds(ICollection<string> ids)
		{
			if (ids == null || ids.Count == 0)
				return new List<T>();

			var urnKeys = ids.ConvertAll(x => IdUtils.CreateUrn<T>(x));
			return GetValues(urnKeys);
		}

		public IList<T> GetAll()
		{
			var allKeys = client.GetAllItemsFromSet(this.TypeIdsSetKey);
			return this.GetByIds(allKeys);
		}

		public T Store(T entity)
		{
			var urnKey = entity.CreateUrn();
			this.SetEntry(urnKey, entity);

			return entity;
		}

		public void StoreAll(IEnumerable<T> entities)
		{
			if (entities == null) return;

			foreach (var entity in entities)
			{
				Store(entity);
			}
		}

		public void Delete(T entity)
		{
			var urnKey = entity.CreateUrn();
			this.RemoveEntry(urnKey);
			client.RemoveTypeIds(entity);
		}

		public void DeleteById(string id)
		{
			var urnKey = IdUtils.CreateUrn<T>(id);

			this.RemoveEntry(urnKey);
			client.RemoveTypeIds<T>(id);
		}

		public void DeleteByIds(ICollection<string> ids)
		{
			if (ids == null) return;

			var urnKeys = ids.ConvertAll(x => IdUtils.CreateUrn<T>(x));
			this.RemoveEntry(urnKeys.ToArray());
			client.RemoveTypeIds<T>(ids.ToArray());
		}

		public void DeleteAll()
		{
			var urnKeys = client.GetAllItemsFromSet(this.TypeIdsSetKey);
			this.RemoveEntry(urnKeys.ToArray());
			this.RemoveEntry(this.TypeIdsSetKey);
		}

		#endregion

		public void Dispose() {}
	}
}