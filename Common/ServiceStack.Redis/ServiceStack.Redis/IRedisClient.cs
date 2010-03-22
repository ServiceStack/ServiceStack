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
using ServiceStack.CacheAccess;
using ServiceStack.DataAccess;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.Redis.Generic;

namespace ServiceStack.Redis
{
	public interface IRedisClient
		: IBasicPersistenceProvider, ICacheClient
	{
		string Host { get; }
		int Port { get; }
		int RetryTimeout { get; set; }
		int RetryCount { get; set; }
		int SendTimeout { get; set; }
		string Password { get; set; }
		bool HadExceptions { get; }

		IRedisTypedClient<T> GetTypedClient<T>();

		IHasNamed<IRedisList> Lists { get; set; }
		IHasNamed<IRedisClientSet> Sets { get; set; }

		Dictionary<string, string> Info { get; }
		int Db { get; set; }
		int DbSize { get; }
		DateTime LastSave { get; }
		string[] AllKeys { get; }

		string this[string key] { get; set; }
		void SetString(string key, string value);
		bool SetIfNotExists(string key, string value);
		string GetString(string key);
		string GetAndSetString(string key, string value);
		bool ContainsKey(string key);
		bool Remove(params string[] args);
		int Increment(string key);
		int IncrementBy(string key, int count);
		int Decrement(string key);
		int DecrementBy(string key, int count);
		RedisKeyType GetKeyType(string key);
		string NewRandomKey();
		bool ExpireKeyIn(string key, TimeSpan expiresAt);
		bool ExpireKeyAt(string key, DateTime dateTime);
		TimeSpan GetTimeToLive(string key);

		string Save();
		void SaveAsync();
		void Shutdown();
		void FlushDb();


		#region List operations

		string[] GetKeys(string pattern);
		List<string> GetKeyValues(List<string> keys);
		List<T> GetKeyValues<T>(List<string> keys);
		List<string> GetSortedRange(string setId, int startingFrom, int endingAt);
		HashSet<string> GetAllFromSet(string setId);
		void AddToSet(string setId, string value);
		void RemoveFromSet(string setId, string value);
		string PopFromSet(string setId);
		void MoveBetweenSets(string fromSetId, string toSetId, string value);
		int GetSetCount(string setId);
		bool SetContainsValue(string setId, string value);
		HashSet<string> GetIntersectFromSets(params string[] setIds);
		void StoreIntersectFromSets(string intoSetId, params string[] setIds);
		HashSet<string> GetUnionFromSets(params string[] setIds);
		void StoreUnionFromSets(string intoSetId, params string[] setIds);
		HashSet<string> GetDifferencesFromSet(string fromSetId, params string[] withSetIds);
		void StoreDifferencesFromSet(string intoSetId, string fromSetId, params string[] withSetIds);
		string GetRandomEntryFromSet(string setId);

		#endregion


		#region Set operations

		List<string> GetAllFromList(string listId);
		List<string> GetRangeFromList(string listId, int startingFrom, int endingAt);
		List<string> GetRangeFromSortedList(string listId, int startingFrom, int endingAt);
		void AddToList(string listId, string value);
		void PrependToList(string listId, string value);
		void RemoveAllFromList(string listId);
		void TrimList(string listId, int keepStartingFrom, int keepEndingAt);
		int RemoveValueFromList(string listId, string value);
		int RemoveValueFromList(string listId, string value, int noOfMatches);
		int GetListCount(string setId);
		string GetItemFromList(string listId, int listIndex);
		void SetItemInList(string listId, int listIndex, string value);
		string DequeueFromList(string listId);
		string PopFromList(string listId);
		string PopAndPushBetweenLists(string fromListId, string toListId);

		#endregion


		#region Sorted Set operations

		bool AddToSortedSet(string setId, string value);
		bool AddToSortedSet(string setId, string value, double score);
		double RemoveFromSortedSet(string setId, string value);
		string PopFromSortedSet(string setId);
		bool SortedSetContainsValue(string setId, string value);
		double IncrementItemInSortedSet(string setId, double incrementBy, string value);
		int GetItemRankInSortedSet(string setId, string value);
		int GetItemRankInSortedSetDesc(string setId, string value);
		List<string> GetAllFromSortedSet(string setId);
		List<string> GetAllFromSortedSetDesc(string setId);
		List<string> GetRangeFromSortedSet(string setId, int startingFrom, int endingAt);
		IDictionary<string, double> GetRangeFromSortedSetWithScores(string setId, int startingFrom, int endingAt);
		List<string> GetRangeFromSortedSetDesc(string setId, int startingFrom, int endingAt);
		IDictionary<string, double> GetRangeFromSortedSetWithScoresDesc(string setId, int startingFrom, int endingAt);
		List<string> GetRangeFromSortedSetByScore(string setId, double fromScore, double toScore, int? skip, int? take);
		IDictionary<string, double> GetRangeFromSortedSetByScoreWithScores(string setId, double fromScoreScore, double toScore, int? skip, int? take);
		List<string> GetRangeFromSortedSetByScore(string setId, string fromStringScore, string toStringScore, int? skip, int? take);
		IDictionary<string, double> GetRangeFromSortedSetByScoreWithScores(string setId, string fromStringScore, string toStringScore, int? skip, int? take);
		List<string> GetRangeFromSortedSetByScoreDesc(string setId, double fromScoreScore, double toScore, int? skip, int? take);
		IDictionary<string, double> GetRangeFromSortedSetByScoreWithScoresDesc(string setId, double fromScoreScore, double toScore, int? skip, int? take);
		int RemoveRangeFromSortedSetByRank(string setId, int minRank, int maxRank);
		int GetSortedSetCount(string setId);
		double GetItemScoreInSortedSet(string setId, string value);
		int StoreIntersectFromSortedSets(string intoSetId, params string[] setIds);
		int StoreUnionFromSortedSets(string intoSetId, params string[] setIds);

		#endregion


		#region Hash operations

		bool SetItemInHash(string hashId, string key, string value);
		string GetItemFromHash(string hashId, string key);
		bool DeleteItemInHash(string hashId, string key);
		int GetHashCount(string hashId);
		List<string> GetHashKeys(string hashId);
		List<string> GetHashValues(string hashId);
		Dictionary<string, string> GetAllFromHash(string hashId);
		
		#endregion
	}

}