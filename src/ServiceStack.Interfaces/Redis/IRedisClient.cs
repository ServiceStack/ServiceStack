//
// https://github.com/ServiceStack/ServiceStack.Redis/
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2014 Service Stack LLC. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using System;
using System.Collections.Generic;
using ServiceStack.Caching;
using ServiceStack.Data;
using ServiceStack.Model;
using ServiceStack.Redis.Generic;
using ServiceStack.Redis.Pipeline;

namespace ServiceStack.Redis
{
    public interface IRedisClient
        : IEntityStore, ICacheClientExtended
    {
        //Basic Redis Connection operations
        long Db { get; set; }
        long DbSize { get; }
        Dictionary<string, string> Info { get; }
        DateTime GetServerTime();
        DateTime LastSave { get; }
        string Host { get; }
        int Port { get; }
        int ConnectTimeout { get; set; }
        int RetryTimeout { get; set; }
        int RetryCount { get; set; }
        int SendTimeout { get; set; }
        string Password { get; set; }
        bool HadExceptions { get; }

        RedisText Custom(params object[] cmdWithArgs);

        void Save();
        void SaveAsync();
        void Shutdown();
        void RewriteAppendOnlyFileAsync();
        void FlushDb();

        RedisText GetServerRoleInfo();
        string GetConfig(string item);
        void SetConfig(string item, string value);
        void SaveConfig();
        void ResetInfoStats();

        string GetClient();
        void SetClient(string name);
        void KillClient(string address);
        long KillClients(string fromAddress = null, string withId = null, RedisClientType? ofType = null, bool? skipMe = null);
        List<Dictionary<string, string>> GetClientsInfo();
        void PauseAllClients(TimeSpan duration);

        //Basic Redis Connection Info
        string this[string key] { get; set; }

        List<string> GetAllKeys();

        [Obsolete("Use SetValue()")]
        void SetEntry(string key, string value);
        [Obsolete("Use SetValue()")]
        void SetEntry(string key, string value, TimeSpan expireIn);
        [Obsolete("Use SetValueIfNotExists()")]
        bool SetEntryIfNotExists(string key, string value);
        [Obsolete("Use GetValue()")]
        string GetEntry(string key);
        [Obsolete("Use GetAndSetValue()")]
        string GetAndSetEntry(string key, string value);

        void SetAll(IEnumerable<string> keys, IEnumerable<string> values);
        void SetAll(Dictionary<string, string> map);

        void SetValue(string key, string value);
        void SetValue(string key, string value, TimeSpan expireIn);
        bool SetValueIfNotExists(string key, string value);
        bool SetValueIfExists(string key, string value);

        string GetValue(string key);
        string GetAndSetValue(string key, string value);

        List<string> GetValues(List<string> keys);
        List<T> GetValues<T>(List<string> keys);
        Dictionary<string, string> GetValuesMap(List<string> keys);
        Dictionary<string, T> GetValuesMap<T>(List<string> keys);
        long AppendToValue(string key, string value);
        void RenameKey(string fromName, string toName);

        //store POCOs as hash
        T GetFromHash<T>(object id);
        void StoreAsHash<T>(T entity);

        object StoreObject(object entity);

        bool ContainsKey(string key);
        bool RemoveEntry(params string[] args);
        long IncrementValue(string key);
        long IncrementValueBy(string key, int count);
        long IncrementValueBy(string key, long count);
        double IncrementValueBy(string key, double count);
        long DecrementValue(string key);
        long DecrementValueBy(string key, int count);
        List<string> SearchKeys(string pattern);

        RedisKeyType GetEntryType(string key);
        string GetRandomKey();
        bool ExpireEntryIn(string key, TimeSpan expireIn);
        bool ExpireEntryAt(string key, DateTime expireAt);
        List<string> GetSortedEntryValues(string key, int startingFrom, int endingAt);

        //Store entities without registering entity ids
        void WriteAll<TEntity>(IEnumerable<TEntity> entities);

        //Scan APIs
        IEnumerable<string> ScanAllKeys(string pattern = null, int pageSize = 1000);
        IEnumerable<string> ScanAllSetItems(string setId, string pattern = null, int pageSize = 1000);
        IEnumerable<KeyValuePair<string, double>> ScanAllSortedSetItems(string setId, string pattern = null, int pageSize = 1000);
        IEnumerable<KeyValuePair<string, string>> ScanAllHashEntries(string hashId, string pattern = null, int pageSize = 1000);

        //Hyperlog APIs
        bool AddToHyperLog(string key, params string[] elements);
        long CountHyperLog(string key);
        void MergeHyperLogs(string toKey, params string[] fromKeys);

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
        long PublishMessage(string toChannel, string message);

        #endregion


        #region Set operations

        HashSet<string> GetAllItemsFromSet(string setId);
        void AddItemToSet(string setId, string item);
        void AddRangeToSet(string setId, List<string> items);
        void RemoveItemFromSet(string setId, string item);
        string PopItemFromSet(string setId);
        void MoveBetweenSets(string fromSetId, string toSetId, string item);
        long GetSetCount(string setId);
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
        ItemRef BlockingRemoveStartFromLists(string[] listIds, TimeSpan? timeOut);
        string RemoveEndFromList(string listId);
        void TrimList(string listId, int keepStartingFrom, int keepEndingAt);
        long RemoveItemFromList(string listId, string value);
        long RemoveItemFromList(string listId, string value, int noOfMatches);
        long GetListCount(string listId);
        string GetItemFromList(string listId, int listIndex);
        void SetItemInList(string listId, int listIndex, string value);

        //Queue operations
        void EnqueueItemOnList(string listId, string value);
        string DequeueItemFromList(string listId);
        string BlockingDequeueItemFromList(string listId, TimeSpan? timeOut);
        ItemRef BlockingDequeueItemFromLists(string[] listIds, TimeSpan? timeOut);

        //Stack operations
        void PushItemToList(string listId, string value);
        string PopItemFromList(string listId);
        string BlockingPopItemFromList(string listId, TimeSpan? timeOut);
        ItemRef BlockingPopItemFromLists(string[] listIds, TimeSpan? timeOut);
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
        long GetItemIndexInSortedSet(string setId, string value);
        long GetItemIndexInSortedSetDesc(string setId, string value);
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
        long RemoveRangeFromSortedSet(string setId, int minRank, int maxRank);
        long RemoveRangeFromSortedSetByScore(string setId, double fromScore, double toScore);
        long RemoveRangeFromSortedSetByScore(string setId, long fromScore, long toScore);
        long GetSortedSetCount(string setId);
        long GetSortedSetCount(string setId, string fromStringScore, string toStringScore);
        long GetSortedSetCount(string setId, long fromScore, long toScore);
        long GetSortedSetCount(string setId, double fromScore, double toScore);
        double GetItemScoreInSortedSet(string setId, string value);
        long StoreIntersectFromSortedSets(string intoSetId, params string[] setIds);
        long StoreIntersectFromSortedSets(string intoSetId, string[] setIds, string[] args);
        long StoreUnionFromSortedSets(string intoSetId, params string[] setIds);
        long StoreUnionFromSortedSets(string intoSetId, string[] setIds, string[] args);
        List<string> SearchSortedSet(string setId, string start = null, string end = null, int? skip = null, int? take = null);
        long SearchSortedSetCount(string setId, string start = null, string end = null);
        long RemoveRangeFromSortedSetBySearch(string setId, string start = null, string end = null);

        #endregion


        #region Hash operations

        bool HashContainsEntry(string hashId, string key);
        bool SetEntryInHash(string hashId, string key, string value);
        bool SetEntryInHashIfNotExists(string hashId, string key, string value);
        void SetRangeInHash(string hashId, IEnumerable<KeyValuePair<string, string>> keyValuePairs);
        long IncrementValueInHash(string hashId, string key, int incrementBy);
        double IncrementValueInHash(string hashId, string key, double incrementBy);
        string GetValueFromHash(string hashId, string key);
        List<string> GetValuesFromHash(string hashId, params string[] keys);
        bool RemoveEntryFromHash(string hashId, string key);
        long GetHashCount(string hashId);
        List<string> GetHashKeys(string hashId);
        List<string> GetHashValues(string hashId);
        Dictionary<string, string> GetAllEntriesFromHash(string hashId);

        #endregion


        #region Eval/Lua operations

        string ExecLuaAsString(string luaBody, params string[] args);
        string ExecLuaAsString(string luaBody, string[] keys, string[] args);
        string ExecLuaShaAsString(string sha1, params string[] args);
        string ExecLuaShaAsString(string sha1, string[] keys, string[] args);

        long ExecLuaAsInt(string luaBody, params string[] args);
        long ExecLuaAsInt(string luaBody, string[] keys, string[] args);
        long ExecLuaShaAsInt(string sha1, params string[] args);
        long ExecLuaShaAsInt(string sha1, string[] keys, string[] args);

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