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
		}

		public string TypeIdsSetKey { get; set; }

		public IRedisTypedTransaction<T> CreateTransaction()
		{
			return new RedisTypedTransaction<T>(this);
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

		public List<string> AllKeys
		{
			get
			{
				return client.AllKeys;
			}
		}

		public T this[string key]
		{
			get { return Get(key); }
			set { Set(key, value); }
		}

		public byte[] ToBytes(T value)
		{
			var strValue = serializer.SerializeToString(value);
			return Encoding.UTF8.GetBytes(strValue);
		}

		public T FromBytes(byte[] value)
		{
			var strValue = value != null ? Encoding.UTF8.GetString(value) : null;
			return serializer.DeserializeFromString(strValue);
		}

		public void Set(string key, T value)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			client.Set(key, ToBytes(value));
			client.RegisterTypeId(value);
		}

		public bool SetIfNotExists(string key, T value)
		{
			var success = client.SetNX(key, ToBytes(value)) == RedisNativeClient.Success;
			if (success) client.RegisterTypeId(value);
			return success;
		}

		public T Get(string key)
		{
			return FromBytes(client.Get(key));
		}

		public T GetAndSet(string key, T value)
		{
			return FromBytes(client.GetSet(key, ToBytes(value)));
		}

		public bool ContainsKey(string key)
		{
			return client.Exists(key) == RedisNativeClient.Success;
		}

		public bool Remove(string key)
		{
			return client.Del(key) == RedisNativeClient.Success;
		}

		public bool Remove(params string[] keys)
		{
			return client.Del(keys) == RedisNativeClient.Success;
		}

		public bool Remove(params IHasStringId[] entities)
		{
			var ids = entities.ConvertAll(x => x.Id);
			var success = client.Del(ids.ToArray()) == RedisNativeClient.Success;
			if (success) client.RemoveTypeIds(ids.ToArray());
			return success;
		}

		public int Increment(string key)
		{
			return client.Incr(key);
		}

		public int IncrementBy(string key, int count)
		{
			return client.IncrBy(key, count);
		}

		public int Decrement(string key)
		{
			return client.Decr(key);
		}

		public int DecrementBy(string key, int count)
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
			return Increment(SequenceKey);
		}

		public RedisKeyType GetKeyType(string key)
		{
			return client.GetKeyType(key);
		}

		public string NewRandomKey()
		{
			return client.RandomKey();
		}

		public bool ExpireKeyIn(string key, TimeSpan expireIn)
		{
			return client.Expire(key, (int)expireIn.TotalSeconds) == RedisNativeClient.Success;
		}

		public bool ExpireKeyAt(string key, DateTime dateTime)
		{
			return client.ExpireAt(key, dateTime.ToUnixTime()) == RedisNativeClient.Success;
		}

		public TimeSpan GetTimeToLive(string key)
		{
			return TimeSpan.FromSeconds(client.Ttl(key));
		}

		public string Save()
		{
			return client.Save();
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

		public T[] GetKeys(string pattern)
		{
			var strKeys = client.GetKeys(pattern);
			var keysCount = strKeys.Count;

			var keys = new T[keysCount];
			for (var i=0; i < keysCount; i++)
			{
				keys[i] = serializer.DeserializeFromString(strKeys[i]);
			}
			return keys;
		}

		public List<T> GetKeyValues(List<string> keys)
		{
			var resultBytesArray = client.MGet(keys.ToArray());

			var results = new List<T>();
			foreach (var resultBytes in resultBytesArray)
			{
				if (resultBytes == null) continue;

				var result = FromBytes(resultBytes);
				results.Add(result);
			}

			return results;
		}


		#region Implementation of IBasicPersistenceProvider<T>

		public T GetById(string id)
		{
			var key = IdUtils.CreateUrn<T>(id);
			return this.Get(key);
		}

		public IList<T> GetByIds(ICollection<string> ids)
		{
			if (ids == null || ids.Count == 0)
				return new List<T>();

			var urnKeys = ids.ConvertAll(x => IdUtils.CreateUrn<T>(x));
			return GetKeyValues(urnKeys);
		}

		public IList<T> GetAll()
		{
			var allKeys = client.GetAllFromSet(this.TypeIdsSetKey);
			return this.GetByIds(allKeys);
		}

		public T Store(T entity)
		{
			var urnKey = entity.CreateUrn();
			this.Set(urnKey, entity);

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
			this.Remove(urnKey);
			client.RemoveTypeIds(entity);
		}

		public void DeleteById(string id)
		{
			var urnKey = IdUtils.CreateUrn<T>(id);

			this.Remove(urnKey);
			client.RemoveTypeIds<T>(id);
		}

		public void DeleteByIds(ICollection<string> ids)
		{
			if (ids == null) return;

			var urnKeys = ids.ConvertAll(x => IdUtils.CreateUrn<T>(x));
			this.Remove(urnKeys.ToArray());
			client.RemoveTypeIds<T>(ids.ToArray());
		}

		public void DeleteAll()
		{
			var urnKeys = client.GetAllFromSet(this.TypeIdsSetKey);
			this.Remove(urnKeys.ToArray());
			this.Remove(this.TypeIdsSetKey);
		}

		#endregion

		public void Dispose() {}
	}
}