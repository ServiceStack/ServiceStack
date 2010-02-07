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
	/// <summary>
	/// Allows you to get Redis value operations to operate against POCO types.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	internal class RedisGenericClient<T>
		: IRedisTypedClient<T>
	{
		readonly TypeSerializer<T> Serializer = new TypeSerializer<T>();
		private readonly RedisClient client;

		internal IRedisNativeClient NativeClient
		{
			get { return client; }
		}

		/// <summary>
		/// Use this to share the same redis connection with another
		/// </summary>
		/// <param name="client">The client.</param>
		public RedisGenericClient(RedisClient client)
		{
			this.client = client;
			this.Lists = new RedisClientLists(this);
			this.Sets = new RedisClientSets(this);

			this.SequenceKey = client.GetTypeSequenceKey<T>();
			this.TypeIdsSetKey = client.GetTypeIdsSetKey<T>(); 
		}

		public string TypeIdsSetKey { get; set; }

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

			client.AddToSet(this.TypeIdsSetKey, value.GetId().ToString());
			client.Set(key, ToBytes(value));
		}

		public bool SetIfNotExists(string key, T value)
		{
			client.AddToSet(this.TypeIdsSetKey, value.GetId().ToString());
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

		public bool Remove(params IHasStringId[] entities)
		{
			var ids = entities.ConvertAll(x => x.Id);
			ids.ForEach(x => client.RemoveFromSet(this.TypeIdsSetKey, x));
			return client.Del(ids.ToArray()) == RedisNativeClient.Success;
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



		public IHasNamed<IRedisSet<T>> Sets { get; set; }

		public int Db
		{
			get { return client.Db; }
			set { client.Db = value; }
		}

		internal class RedisClientSets
			: IHasNamed<IRedisSet<T>>
		{
			private readonly RedisGenericClient<T> client;

			public RedisClientSets(RedisGenericClient<T> client)
			{
				this.client = client;
			}

			public IRedisSet<T> this[string setId]
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

		public List<T> GetRangeFromSortedSet(IRedisSet<T> fromSet, int startingFrom, int endingAt)
		{
			var multiDataList = client.Sort(fromSet.Id, startingFrom, endingAt, true, false);
			return CreateList(multiDataList);
		}

		public HashSet<T> GetAllFromSet(IRedisSet<T> fromSet)
		{
			var multiDataList = client.SMembers(fromSet.Id);
			return CreateHashSet(multiDataList);
		}

		public void AddToSet(IRedisSet<T> toSet, T value)
		{
			client.SAdd(toSet.Id, ToBytes(value));
		}

		public void RemoveFromSet(IRedisSet<T> fromSet, T value)
		{
			client.SRem(fromSet.Id, ToBytes(value));
		}

		public T PopFromSet(IRedisSet<T> fromSet)
		{
			return FromBytes(client.SPop(fromSet.Id));
		}

		public void MoveBetweenSets(IRedisSet<T> fromSet, IRedisSet<T> toSet, T value)
		{
			client.SMove(fromSet.Id, toSet.Id, ToBytes(value));
		}

		public int GetSetCount(IRedisSet<T> set)
		{
			return client.SCard(set.Id);
		}

		public bool SetContainsValue(IRedisSet<T> set, T value)
		{
			return client.SIsMember(set.Id, ToBytes(value)) == 1;
		}

		public HashSet<T> GetIntersectFromSets(params IRedisSet<T>[] sets)
		{
			var multiDataList = client.SInter(sets.ConvertAll(x => x.Id).ToArray());
			return CreateHashSet(multiDataList);
		}

		public void StoreIntersectFromSets(IRedisSet<T> intoSet, params IRedisSet<T>[] sets)
		{
			client.SInterStore(intoSet.Id, sets.ConvertAll(x => x.Id).ToArray());
		}

		public HashSet<T> GetUnionFromSets(params IRedisSet<T>[] sets)
		{
			var multiDataList = client.SUnion(sets.ConvertAll(x => x.Id).ToArray());
			return CreateHashSet(multiDataList);
		}

		public void StoreUnionFromSets(IRedisSet<T> intoSet, params IRedisSet<T>[] sets)
		{
			client.SUnionStore(intoSet.Id, sets.ConvertAll(x => x.Id).ToArray());
		}

		public HashSet<T> GetDifferencesFromSet(IRedisSet<T> fromSet, params IRedisSet<T>[] withSets)
		{
			var multiDataList = client.SDiff(fromSet.Id, withSets.ConvertAll(x => x.Id).ToArray());
			return CreateHashSet(multiDataList);
		}

		public void StoreDifferencesFromSet(IRedisSet<T> intoSet, IRedisSet<T> fromSet, params IRedisSet<T>[] withSets)
		{
			client.SDiffStore(intoSet.Id, fromSet.Id, withSets.ConvertAll(x => x.Id).ToArray());
		}

		public T GetRandomEntryFromSet(IRedisSet<T> fromSet)
		{
			return FromBytes(client.SRandMember(fromSet.Id));
		}



		const int FirstElement = 0;
		const int LastElement = -1;

		public IHasNamed<IRedisList<T>> Lists { get; set; }

		internal class RedisClientLists
			: IHasNamed<IRedisList<T>>
		{
			private readonly RedisGenericClient<T> client;

			public RedisClientLists(RedisGenericClient<T> client)
			{
				this.client = client;
			}

			public IRedisList<T> this[string listId]
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

		public List<T> GetAllFromList(IRedisList<T> fromList)
		{
			var multiDataList = client.LRange(fromList.Id, FirstElement, LastElement);
			return CreateList(multiDataList);
		}

		public List<T> GetRangeFromList(IRedisList<T> fromList, int startingFrom, int endingAt)
		{
			var multiDataList = client.LRange(fromList.Id, startingFrom, endingAt);
			return CreateList(multiDataList);
		}

		public List<T> GetRangeFromSortedList(IRedisList<T> fromList, int startingFrom, int endingAt)
		{
			var multiDataList = client.Sort(fromList.Id, startingFrom, endingAt, true, false);
			return CreateList(multiDataList);
		}

		public void AddToList(IRedisList<T> fromList, T value)
		{
			client.RPush(fromList.Id, ToBytes(value));
		}

		public void PrependToList(IRedisList<T> fromList, T value)
		{
			client.LPush(fromList.Id, ToBytes(value));
		}

		public void RemoveAllFromList(IRedisList<T> fromList)
		{
			client.LTrim(fromList.Id, LastElement, FirstElement);
		}

		public void TrimList(IRedisList<T> fromList, int keepStartingFrom, int keepEndingAt)
		{
			client.LTrim(fromList.Id, keepStartingFrom, keepEndingAt);
		}

		public int RemoveValueFromList(IRedisList<T> fromList, T value)
		{
			const int removeAll = 0;
			return client.LRem(fromList.Id, removeAll, ToBytes(value));
		}

		public int RemoveValueFromList(IRedisList<T> fromList, T value, int noOfMatches)
		{
			return client.LRem(fromList.Id, noOfMatches, ToBytes(value));
		}

		public int GetListCount(IRedisList<T> fromList)
		{
			return client.LLen(fromList.Id);
		}

		public T GetItemFromList(IRedisList<T> fromList, int listIndex)
		{
			return FromBytes(client.LIndex(fromList.Id, listIndex));
		}

		public void SetItemInList(IRedisList<T> toList, int listIndex, T value)
		{
			client.LSet(toList.Id, listIndex, ToBytes(value));
		}

		public T DequeueFromList(IRedisList<T> fromList)
		{
			return FromBytes(client.LPop(fromList.Id));
		}

		public T PopFromList(IRedisList<T> fromList)
		{
			return FromBytes(client.RPop(fromList.Id));
		}

		public void PopAndPushBetweenLists(IRedisList<T> fromList, IRedisList<T> toList)
		{
			client.RPopLPush(fromList.Id, toList.Id);
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
			client.RemoveFromSet(this.TypeIdsSetKey, urnKey);
			this.Remove(urnKey);
		}

		public void DeleteById(string id)
		{
			var urnKey = IdUtils.CreateUrn<T>(id);

			client.RemoveFromSet(this.TypeIdsSetKey, urnKey);
			this.Remove(urnKey);
		}

		public void DeleteByIds(ICollection<string> ids)
		{
			if (ids == null) return;

			var urnKeysLength = ids.Count;
			var urnKeys = new string[urnKeysLength];

			var i = 0;
			foreach (var id in ids)
			{
				var urnKey = IdUtils.CreateUrn<T>(id);
				urnKeys[i++] = urnKey;

				client.RemoveFromSet(this.TypeIdsSetKey, urnKey);
			}

			this.Remove(urnKeys);
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