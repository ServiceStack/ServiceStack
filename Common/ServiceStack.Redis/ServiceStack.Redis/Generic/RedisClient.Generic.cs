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
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Text;
using ServiceStack.Common.Utils;
using ServiceStack.DataAccess;
using ServiceStack.DesignPatterns.Model;

namespace ServiceStack.Redis.Generic
{
	public class RedisGenericClient<T>
		: IRedisGenericClient<T>, IBasicPersistenceProvider<T>
	{
		readonly TypeSerializer<T> Serializer = new TypeSerializer<T>();
		private readonly RedisNativeClient client;

		internal IRedisNativeClient NativeClient
		{
			get { return client; }
		}

		public RedisGenericClient(string host, int port)
			: this(new RedisNativeClient(host, port))
		{
		}

		public RedisGenericClient()
			: this(new RedisNativeClient())
		{
		}

		public RedisGenericClient(RedisNativeClient client)
		{
			this.client = client;
			this.Lists = new RedisClientLists(this);
			this.Sets = new RedisClientSets(this);
		}

		public DateTime LastSave
		{
			get { return client.LastSave; }
		}

		public string[] AllKeys
		{
			get
			{
				return Encoding.UTF8.GetString(client.Keys("*")).Split(' ');
			}
		}

		public T this[string key]
		{
			get { return Get(key); }
			set { Set(key, value); }
		}

		public byte[] ToBytes(T value)
		{
			var strValue = Serializer.SerializeToString(value);
			return Encoding.UTF8.GetBytes(strValue);
		}

		public T FromBytes(byte[] value)
		{
			var strValue = value != null ? Encoding.UTF8.GetString(value) : null;
			return Serializer.DeserializeFromString(strValue);
		}

		public void Set(string key, T value)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			client.Set(key, ToBytes(value));
		}

		public bool SetIfNotExists(string key, T value)
		{
			return client.SetNX(key, ToBytes(value)) == RedisNativeClient.Success;
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

		public void Shutdown()
		{
			client.Shutdown();
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
			var strKeys = Encoding.UTF8.GetString(client.Keys(pattern)).Split(' ');
			var keysCount = strKeys.Length;

			var keys = new T[keysCount];
			for (var i=0; i < keysCount; i++)
			{
				keys[i] = Serializer.DeserializeFromString(strKeys[i]);
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



		public IHasNamedCollection<T> Sets { get; set; }
		public Dictionary<string, string> Info
		{
			get { return client.Info; }
		}

		public int Db
		{
			get { return client.Db; }
			set { client.Db = value; }
		}

		public int DbSize
		{
			get { return client.DbSize; }
		}

		internal class RedisClientSets
			: IHasNamedCollection<T>
		{
			private readonly RedisGenericClient<T> client;

			public RedisClientSets(RedisGenericClient<T> client)
			{
				this.client = client;
			}

			public ICollection<T> this[string setId]
			{
				get
				{
					return new RedisClientSet<T>(client, setId);
				}
				set
				{
					var col = this[setId];
					col.Clear();
					col.CopyTo(value.ToArray(), 0);
				}
			}
		}

		private HashSet<T> CreateHashSet(byte[][] multiDataList)
		{
			var results = new HashSet<T>();
			foreach (var multiData in multiDataList)
			{
				results.Add(FromBytes(multiData));
			}
			return results;
		}

		public List<T> GetRangeFromSortedSet(string setId, int startingFrom, int endingAt)
		{
			var multiDataList = client.Sort(setId, startingFrom, endingAt, true, false);
			return CreateList(multiDataList);
		}

		public HashSet<T> GetAllFromSet(string setId)
		{
			var multiDataList = client.SMembers(setId);
			return CreateHashSet(multiDataList);
		}

		public void AddToSet(string setId, T value)
		{
			client.SAdd(setId, ToBytes(value));
		}

		public void RemoveFromSet(string setId, T value)
		{
			client.SRem(setId, ToBytes(value));
		}

		public T PopFromSet(string setId)
		{
			return FromBytes(client.SPop(setId));
		}

		public void MoveBetweenSets(string fromSetId, string toSetId, T value)
		{
			client.SMove(fromSetId, toSetId, ToBytes(value));
		}

		public int GetSetCount(string setId)
		{
			return client.SCard(setId);
		}

		public bool SetContainsValue(string setId, T value)
		{
			return client.SIsMember(setId, ToBytes(value)) == 1;
		}

		public HashSet<T> GetIntersectFromSets(params string[] setIds)
		{
			var multiDataList = client.SInter(setIds);
			return CreateHashSet(multiDataList);
		}

		public void StoreIntersectFromSets(string intoSetId, params string[] setIds)
		{
			client.SInterStore(intoSetId, setIds);
		}

		public HashSet<T> GetUnionFromSets(params string[] setIds)
		{
			var multiDataList = client.SUnion(setIds);
			return CreateHashSet(multiDataList);
		}

		public void StoreUnionFromSets(string intoSetId, params string[] setIds)
		{
			client.SUnionStore(intoSetId, setIds);
		}

		public HashSet<T> GetDifferencesFromSet(string fromSetId, params string[] withSetIds)
		{
			var multiDataList = client.SDiff(fromSetId, withSetIds);
			return CreateHashSet(multiDataList);
		}

		public void StoreDifferencesFromSet(string intoSetId, string fromSetId, params string[] withSetIds)
		{
			client.SDiffStore(intoSetId, fromSetId, withSetIds);
		}

		public T GetRandomEntryFromSet(string setId)
		{
			return FromBytes(client.SRandMember(setId));
		}


		public string Host
		{
			get { return client.Host; }
		}

		public int Port
		{
			get { return client.Port; }
		}

		public int RetryTimeout
		{
			get { return client.RetryTimeout; }
			set { client.RetryTimeout = value; }
		}

		public int RetryCount
		{
			get { return client.RetryCount; }
			set { client.RetryCount = value; }
		}

		public int SendTimeout
		{
			get { return client.SendTimeout; }
			set { client.SendTimeout = value; }
		}

		public string Password
		{
			get { return client.Password; }
			set { client.Password = value; }
		}


		const int FirstElement = 0;
		const int LastElement = -1;

		public IHasNamedList<T> Lists { get; set; }

		internal class RedisClientLists
			: IHasNamedList<T>
		{
			private readonly RedisGenericClient<T> client;

			public RedisClientLists(RedisGenericClient<T> client)
			{
				this.client = client;
			}

			public IList<T> this[string listId]
			{
				get
				{
					return new RedisClientList<T>(client, listId);
				}
				set
				{
					var list = this[listId];
					list.Clear();
					list.CopyTo(value.ToArray(), 0);
				}
			}
		}

		private List<T> CreateList(byte[][] multiDataList)
		{
			var results = new List<T>();
			foreach (var multiData in multiDataList)
			{
				results.Add(FromBytes(multiData));
			}
			return results;
		}

		public List<T> GetAllFromList(string listId)
		{
			var multiDataList = client.LRange(listId, FirstElement, LastElement);
			return CreateList(multiDataList);
		}

		public List<T> GetRangeFromList(string listId, int startingFrom, int endingAt)
		{
			var multiDataList = client.LRange(listId, startingFrom, endingAt);
			return CreateList(multiDataList);
		}

		public List<T> GetRangeFromSortedList(string listId, int startingFrom, int endingAt)
		{
			var multiDataList = client.Sort(listId, startingFrom, endingAt, true, false);
			return CreateList(multiDataList);
		}

		public void AddToList(string listId, T value)
		{
			client.RPush(listId, ToBytes(value));
		}

		public void PrependToList(string listId, T value)
		{
			client.LPush(listId, ToBytes(value));
		}

		public void RemoveAllFromList(string listId)
		{
			client.LTrim(listId, LastElement, FirstElement);
		}

		public void TrimList(string listId, int keepStartingFrom, int keepEndingAt)
		{
			client.LTrim(listId, keepStartingFrom, keepEndingAt);
		}

		public int RemoveValueFromList(string listId, T value)
		{
			const int removeAll = 0;
			return client.LRem(listId, removeAll, ToBytes(value));
		}

		public int RemoveValueFromList(string listId, T value, int noOfMatches)
		{
			return client.LRem(listId, noOfMatches, ToBytes(value));
		}

		public int GetListCount(string setId)
		{
			return client.LLen(setId);
		}

		public T GetItemFromList(string listId, int listIndex)
		{
			return FromBytes(client.LIndex(listId, listIndex));
		}

		public void SetItemInList(string listId, int listIndex, T value)
		{
			client.LSet(listId, listIndex, ToBytes(value));
		}

		public T DequeueFromList(string listId)
		{
			return FromBytes(client.LPop(listId));
		}

		public T PopFromList(string listId)
		{
			return FromBytes(client.RPop(listId));
		}

		public void PopAndPushBetweenLists(string fromListId, string toListId)
		{
			client.RPopLPush(fromListId, toListId);
		}


		#region Implementation of IBasicPersistenceProvider<T>

		public T GetById(string id)
		{
			var key = IdUtils.CreateUrn<T>(id);
			return this.Get(key); 
		}

		public IList<T> GetByIds(ICollection<string> ids)
		{
			var keys = new List<string>();
			foreach (var id in ids)
			{
				var key = IdUtils.CreateUrn<T>(id);
				keys.Add(key);
			}

			return GetKeyValues(keys);
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
		}

		public void DeleteById(string id)
		{
			var key = IdUtils.CreateUrn<T>(id);
			this.Remove(key);
		}

		public void DeleteByIds(ICollection<string> ids)
		{
			if (ids == null) return;

			var keysLength = ids.Count;
			var keys = new string[keysLength];

			var i = 0;
			foreach (var id in ids)
			{
				var key = IdUtils.CreateUrn<T>(id);
				keys[i++] = key;
			}

			this.Remove(keys);
		}

		public void DeleteAll()
		{
			throw new NotImplementedException();
			//TODO: replace with DeleteAll of TEntity
			client.FlushDb();
		}

		#endregion

		public void Dispose()
		{
			client.Dispose();
		}
	}
}