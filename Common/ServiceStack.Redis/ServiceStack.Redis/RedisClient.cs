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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Utils;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.Redis.Generic;
using ServiceStack.Redis.Support;
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
		}


		#region Common Methods

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

		public string[] AllKeys
		{
			get
			{
				return ToString(Keys("*")).Split(' ');
			}
		}

		public void SetString(string key, string value)
		{
			var bytesValue = value != null
				? ToBytes(value)
				: null;

			Set(key, bytesValue);
		}

		public bool SetIfNotExists(string key, string value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			return SetNX(key, ToBytes(value)) == Success;
		}

		public string GetString(string key)
		{
			var bytes = Get(key);
			return bytes == null
				? null
				: ToString(bytes);
		}

		public string GetAndSetString(string key, string value)
		{
			return ToString(GetSet(key, ToBytes(value)));
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

		public string[] GetKeys(string pattern)
		{
			var spaceDelimitedKeys = ToString(Keys(pattern));
			return spaceDelimitedKeys.IsNullOrEmpty()
				? new string[0]
				: spaceDelimitedKeys.Split(' ');
		}

		public List<string> GetKeyValues(List<string> keys)
		{
			var resultBytesArray = MGet(keys.ToArray());

			var results = new List<string>();
			foreach (var resultBytes in resultBytesArray)
			{
				if (resultBytes == null) continue;

				var resultString = ToString(resultBytes);
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

				var resultString = ToString(resultBytes);
				var result = TypeSerializer.DeserializeFromString<T>(resultString);
				results.Add(result);
			}

			return results;
		}

		#endregion


		#region Set Methods

		public IHasNamed<IRedisClientSet> Sets { get; set; }

		internal class RedisClientSets
			: IHasNamed<IRedisClientSet>
		{
			private readonly RedisClient client;

			public RedisClientSets(RedisClient client)
			{
				this.client = client;
			}

			public IRedisClientSet this[string setId]
			{
				get
				{
					return new RedisClientSet(client, setId);
				}
				set
				{
					var col = this[setId];
					col.Clear();
					col.CopyTo(value.ToArray(), 0);
				}
			}
		}

		private static HashSet<string> CreateHashSet(byte[][] multiDataList)
		{
			var results = new HashSet<string>();
			foreach (var multiData in multiDataList)
			{
				results.Add(ToString(multiData));
			}
			return results;
		}

		public List<string> GetSortedRange(string setId, int startingFrom, int endingAt)
		{
			var multiDataList = Sort(setId, startingFrom, endingAt, true, false);
			return CreateList(multiDataList);
		}

		public HashSet<string> GetAllFromSet(string setId)
		{
			var multiDataList = SMembers(setId);
			return CreateHashSet(multiDataList);
		}

		public void AddToSet(string setId, string value)
		{
			SAdd(setId, ToBytes(value));
		}

		public void RemoveFromSet(string setId, string value)
		{
			SRem(setId, ToBytes(value));
		}

		public string PopFromSet(string setId)
		{
			return ToString(SPop(setId));
		}

		public void MoveBetweenSets(string fromSetId, string toSetId, string value)
		{
			SMove(fromSetId, toSetId, ToBytes(value));
		}

		public int GetSetCount(string setId)
		{
			return SCard(setId);
		}

		public bool SetContainsValue(string setId, string value)
		{
			return SIsMember(setId, ToBytes(value)) == 1;
		}

		public HashSet<string> GetIntersectFromSets(params string[] setIds)
		{
			if (setIds.Length == 0)
				return new HashSet<string>();

			var multiDataList = SInter(setIds);
			return CreateHashSet(multiDataList);
		}

		public void StoreIntersectFromSets(string intoSetId, params string[] setIds)
		{
			if (setIds.Length == 0) return;

			SInterStore(intoSetId, setIds);
		}

		public HashSet<string> GetUnionFromSets(params string[] setIds)
		{
			if (setIds.Length == 0)
				return new HashSet<string>();

			var multiDataList = SUnion(setIds);
			return CreateHashSet(multiDataList);
		}

		public void StoreUnionFromSets(string intoSetId, params string[] setIds)
		{
			if (setIds.Length == 0) return;

			SUnionStore(intoSetId, setIds);
		}

		public HashSet<string> GetDifferencesFromSet(string fromSetId, params string[] withSetIds)
		{
			if (withSetIds.Length == 0)
				return new HashSet<string>();

			var multiDataList = SDiff(fromSetId, withSetIds);
			return CreateHashSet(multiDataList);
		}

		public void StoreDifferencesFromSet(string intoSetId, string fromSetId, params string[] withSetIds)
		{
			if (withSetIds.Length == 0) return;

			SDiffStore(intoSetId, fromSetId, withSetIds);
		}

		public string GetRandomEntryFromSet(string setId)
		{
			return ToString(SRandMember(setId));
		}

		#endregion


		#region List Methods

		const int FirstElement = 0;
		const int LastElement = -1;

		public IRedisTypedClient<T> GetTypedClient<T>()
		{
			return new RedisTypedClient<T>(this);
		}

		public IHasNamed<IRedisList> Lists { get; set; }

		internal class RedisClientLists
			: IHasNamed<IRedisList>
		{
			private readonly RedisClient client;

			public RedisClientLists(RedisClient client)
			{
				this.client = client;
			}

			public IRedisList this[string listId]
			{
				get
				{
					return new RedisClientList(client, listId);
				}
				set
				{
					var list = this[listId];
					list.Clear();
					list.CopyTo(value.ToArray(), 0);
				}
			}
		}

		private static List<string> CreateList(byte[][] multiDataList)
		{
			var results = new List<string>();
			foreach (var multiData in multiDataList)
			{
				results.Add(ToString(multiData));
			}
			return results;
		}

		public List<string> GetAllFromList(string listId)
		{
			var multiDataList = LRange(listId, FirstElement, LastElement);
			return CreateList(multiDataList);
		}

		public List<string> GetRangeFromList(string listId, int startingFrom, int endingAt)
		{
			var multiDataList = LRange(listId, startingFrom, endingAt);
			return CreateList(multiDataList);
		}

		public List<string> GetRangeFromSortedList(string listId, int startingFrom, int endingAt)
		{
			var multiDataList = Sort(listId, startingFrom, endingAt, true, false);
			return CreateList(multiDataList);
		}

		public void AddToList(string listId, string value)
		{
			RPush(listId, ToBytes(value));
		}

		public void PrependToList(string listId, string value)
		{
			LPush(listId, ToBytes(value));
		}

		public void RemoveAllFromList(string listId)
		{
			LTrim(listId, LastElement, FirstElement);
		}

		public void TrimList(string listId, int keepStartingFrom, int keepEndingAt)
		{
			LTrim(listId, keepStartingFrom, keepEndingAt);
		}

		public int RemoveValueFromList(string listId, string value)
		{
			return LRem(listId, 0, ToBytes(value));
		}

		public int RemoveValueFromList(string listId, string value, int noOfMatches)
		{
			return LRem(listId, noOfMatches, ToBytes(value));
		}

		public int GetListCount(string listId)
		{
			return LLen(listId);
		}

		public string GetItemFromList(string listId, int listIndex)
		{
			return ToString(LIndex(listId, listIndex));
		}

		public void SetItemInList(string listId, int listIndex, string value)
		{
			LSet(listId, listIndex, ToBytes(value));
		}

		public string DequeueFromList(string listId)
		{
			return ToString(LPop(listId));
		}

		public string PopFromList(string listId)
		{
			return ToString(RPop(listId));
		}

		public string PopAndPushBetweenLists(string fromListId, string toListId)
		{
			return ToString(RPopLPush(fromListId, toListId));
		}


		private static double GetLexicalScore(string value)
		{
			if (string.IsNullOrEmpty(value))
				return 0;

			var lexicalValue = 0;
			if (value.Length >= 1)
				lexicalValue += value[0] * (int)Math.Pow(256, 3);

			if (value.Length >= 2)
				lexicalValue += value[1] * (int)Math.Pow(256, 2);

			if (value.Length >= 3)
				lexicalValue += value[2] * (int)Math.Pow(256, 1);

			if (value.Length >= 4)
				lexicalValue += value[3];

			return lexicalValue;
		}

		public bool AddToSortedSet(string setId, string value)
		{
			return AddToSortedSet(setId, value, GetLexicalScore(value));
		}

		public bool AddToSortedSet(string setId, string value, double score)
		{
			return base.ZAdd(setId, score, ToBytes(value)) == Success;
		}

		public double RemoveFromSortedSet(string setId, string value)
		{
			return base.ZRem(setId, ToBytes(value));
		}

		public string PopFromSortedSet(string setId)
		{
			//TODO: this should be atomic
			var topScoreItemBytes = base.ZRevRange(setId, FirstElement, 1);
			if (topScoreItemBytes.Length == 0) return null;

			base.ZRem(setId, topScoreItemBytes[0]);
			return ToString(topScoreItemBytes[0]);
		}

		public bool SortedSetContainsValue(string setId, string value)
		{
			return base.ZRank(setId, ToBytes(value)) != -1;
		}

		public double IncrementItemInSortedSet(string setId, double incrementBy, string value)
		{
			return base.ZIncrBy(setId, incrementBy, ToBytes(value));
		}

		public int GetItemRankInSortedSet(string setId, string value)
		{
			return base.ZRank(setId, ToBytes(value));
		}

		public int GetItemRankInSortedSetDesc(string setId, string value)
		{
			return base.ZRevRank(setId, ToBytes(value));
		}

		public List<string> GetAllFromSortedSet(string setId)
		{
			var multiDataList = base.ZRange(setId, FirstElement, LastElement);
			return CreateList(multiDataList);
		}

		public List<string> GetAllFromSortedSetDesc(string setId)
		{
			var multiDataList = base.ZRevRange(setId, FirstElement, LastElement);
			return CreateList(multiDataList);
		}

		public List<string> GetRangeFromSortedSet(string setId, int startingFrom, int endingAt)
		{
			var multiDataList = base.ZRange(setId, startingFrom, endingAt);
			return CreateList(multiDataList);
		}

		public IDictionary<string, double> GetRangeFromSortedSetWithScores(string setId, int startingFrom, int endingAt)
		{
			var multiDataList = base.ZRangeWithScores(setId, startingFrom, endingAt);
			return CreateSortedScoreMap(multiDataList);
			
		}

		public List<string> GetRangeFromSortedSetDesc(string setId, int startingFrom, int endingAt)
		{
			var multiDataList = base.ZRange(setId, startingFrom, endingAt);
			return CreateList(multiDataList);
		}

		public IDictionary<string, double> GetRangeFromSortedSetWithScoresDesc(string setId, int startingFrom, int endingAt)
		{
			var multiDataList = base.ZRevRangeWithScores(setId, startingFrom, endingAt);
			return CreateSortedScoreMap(multiDataList);
		}

		public List<string> GetRangeFromSortedSetByScore(string setId, double startingFrom, double endingAt, int? skip, int? take)
		{
			var multiDataList = base.ZRangeByScore(setId, startingFrom, endingAt, skip, take);
			return CreateList(multiDataList);
		}

		private static IDictionary<string, double> CreateSortedScoreMap(byte[][] multiDataList)
		{
			var map = new OrderedDictionary<string, double>();

			for (var i = 0; i < multiDataList.Length; i += 2)
			{
				var key = ToString(multiDataList[i]);
				double value;
				double.TryParse(ToString(multiDataList[i + 1]), out value);
				map[key] = value;
			}

			return map;
		}

		public IDictionary<string, double> GetRangeFromSortedSetByScoreWithScores(string setId, double fromScore, double toScore, int? skip, int? take)
		{
			var multiDataList = base.ZRangeByScoreWithScores(setId, fromScore, toScore, skip, take);
			return CreateSortedScoreMap(multiDataList);
		}

		public List<string> GetRangeFromSortedSetByScore(string setId, string fromStringScore, string toStringScore, int? skip, int? take)
		{
			var fromScore = GetLexicalScore(fromStringScore);
			var toScore = GetLexicalScore(toStringScore);
			return GetRangeFromSortedSetByScore(setId, fromScore, toScore, skip, take);
		}

		public IDictionary<string, double> GetRangeFromSortedSetByScoreWithScores(string setId, string fromStringScore, string toStringScore, int? skip, int? take)
		{
			var fromScore = GetLexicalScore(fromStringScore);
			var toScore = GetLexicalScore(toStringScore);
			return GetRangeFromSortedSetByScoreWithScores(setId, fromScore, toScore, skip, take);
		}

		public List<string> GetRangeFromSortedSetByScoreDesc(string setId, double fromScore, double toScore, int? skip, int? take)
		{
			var multiDataList = base.ZRevRangeByScore(setId, fromScore, toScore, skip, take);
			return CreateList(multiDataList);
		}

		public IDictionary<string, double> GetRangeFromSortedSetByScoreWithScoresDesc(string setId, double fromScore, double toScore, int? skip, int? take)
		{
			var multiDataList = base.ZRevRangeByScoreWithScores(setId, fromScore, toScore, skip, take);
			return CreateSortedScoreMap(multiDataList);
		}

		public int RemoveRangeFromSortedSetByRank(string setId, int minRank, int maxRank)
		{
			return base.ZRemRangeByRank(setId, maxRank, maxRank);
		}

		public int GetSortedSetCount(string setId)
		{
			return base.ZCard(setId);
		}

		public double GetItemScoreInSortedSet(string setId, string value)
		{
			return base.ZScore(setId, ToBytes(value));
		}

		public int StoreIntersectFromSortedSets(string intoSetId, params string[] setIds)
		{
			return base.ZInter(intoSetId, setIds);
		}

		public int StoreUnionFromSortedSets(string intoSetId, params string[] setIds)
		{
			return base.ZUnion(intoSetId, setIds);
		}


		public bool SetItemInHash(string hashId, string key, string value)
		{
			return base.HSet(hashId, key, ToBytes(value)) == Success;
		}

		public string GetItemFromHash(string hashId, string key)
		{
			return ToString(base.HGet(hashId, key));
		}

		public bool DeleteItemInHash(string hashId, string key)
		{
			return base.HDel(hashId, key) == Success;
		}

		public int GetHashCount(string hashId)
		{
			return base.HLen(hashId);
		}

		public List<string> GetHashKeys(string hashId)
		{
			var multiDataList = base.HKeys(hashId);
			return CreateList(multiDataList);
		}

		public List<string> GetHashValues(string hashId)
		{
			var multiDataList = base.HValues(hashId);
			return CreateList(multiDataList);
		}

		public Dictionary<string, string> GetAllFromHash(string hashId)
		{
			var multiDataList = base.HGetAll(hashId);
			var map = new Dictionary<string, string>();

			for (var i = 0; i < multiDataList.Length; i += 2)
			{
				var key = ToString(multiDataList[i]);
				map[key] = ToString(multiDataList[i + 1]);
			}

			return map;
		}

		#endregion

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


		public static string ToString(byte[] bytes)
		{
			return Encoding.UTF8.GetString(bytes);
		}

		public static byte[] ToBytes(string value)
		{
			return Encoding.UTF8.GetBytes(value);
		}
	}
}