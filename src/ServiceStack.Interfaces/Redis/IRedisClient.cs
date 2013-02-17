//
// https://github.com/ServiceStack/ServiceStack.Redis/
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2013 ServiceStack.
//
// Licensed under the same terms of Redis and ServiceStack: new BSD license.
//

using System;
using System.Collections.Generic;
using ServiceStack.CacheAccess;
using ServiceStack.DataAccess;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.Redis.Generic;
using ServiceStack.Redis.Pipeline;
#if WINDOWS_PHONE
using ServiceStack.Text.WP;
#endif

namespace ServiceStack.Redis
{
	public interface IRedisClient
		: IBasicPersistenceProvider, ICacheClient
	{
		//Basic Redis Connection operations
		int Db { get; set; }
		int DbSize { get; }
		Dictionary<string, string> Info { get; }
		DateTime LastSave { get; }
		string Host { get; }
		int Port { get; }
        int ConnectTimeout { get; set; }
		int RetryTimeout { get; set; }
		int RetryCount { get; set; }
		int SendTimeout { get; set; }
		string Password { get; set; }
		bool HadExceptions { get; }

		void Save();
		void SaveAsync();
		void Shutdown();
		void RewriteAppendOnlyFileAsync();
		void FlushDb();

		//Basic Redis Connection Info
		string this[string key] { get; set; }

		List<string> GetAllKeys();
		void SetEntry(string key, string value);
		void SetEntry(string key, string value, TimeSpan expireIn);
		bool SetEntryIfNotExists(string key, string value);
	    void SetAll(IEnumerable<string> keys, IEnumerable<string> values);
	    void SetAll(Dictionary<string, string> map);
		string GetValue(string key);
		string GetAndSetEntry(string key, string value);
		List<string> GetValues(List<string> keys);
		List<T> GetValues<T>(List<string> keys);
		Dictionary<string, string> GetValuesMap(List<string> keys);
		Dictionary<string, T> GetValuesMap<T>(List<string> keys);
		int AppendToValue(string key, string value);
		void RenameKey(string fromName, string toName);
		string GetSubstring(string key, int fromIndex, int toIndex);

        //store POCOs as hash
	    T GetFromHash<T>(object id);
	    void StoreAsHash<T>(T entity);

	    object StoreObject(object entity);

		bool ContainsKey(string key);
		bool RemoveEntry(params string[] args);
		long IncrementValue(string key);
		long IncrementValueBy(string key, int count);
		long DecrementValue(string key);
		long DecrementValueBy(string key, int count);
		List<string> SearchKeys(string pattern);

		RedisKeyType GetEntryType(string key);
		string GetRandomKey();
		bool ExpireEntryIn(string key, TimeSpan expireIn);
		bool ExpireEntryAt(string key, DateTime expireAt);
		TimeSpan GetTimeToLive(string key);
		List<string> GetSortedEntryValues(string key, int startingFrom, int endingAt);

		//Store entities without registering entity ids
		void WriteAll<TEntity>(IEnumerable<TEntity> entities);

		/// <summary>
		/// Returns a high-level typed client API
		/// Shorter Alias is As&lt;T&gt;();
		/// </summary>
		/// <typeparam name="T"></typeparam>
		IRedisTypedClient<T> GetTypedClient<T>();

		/// <summary>
		/// Returns a high-level typed client API
		/// </summary>
		/// <typeparam name="T"></typeparam>
		IRedisTypedClient<T> As<T>(); 

		IHasNamed<IRedisList> Lists { get; set; }
		IHasNamed<IRedisSet> Sets { get; set; }
		IHasNamed<IRedisSortedSet> SortedSets { get; set; }
		IHasNamed<IRedisHash> Hashes { get; set; }

		IRedisTransaction CreateTransaction();
	    IRedisPipeline CreatePipeline();

		IDisposable AcquireLock(string key);
		IDisposable AcquireLock(string key, TimeSpan timeOut);

		#region Redis pubsub

		void Watch(params string[] keys);
		void UnWatch();
		IRedisSubscription CreateSubscription();
		int PublishMessage(string toChannel, string message);

		#endregion


		#region Set operations

		HashSet<string> GetAllItemsFromSet(string setId);
		void AddItemToSet(string setId, string item);
		void AddRangeToSet(string setId, List<string> items);
		void RemoveItemFromSet(string setId, string item);
		string PopItemFromSet(string setId);
		void MoveBetweenSets(string fromSetId, string toSetId, string item);
		int GetSetCount(string setId);
		bool SetContainsItem(string setId, string item);
		HashSet<string> GetIntersectFromSets(params string[] setIds);
		void StoreIntersectFromSets(string intoSetId, params string[] setIds);
		HashSet<string> GetUnionFromSets(params string[] setIds);
		void StoreUnionFromSets(string intoSetId, params string[] setIds);
		HashSet<string> GetDifferencesFromSet(string fromSetId, params string[] withSetIds);
		void StoreDifferencesFromSet(string intoSetId, string fromSetId, params string[] withSetIds);
		string GetRandomItemFromSet(string setId);

		#endregion


		#region List operations

		List<string> GetAllItemsFromList(string listId);
		List<string> GetRangeFromList(string listId, int startingFrom, int endingAt);
		List<string> GetRangeFromSortedList(string listId, int startingFrom, int endingAt);
		List<string> GetSortedItemsFromList(string listId, SortOptions sortOptions);
		void AddItemToList(string listId, string value);
		void AddRangeToList(string listId, List<string> values);
		void PrependItemToList(string listId, string value);
		void PrependRangeToList(string listId, List<string> values);

		void RemoveAllFromList(string listId);
		string RemoveStartFromList(string listId);
		string BlockingRemoveStartFromList(string listId, TimeSpan? timeOut);
        ItemRef BlockingRemoveStartFromLists(string []listIds, TimeSpan? timeOut);
		string RemoveEndFromList(string listId);
		void TrimList(string listId, int keepStartingFrom, int keepEndingAt);
		int RemoveItemFromList(string listId, string value);
		int RemoveItemFromList(string listId, string value, int noOfMatches);
		int GetListCount(string listId);
		string GetItemFromList(string listId, int listIndex);
		void SetItemInList(string listId, int listIndex, string value);

		//Queue operations
		void EnqueueItemOnList(string listId, string value);
		string DequeueItemFromList(string listId);
		string BlockingDequeueItemFromList(string listId, TimeSpan? timeOut);
        ItemRef BlockingDequeueItemFromLists(string []listIds, TimeSpan? timeOut);

		//Stack operations
		void PushItemToList(string listId, string value);
		string PopItemFromList(string listId);
		string BlockingPopItemFromList(string listId, TimeSpan? timeOut);
        ItemRef BlockingPopItemFromLists(string []listIds, TimeSpan? timeOut);
		string PopAndPushItemBetweenLists(string fromListId, string toListId);
        string BlockingPopAndPushItemBetweenLists(string fromListId, string toListId, TimeSpan? timeOut);

		#endregion


		#region Sorted Set operations

		bool AddItemToSortedSet(string setId, string value);
		bool AddItemToSortedSet(string setId, string value, double score);
		bool AddRangeToSortedSet(string setId, List<string> values, double score);
		bool AddRangeToSortedSet(string setId, List<string> values, long score);
		bool RemoveItemFromSortedSet(string setId, string value);
		string PopItemWithLowestScoreFromSortedSet(string setId);
		string PopItemWithHighestScoreFromSortedSet(string setId);
		bool SortedSetContainsItem(string setId, string value);
		double IncrementItemInSortedSet(string setId, string value, double incrementBy);
		double IncrementItemInSortedSet(string setId, string value, long incrementBy);
		int GetItemIndexInSortedSet(string setId, string value);
		int GetItemIndexInSortedSetDesc(string setId, string value);
		List<string> GetAllItemsFromSortedSet(string setId);
		List<string> GetAllItemsFromSortedSetDesc(string setId);
		List<string> GetRangeFromSortedSet(string setId, int fromRank, int toRank);
		List<string> GetRangeFromSortedSetDesc(string setId, int fromRank, int toRank);
		IDictionary<string, double> GetAllWithScoresFromSortedSet(string setId);
		IDictionary<string, double> GetRangeWithScoresFromSortedSet(string setId, int fromRank, int toRank);
		IDictionary<string, double> GetRangeWithScoresFromSortedSetDesc(string setId, int fromRank, int toRank);
		List<string> GetRangeFromSortedSetByLowestScore(string setId, string fromStringScore, string toStringScore);
		List<string> GetRangeFromSortedSetByLowestScore(string setId, string fromStringScore, string toStringScore, int? skip, int? take);
		List<string> GetRangeFromSortedSetByLowestScore(string setId, double fromScore, double toScore);
		List<string> GetRangeFromSortedSetByLowestScore(string setId, long fromScore, long toScore);
		List<string> GetRangeFromSortedSetByLowestScore(string setId, double fromScore, double toScore, int? skip, int? take);
		List<string> GetRangeFromSortedSetByLowestScore(string setId, long fromScore, long toScore, int? skip, int? take);
		IDictionary<string, double> GetRangeWithScoresFromSortedSetByLowestScore(string setId, string fromStringScore, string toStringScore);
		IDictionary<string, double> GetRangeWithScoresFromSortedSetByLowestScore(string setId, string fromStringScore, string toStringScore, int? skip, int? take);
		IDictionary<string, double> GetRangeWithScoresFromSortedSetByLowestScore(string setId, double fromScore, double toScore);
		IDictionary<string, double> GetRangeWithScoresFromSortedSetByLowestScore(string setId, long fromScore, long toScore);
		IDictionary<string, double> GetRangeWithScoresFromSortedSetByLowestScore(string setId, double fromScore, double toScore, int? skip, int? take);
		IDictionary<string, double> GetRangeWithScoresFromSortedSetByLowestScore(string setId, long fromScore, long toScore, int? skip, int? take);
		List<string> GetRangeFromSortedSetByHighestScore(string setId, string fromStringScore, string toStringScore);
		List<string> GetRangeFromSortedSetByHighestScore(string setId, string fromStringScore, string toStringScore, int? skip, int? take);
		List<string> GetRangeFromSortedSetByHighestScore(string setId, double fromScore, double toScore);
		List<string> GetRangeFromSortedSetByHighestScore(string setId, long fromScore, long toScore);
		List<string> GetRangeFromSortedSetByHighestScore(string setId, double fromScore, double toScore, int? skip, int? take);
		List<string> GetRangeFromSortedSetByHighestScore(string setId, long fromScore, long toScore, int? skip, int? take);
		IDictionary<string, double> GetRangeWithScoresFromSortedSetByHighestScore(string setId, string fromStringScore, string toStringScore);
		IDictionary<string, double> GetRangeWithScoresFromSortedSetByHighestScore(string setId, string fromStringScore, string toStringScore, int? skip, int? take);
		IDictionary<string, double> GetRangeWithScoresFromSortedSetByHighestScore(string setId, double fromScore, double toScore);
		IDictionary<string, double> GetRangeWithScoresFromSortedSetByHighestScore(string setId, long fromScore, long toScore);
		IDictionary<string, double> GetRangeWithScoresFromSortedSetByHighestScore(string setId, double fromScore, double toScore, int? skip, int? take);
		IDictionary<string, double> GetRangeWithScoresFromSortedSetByHighestScore(string setId, long fromScore, long toScore, int? skip, int? take);
		int RemoveRangeFromSortedSet(string setId, int minRank, int maxRank);
		int RemoveRangeFromSortedSetByScore(string setId, double fromScore, double toScore);
		int RemoveRangeFromSortedSetByScore(string setId, long fromScore, long toScore);
        int GetSortedSetCount(string setId);
        int GetSortedSetCount(string setId, string fromStringScore, string toStringScore);
        int GetSortedSetCount(string setId, long fromScore, long toScore);
        int GetSortedSetCount(string setId, double fromScore, double toScore);
		double GetItemScoreInSortedSet(string setId, string value);
		int StoreIntersectFromSortedSets(string intoSetId, params string[] setIds);
		int StoreUnionFromSortedSets(string intoSetId, params string[] setIds);

		#endregion


		#region Hash operations

		bool HashContainsEntry(string hashId, string key);
		bool SetEntryInHash(string hashId, string key, string value);
		bool SetEntryInHashIfNotExists(string hashId, string key, string value);
		void SetRangeInHash(string hashId, IEnumerable<KeyValuePair<string, string>> keyValuePairs);
		int IncrementValueInHash(string hashId, string key, int incrementBy);
		string GetValueFromHash(string hashId, string key);
		List<string> GetValuesFromHash(string hashId, params string[] keys);
		bool RemoveEntryFromHash(string hashId, string key);
		int GetHashCount(string hashId);
		List<string> GetHashKeys(string hashId);
		List<string> GetHashValues(string hashId);
		Dictionary<string, string> GetAllEntriesFromHash(string hashId);

		#endregion


		#region Eval/Lua operations

        string ExecLuaAsString(string luaBody, params string[] args);
        string ExecLuaAsString(string luaBody, string[] keys, string[] args);
        string ExecLuaShaAsString(string sha1, params string[] args);
        string ExecLuaShaAsString(string sha1, string[] keys, string[] args);
        
        int ExecLuaAsInt(string luaBody, params string[] args);
        int ExecLuaAsInt(string luaBody, string[] keys, string[] args);
        int ExecLuaShaAsInt(string sha1, params string[] args);
        int ExecLuaShaAsInt(string sha1, string[] keys, string[] args);

        List<string> ExecLuaAsList(string luaBody, params string[] args);
        List<string> ExecLuaAsList(string luaBody, string[] keys, string[] args);
        List<string> ExecLuaShaAsList(string sha1, params string[] args);
        List<string> ExecLuaShaAsList(string sha1, string[] keys, string[] args);

        string CalculateSha1(string luaBody);
        
        bool HasLuaScript(string sha1Ref);
        Dictionary<string, bool> WhichLuaScriptsExists(params string[] sha1Refs);
        void RemoveAllLuaScripts();
        void KillRunningLuaScript();
        string LoadLuaScript(string body);
        
        #endregion

	}
}