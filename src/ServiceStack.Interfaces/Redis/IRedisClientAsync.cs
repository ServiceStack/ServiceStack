//
// https://github.com/ServiceStack/ServiceStack.Redis/
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2017 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using ServiceStack.Caching;
using ServiceStack.Data;
using ServiceStack.Model;
using ServiceStack.Redis.Generic;
using ServiceStack.Redis.Pipeline;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis
{
    public interface IRedisClientAsync
        : IEntityStoreAsync, ICacheClientAsync, IRemoveByPatternAsync
    {
        /* non-obvious changes from IRedisClient:
        - sync API is Save (foreground) and SaveAsync (background); renamed here to ForegroundSaveAsync and BackgroundSaveAsync
          to avoid overload problems and accidental swaps from bg to fg when migrating to async API
        - RewriteAppendOnlyFileAsync becomes BackgroundRewriteAppendOnlyFileAsync for consistency with the above
        - AcquireLockAsync - timeout made an optional arg rather than an overload
        - SetValueIf[Not]ExistsAsync - flatten overloads via optional expiry
        - move all Dictionary<,> args to IDictionary<,>
        - add SlowlogGet / Reset
        */
        //Basic Redis Connection operations

        ////Basic Redis Connection Info
        //ValueTask<string> this[string key] { get; set; }

        IHasNamed<IRedisListAsync> Lists { get; }
        IHasNamed<IRedisSetAsync> Sets { get; }
        IHasNamed<IRedisSortedSetAsync> SortedSets { get; }
        IHasNamed<IRedisHashAsync> Hashes { get; }

        long Db { get; }
        ValueTask SelectAsync(long db, CancellationToken cancellationToken = default);
        ValueTask<long> DbSizeAsync(CancellationToken cancellationToken = default);

        ValueTask<Dictionary<string, string>> InfoAsync(CancellationToken cancellationToken = default);
        ValueTask<DateTime> GetServerTimeAsync(CancellationToken cancellationToken = default);
        ValueTask<DateTime> LastSaveAsync(CancellationToken cancellationToken = default);
        string Host { get; }
        int Port { get; }
        int ConnectTimeout { get; set; }
        int RetryTimeout { get; set; }
        int RetryCount { get; set; }
        int SendTimeout { get; set; }
        string Password { get; set; }
        bool HadExceptions { get; }

        ValueTask<bool> PingAsync(CancellationToken cancellationToken = default);
        ValueTask<string> EchoAsync(string text, CancellationToken cancellationToken = default);

        ValueTask<RedisText> CustomAsync(object[] cmdWithArgs, CancellationToken cancellationToken = default);
        ValueTask<RedisText> CustomAsync(params object[] cmdWithArgs); // convenience API

        ValueTask ForegroundSaveAsync(CancellationToken cancellationToken = default);
        ValueTask BackgroundSaveAsync(CancellationToken cancellationToken = default);
        ValueTask ShutdownAsync(CancellationToken cancellationToken = default);
        ValueTask ShutdownNoSaveAsync(CancellationToken cancellationToken = default);
        ValueTask BackgroundRewriteAppendOnlyFileAsync(CancellationToken cancellationToken = default);
        ValueTask FlushDbAsync(CancellationToken cancellationToken = default);


        ValueTask<RedisServerRole> GetServerRoleAsync(CancellationToken cancellationToken = default);
        ValueTask<RedisText> GetServerRoleInfoAsync(CancellationToken cancellationToken = default);
        ValueTask<string> GetConfigAsync(string item, CancellationToken cancellationToken = default);
        ValueTask SetConfigAsync(string item, string value, CancellationToken cancellationToken = default);
        ValueTask SaveConfigAsync(CancellationToken cancellationToken = default);
        ValueTask ResetInfoStatsAsync(CancellationToken cancellationToken = default);

        ValueTask<string> GetClientAsync(CancellationToken cancellationToken = default);
        ValueTask SetClientAsync(string name, CancellationToken cancellationToken = default);
        ValueTask KillClientAsync(string address, CancellationToken cancellationToken = default);
        ValueTask<long> KillClientsAsync(string fromAddress = null, string withId = null, RedisClientType? ofType = null, bool? skipMe = null, CancellationToken cancellationToken = default);
        ValueTask<List<Dictionary<string, string>>> GetClientsInfoAsync(CancellationToken cancellationToken = default);
        ValueTask PauseAllClientsAsync(TimeSpan duration, CancellationToken cancellationToken = default);

        ValueTask<List<string>> GetAllKeysAsync(CancellationToken cancellationToken = default);

        //Fetch fully qualified key for specific Type and Id
        string UrnKey<T>(T value);
        string UrnKey<T>(object id);
        string UrnKey(Type type, object id);

        ValueTask SetAllAsync(IEnumerable<string> keys, IEnumerable<string> values, CancellationToken cancellationToken = default);
        ValueTask SetAllAsync(IDictionary<string, string> map, CancellationToken cancellationToken = default);
        ValueTask SetValuesAsync(IDictionary<string, string> map, CancellationToken cancellationToken = default);

        ValueTask SetValueAsync(string key, string value, CancellationToken cancellationToken = default);
        ValueTask SetValueAsync(string key, string value, TimeSpan expireIn, CancellationToken cancellationToken = default);
        ValueTask<bool> SetValueIfNotExistsAsync(string key, string value, TimeSpan? expireIn = default, CancellationToken cancellationToken = default);
        ValueTask<bool> SetValueIfExistsAsync(string key, string value, TimeSpan? expireIn = default, CancellationToken cancellationToken = default);

        ValueTask<string> GetValueAsync(string key, CancellationToken cancellationToken = default);
        ValueTask<string> GetAndSetValueAsync(string key, string value, CancellationToken cancellationToken = default);

        ValueTask<List<string>> GetValuesAsync(List<string> keys, CancellationToken cancellationToken = default);
        ValueTask<List<T>> GetValuesAsync<T>(List<string> keys, CancellationToken cancellationToken = default);
        ValueTask<Dictionary<string, string>> GetValuesMapAsync(List<string> keys, CancellationToken cancellationToken = default);
        ValueTask<Dictionary<string, T>> GetValuesMapAsync<T>(List<string> keys, CancellationToken cancellationToken = default);
        ValueTask<long> AppendToValueAsync(string key, string value, CancellationToken cancellationToken = default);
        ValueTask RenameKeyAsync(string fromName, string toName, CancellationToken cancellationToken = default);

        //store POCOs as hash
        ValueTask<T> GetFromHashAsync<T>(object id, CancellationToken cancellationToken = default);
        ValueTask StoreAsHashAsync<T>(T entity, CancellationToken cancellationToken = default);

        ValueTask<object> StoreObjectAsync(object entity, CancellationToken cancellationToken = default);

        ValueTask<bool> ContainsKeyAsync(string key, CancellationToken cancellationToken = default);
        ValueTask<bool> RemoveEntryAsync(string[] args, CancellationToken cancellationToken = default);
        ValueTask<bool> RemoveEntryAsync(params string[] args); // convenience API
        ValueTask<long> IncrementValueAsync(string key, CancellationToken cancellationToken = default);
        ValueTask<long> IncrementValueByAsync(string key, int count, CancellationToken cancellationToken = default);
        ValueTask<long> IncrementValueByAsync(string key, long count, CancellationToken cancellationToken = default);
        ValueTask<double> IncrementValueByAsync(string key, double count, CancellationToken cancellationToken = default);
        ValueTask<long> DecrementValueAsync(string key, CancellationToken cancellationToken = default);
        ValueTask<long> DecrementValueByAsync(string key, int count, CancellationToken cancellationToken = default);
        ValueTask<List<string>> SearchKeysAsync(string pattern, CancellationToken cancellationToken = default);

        ValueTask<string> TypeAsync(string key, CancellationToken cancellationToken = default);
        ValueTask<RedisKeyType> GetEntryTypeAsync(string key, CancellationToken cancellationToken = default);
        ValueTask<long> GetStringCountAsync(string key, CancellationToken cancellationToken = default);
        ValueTask<string> GetRandomKeyAsync(CancellationToken cancellationToken = default);
        ValueTask<bool> ExpireEntryInAsync(string key, TimeSpan expireIn, CancellationToken cancellationToken = default);
        ValueTask<bool> ExpireEntryAtAsync(string key, DateTime expireAt, CancellationToken cancellationToken = default);
        ValueTask<List<string>> GetSortedEntryValuesAsync(string key, int startingFrom, int endingAt, CancellationToken cancellationToken = default);

        //Store entities without registering entity ids
        ValueTask WriteAllAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        //Scan APIs
        IAsyncEnumerable<string> ScanAllKeysAsync(string pattern = null, int pageSize = 1000, CancellationToken cancellationToken = default);
        IAsyncEnumerable<string> ScanAllSetItemsAsync(string setId, string pattern = null, int pageSize = 1000, CancellationToken cancellationToken = default);
        IAsyncEnumerable<KeyValuePair<string, double>> ScanAllSortedSetItemsAsync(string setId, string pattern = null, int pageSize = 1000, CancellationToken cancellationToken = default);
        IAsyncEnumerable<KeyValuePair<string, string>> ScanAllHashEntriesAsync(string hashId, string pattern = null, int pageSize = 1000, CancellationToken cancellationToken = default);

        //Hyperlog APIs
        ValueTask<bool> AddToHyperLogAsync(string key, string[] elements, CancellationToken cancellationToken = default);
        ValueTask<bool> AddToHyperLogAsync(string key, params string[] elements); // convenience API
        ValueTask<long> CountHyperLogAsync(string key, CancellationToken cancellationToken = default);
        ValueTask MergeHyperLogsAsync(string toKey, string[] fromKeys, CancellationToken cancellationToken = default);
        ValueTask MergeHyperLogsAsync(string toKey, params string[] fromKeys); // convenience API

        //GEO APIs
        ValueTask<long> AddGeoMemberAsync(string key, double longitude, double latitude, string member, CancellationToken cancellationToken = default);
        ValueTask<long> AddGeoMembersAsync(string key, RedisGeo[] geoPoints, CancellationToken cancellationToken = default);
        ValueTask<long> AddGeoMembersAsync(string key, params RedisGeo[] geoPoints); // convenience API
        ValueTask<double> CalculateDistanceBetweenGeoMembersAsync(string key, string fromMember, string toMember, string unit = null, CancellationToken cancellationToken = default);
        ValueTask<string[]> GetGeohashesAsync(string key, string[] members, CancellationToken cancellationToken = default);
        ValueTask<string[]> GetGeohashesAsync(string key, params string[] members); // convenience API
        ValueTask<List<RedisGeo>> GetGeoCoordinatesAsync(string key, string[] members, CancellationToken cancellationToken = default);
        ValueTask<List<RedisGeo>> GetGeoCoordinatesAsync(string key, params string[] members); // convenience API
        ValueTask<string[]> FindGeoMembersInRadiusAsync(string key, double longitude, double latitude, double radius, string unit, CancellationToken cancellationToken = default);
        ValueTask<List<RedisGeoResult>> FindGeoResultsInRadiusAsync(string key, double longitude, double latitude, double radius, string unit, int? count = null, bool? sortByNearest = null, CancellationToken cancellationToken = default);
        ValueTask<string[]> FindGeoMembersInRadiusAsync(string key, string member, double radius, string unit, CancellationToken cancellationToken = default);
        ValueTask<List<RedisGeoResult>> FindGeoResultsInRadiusAsync(string key, string member, double radius, string unit, int? count = null, bool? sortByNearest = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a high-level typed client API
        /// </summary>
        /// <typeparam name="T"></typeparam>
        IRedisTypedClientAsync<T> As<T>();

        ValueTask<IRedisTransactionAsync> CreateTransactionAsync(CancellationToken cancellationToken = default);
        IRedisPipelineAsync CreatePipeline();

        ValueTask<IAsyncDisposable> AcquireLockAsync(string key, TimeSpan? timeOut = default, CancellationToken cancellationToken = default);

        #region Redis pubsub

        ValueTask WatchAsync(string[] keys, CancellationToken cancellationToken = default);
        ValueTask WatchAsync(params string[] keys); // convenience API
        ValueTask UnWatchAsync(CancellationToken cancellationToken = default);
        ValueTask<IRedisSubscriptionAsync> CreateSubscriptionAsync(CancellationToken cancellationToken = default);
        ValueTask<long> PublishMessageAsync(string toChannel, string message, CancellationToken cancellationToken = default);

        #endregion


        #region Set operations

        ValueTask<HashSet<string>> GetAllItemsFromSetAsync(string setId, CancellationToken cancellationToken = default);
        ValueTask AddItemToSetAsync(string setId, string item, CancellationToken cancellationToken = default);
        ValueTask AddRangeToSetAsync(string setId, List<string> items, CancellationToken cancellationToken = default);
        ValueTask RemoveItemFromSetAsync(string setId, string item, CancellationToken cancellationToken = default);
        ValueTask<string> PopItemFromSetAsync(string setId, CancellationToken cancellationToken = default);
        ValueTask<List<string>> PopItemsFromSetAsync(string setId, int count, CancellationToken cancellationToken = default);
        ValueTask MoveBetweenSetsAsync(string fromSetId, string toSetId, string item, CancellationToken cancellationToken = default);
        ValueTask<long> GetSetCountAsync(string setId, CancellationToken cancellationToken = default);
        ValueTask<bool> SetContainsItemAsync(string setId, string item, CancellationToken cancellationToken = default);
        ValueTask<HashSet<string>> GetIntersectFromSetsAsync(string[] setIds, CancellationToken cancellationToken = default);
        ValueTask<HashSet<string>> GetIntersectFromSetsAsync(params string[] setIds); // convenience API
        ValueTask StoreIntersectFromSetsAsync(string intoSetId, string[] setIds, CancellationToken cancellationToken = default);
        ValueTask StoreIntersectFromSetsAsync(string intoSetId, params string[] setIds); // convenience API
        ValueTask<HashSet<string>> GetUnionFromSetsAsync(string[] setIds, CancellationToken cancellationToken = default);
        ValueTask<HashSet<string>> GetUnionFromSetsAsync(params string[] setIds); // convenience API
        ValueTask StoreUnionFromSetsAsync(string intoSetId, string[] setIds, CancellationToken cancellationToken = default);
        ValueTask StoreUnionFromSetsAsync(string intoSetId, params string[] setIds); // convenience API
        ValueTask<HashSet<string>> GetDifferencesFromSetAsync(string fromSetId, string[] withSetIds, CancellationToken cancellationToken = default);
        ValueTask<HashSet<string>> GetDifferencesFromSetAsync(string fromSetId, params string[] withSetIds); // convenience API
        ValueTask StoreDifferencesFromSetAsync(string intoSetId, string fromSetId, string[] withSetIds, CancellationToken cancellationToken = default);
        ValueTask StoreDifferencesFromSetAsync(string intoSetId, string fromSetId, params string[] withSetIds); // convenience API
        ValueTask<string> GetRandomItemFromSetAsync(string setId, CancellationToken cancellationToken = default);

        #endregion


        #region List operations

        ValueTask<List<string>> GetAllItemsFromListAsync(string listId, CancellationToken cancellationToken = default);
        ValueTask<List<string>> GetRangeFromListAsync(string listId, int startingFrom, int endingAt, CancellationToken cancellationToken = default);
        ValueTask<List<string>> GetRangeFromSortedListAsync(string listId, int startingFrom, int endingAt, CancellationToken cancellationToken = default);
        ValueTask<List<string>> GetSortedItemsFromListAsync(string listId, SortOptions sortOptions, CancellationToken cancellationToken = default);
        ValueTask AddItemToListAsync(string listId, string value, CancellationToken cancellationToken = default);
        ValueTask AddRangeToListAsync(string listId, List<string> values, CancellationToken cancellationToken = default);
        ValueTask PrependItemToListAsync(string listId, string value, CancellationToken cancellationToken = default);
        ValueTask PrependRangeToListAsync(string listId, List<string> values, CancellationToken cancellationToken = default);

        ValueTask RemoveAllFromListAsync(string listId, CancellationToken cancellationToken = default);
        ValueTask<string> RemoveStartFromListAsync(string listId, CancellationToken cancellationToken = default);
        ValueTask<string> BlockingRemoveStartFromListAsync(string listId, TimeSpan? timeOut, CancellationToken cancellationToken = default);
        ValueTask<ItemRef> BlockingRemoveStartFromListsAsync(string[] listIds, TimeSpan? timeOut, CancellationToken cancellationToken = default);
        ValueTask<string> RemoveEndFromListAsync(string listId, CancellationToken cancellationToken = default);
        ValueTask TrimListAsync(string listId, int keepStartingFrom, int keepEndingAt, CancellationToken cancellationToken = default);
        ValueTask<long> RemoveItemFromListAsync(string listId, string value, CancellationToken cancellationToken = default);
        ValueTask<long> RemoveItemFromListAsync(string listId, string value, int noOfMatches, CancellationToken cancellationToken = default);
        ValueTask<long> GetListCountAsync(string listId, CancellationToken cancellationToken = default);
        ValueTask<string> GetItemFromListAsync(string listId, int listIndex, CancellationToken cancellationToken = default);
        ValueTask SetItemInListAsync(string listId, int listIndex, string value, CancellationToken cancellationToken = default);

        //Queue operations
        ValueTask EnqueueItemOnListAsync(string listId, string value, CancellationToken cancellationToken = default);
        ValueTask<string> DequeueItemFromListAsync(string listId, CancellationToken cancellationToken = default);
        ValueTask<string> BlockingDequeueItemFromListAsync(string listId, TimeSpan? timeOut, CancellationToken cancellationToken = default);
        ValueTask<ItemRef> BlockingDequeueItemFromListsAsync(string[] listIds, TimeSpan? timeOut, CancellationToken cancellationToken = default);

        //Stack operations
        ValueTask PushItemToListAsync(string listId, string value, CancellationToken cancellationToken = default);
        ValueTask<string> PopItemFromListAsync(string listId, CancellationToken cancellationToken = default);
        ValueTask<string> BlockingPopItemFromListAsync(string listId, TimeSpan? timeOut, CancellationToken cancellationToken = default);
        ValueTask<ItemRef> BlockingPopItemFromListsAsync(string[] listIds, TimeSpan? timeOut, CancellationToken cancellationToken = default);
        ValueTask<string> PopAndPushItemBetweenListsAsync(string fromListId, string toListId, CancellationToken cancellationToken = default);
        ValueTask<string> BlockingPopAndPushItemBetweenListsAsync(string fromListId, string toListId, TimeSpan? timeOut, CancellationToken cancellationToken = default);

        #endregion


        #region Sorted Set operations

        ValueTask<bool> AddItemToSortedSetAsync(string setId, string value, CancellationToken cancellationToken = default);
        ValueTask<bool> AddItemToSortedSetAsync(string setId, string value, double score, CancellationToken cancellationToken = default);
        ValueTask<bool> AddRangeToSortedSetAsync(string setId, List<string> values, double score, CancellationToken cancellationToken = default);
        ValueTask<bool> AddRangeToSortedSetAsync(string setId, List<string> values, long score, CancellationToken cancellationToken = default);
        ValueTask<bool> RemoveItemFromSortedSetAsync(string setId, string value, CancellationToken cancellationToken = default);
        ValueTask<long> RemoveItemsFromSortedSetAsync(string setId, List<string> values, CancellationToken cancellationToken = default);
        ValueTask<string> PopItemWithLowestScoreFromSortedSetAsync(string setId, CancellationToken cancellationToken = default);
        ValueTask<string> PopItemWithHighestScoreFromSortedSetAsync(string setId, CancellationToken cancellationToken = default);
        ValueTask<bool> SortedSetContainsItemAsync(string setId, string value, CancellationToken cancellationToken = default);
        ValueTask<double> IncrementItemInSortedSetAsync(string setId, string value, double incrementBy, CancellationToken cancellationToken = default);
        ValueTask<double> IncrementItemInSortedSetAsync(string setId, string value, long incrementBy, CancellationToken cancellationToken = default);
        ValueTask<long> GetItemIndexInSortedSetAsync(string setId, string value, CancellationToken cancellationToken = default);
        ValueTask<long> GetItemIndexInSortedSetDescAsync(string setId, string value, CancellationToken cancellationToken = default);
        ValueTask<List<string>> GetAllItemsFromSortedSetAsync(string setId, CancellationToken cancellationToken = default);
        ValueTask<List<string>> GetAllItemsFromSortedSetDescAsync(string setId, CancellationToken cancellationToken = default);
        ValueTask<List<string>> GetRangeFromSortedSetAsync(string setId, int fromRank, int toRank, CancellationToken cancellationToken = default);
        ValueTask<List<string>> GetRangeFromSortedSetDescAsync(string setId, int fromRank, int toRank, CancellationToken cancellationToken = default);
        ValueTask<IDictionary<string, double>> GetAllWithScoresFromSortedSetAsync(string setId, CancellationToken cancellationToken = default);
        ValueTask<IDictionary<string, double>> GetRangeWithScoresFromSortedSetAsync(string setId, int fromRank, int toRank, CancellationToken cancellationToken = default);
        ValueTask<IDictionary<string, double>> GetRangeWithScoresFromSortedSetDescAsync(string setId, int fromRank, int toRank, CancellationToken cancellationToken = default);
        ValueTask<List<string>> GetRangeFromSortedSetByLowestScoreAsync(string setId, string fromStringScore, string toStringScore, CancellationToken cancellationToken = default);
        ValueTask<List<string>> GetRangeFromSortedSetByLowestScoreAsync(string setId, string fromStringScore, string toStringScore, int? skip, int? take, CancellationToken cancellationToken = default);
        ValueTask<List<string>> GetRangeFromSortedSetByLowestScoreAsync(string setId, double fromScore, double toScore, CancellationToken cancellationToken = default);
        ValueTask<List<string>> GetRangeFromSortedSetByLowestScoreAsync(string setId, long fromScore, long toScore, CancellationToken cancellationToken = default);
        ValueTask<List<string>> GetRangeFromSortedSetByLowestScoreAsync(string setId, double fromScore, double toScore, int? skip, int? take, CancellationToken cancellationToken = default);
        ValueTask<List<string>> GetRangeFromSortedSetByLowestScoreAsync(string setId, long fromScore, long toScore, int? skip, int? take, CancellationToken cancellationToken = default);
        ValueTask<IDictionary<string, double>> GetRangeWithScoresFromSortedSetByLowestScoreAsync(string setId, string fromStringScore, string toStringScore, CancellationToken cancellationToken = default);
        ValueTask<IDictionary<string, double>> GetRangeWithScoresFromSortedSetByLowestScoreAsync(string setId, string fromStringScore, string toStringScore, int? skip, int? take, CancellationToken cancellationToken = default);
        ValueTask<IDictionary<string, double>> GetRangeWithScoresFromSortedSetByLowestScoreAsync(string setId, double fromScore, double toScore, CancellationToken cancellationToken = default);
        ValueTask<IDictionary<string, double>> GetRangeWithScoresFromSortedSetByLowestScoreAsync(string setId, long fromScore, long toScore, CancellationToken cancellationToken = default);
        ValueTask<IDictionary<string, double>> GetRangeWithScoresFromSortedSetByLowestScoreAsync(string setId, double fromScore, double toScore, int? skip, int? take, CancellationToken cancellationToken = default);
        ValueTask<IDictionary<string, double>> GetRangeWithScoresFromSortedSetByLowestScoreAsync(string setId, long fromScore, long toScore, int? skip, int? take, CancellationToken cancellationToken = default);
        ValueTask<List<string>> GetRangeFromSortedSetByHighestScoreAsync(string setId, string fromStringScore, string toStringScore, CancellationToken cancellationToken = default);
        ValueTask<List<string>> GetRangeFromSortedSetByHighestScoreAsync(string setId, string fromStringScore, string toStringScore, int? skip, int? take, CancellationToken cancellationToken = default);
        ValueTask<List<string>> GetRangeFromSortedSetByHighestScoreAsync(string setId, double fromScore, double toScore, CancellationToken cancellationToken = default);
        ValueTask<List<string>> GetRangeFromSortedSetByHighestScoreAsync(string setId, long fromScore, long toScore, CancellationToken cancellationToken = default);
        ValueTask<List<string>> GetRangeFromSortedSetByHighestScoreAsync(string setId, double fromScore, double toScore, int? skip, int? take, CancellationToken cancellationToken = default);
        ValueTask<List<string>> GetRangeFromSortedSetByHighestScoreAsync(string setId, long fromScore, long toScore, int? skip, int? take, CancellationToken cancellationToken = default);
        ValueTask<IDictionary<string, double>> GetRangeWithScoresFromSortedSetByHighestScoreAsync(string setId, string fromStringScore, string toStringScore, CancellationToken cancellationToken = default);
        ValueTask<IDictionary<string, double>> GetRangeWithScoresFromSortedSetByHighestScoreAsync(string setId, string fromStringScore, string toStringScore, int? skip, int? take, CancellationToken cancellationToken = default);
        ValueTask<IDictionary<string, double>> GetRangeWithScoresFromSortedSetByHighestScoreAsync(string setId, double fromScore, double toScore, CancellationToken cancellationToken = default);
        ValueTask<IDictionary<string, double>> GetRangeWithScoresFromSortedSetByHighestScoreAsync(string setId, long fromScore, long toScore, CancellationToken cancellationToken = default);
        ValueTask<IDictionary<string, double>> GetRangeWithScoresFromSortedSetByHighestScoreAsync(string setId, double fromScore, double toScore, int? skip, int? take, CancellationToken cancellationToken = default);
        ValueTask<IDictionary<string, double>> GetRangeWithScoresFromSortedSetByHighestScoreAsync(string setId, long fromScore, long toScore, int? skip, int? take, CancellationToken cancellationToken = default);
        ValueTask<long> RemoveRangeFromSortedSetAsync(string setId, int minRank, int maxRank, CancellationToken cancellationToken = default);
        ValueTask<long> RemoveRangeFromSortedSetByScoreAsync(string setId, double fromScore, double toScore, CancellationToken cancellationToken = default);
        ValueTask<long> RemoveRangeFromSortedSetByScoreAsync(string setId, long fromScore, long toScore, CancellationToken cancellationToken = default);
        ValueTask<long> GetSortedSetCountAsync(string setId, CancellationToken cancellationToken = default);
        ValueTask<long> GetSortedSetCountAsync(string setId, string fromStringScore, string toStringScore, CancellationToken cancellationToken = default);
        ValueTask<long> GetSortedSetCountAsync(string setId, long fromScore, long toScore, CancellationToken cancellationToken = default);
        ValueTask<long> GetSortedSetCountAsync(string setId, double fromScore, double toScore, CancellationToken cancellationToken = default);
        ValueTask<double> GetItemScoreInSortedSetAsync(string setId, string value, CancellationToken cancellationToken = default);
        ValueTask<long> StoreIntersectFromSortedSetsAsync(string intoSetId, string[] setIds, CancellationToken cancellationToken = default);
        ValueTask<long> StoreIntersectFromSortedSetsAsync(string intoSetId, params string[] setIds); // convenience API
        ValueTask<long> StoreIntersectFromSortedSetsAsync(string intoSetId, string[] setIds, string[] args, CancellationToken cancellationToken = default);
        ValueTask<long> StoreUnionFromSortedSetsAsync(string intoSetId, string[] setIds, CancellationToken cancellationToken = default);
        ValueTask<long> StoreUnionFromSortedSetsAsync(string intoSetId, params string[] setIds); // convenience API
        ValueTask<long> StoreUnionFromSortedSetsAsync(string intoSetId, string[] setIds, string[] args, CancellationToken cancellationToken = default);
        ValueTask<List<string>> SearchSortedSetAsync(string setId, string start = null, string end = null, int? skip = null, int? take = null, CancellationToken cancellationToken = default);
        ValueTask<long> SearchSortedSetCountAsync(string setId, string start = null, string end = null, CancellationToken cancellationToken = default);
        ValueTask<long> RemoveRangeFromSortedSetBySearchAsync(string setId, string start = null, string end = null, CancellationToken cancellationToken = default);

        #endregion


        #region Hash operations

        ValueTask<bool> HashContainsEntryAsync(string hashId, string key, CancellationToken cancellationToken = default);
        ValueTask<bool> SetEntryInHashAsync(string hashId, string key, string value, CancellationToken cancellationToken = default);
        ValueTask<bool> SetEntryInHashIfNotExistsAsync(string hashId, string key, string value, CancellationToken cancellationToken = default);
        ValueTask SetRangeInHashAsync(string hashId, IEnumerable<KeyValuePair<string, string>> keyValuePairs, CancellationToken cancellationToken = default);
        ValueTask<long> IncrementValueInHashAsync(string hashId, string key, int incrementBy, CancellationToken cancellationToken = default);
        ValueTask<double> IncrementValueInHashAsync(string hashId, string key, double incrementBy, CancellationToken cancellationToken = default);
        ValueTask<string> GetValueFromHashAsync(string hashId, string key, CancellationToken cancellationToken = default);
        ValueTask<List<string>> GetValuesFromHashAsync(string hashId, string[] keys, CancellationToken cancellationToken = default);
        ValueTask<List<string>> GetValuesFromHashAsync(string hashId, params string[] keys); // convenience API
        ValueTask<bool> RemoveEntryFromHashAsync(string hashId, string key, CancellationToken cancellationToken = default);
        ValueTask<long> GetHashCountAsync(string hashId, CancellationToken cancellationToken = default);
        ValueTask<List<string>> GetHashKeysAsync(string hashId, CancellationToken cancellationToken = default);
        ValueTask<List<string>> GetHashValuesAsync(string hashId, CancellationToken cancellationToken = default);
        ValueTask<Dictionary<string, string>> GetAllEntriesFromHashAsync(string hashId, CancellationToken cancellationToken = default);

        #endregion


        #region Eval/Lua operations

        ValueTask<T> ExecCachedLuaAsync<T>(string scriptBody, Func<string, ValueTask<T>> scriptSha1, CancellationToken cancellationToken = default);

        ValueTask<RedisText> ExecLuaAsync(string body, string[] args, CancellationToken cancellationToken = default);
        ValueTask<RedisText> ExecLuaAsync(string body, params string[] args); // conveinence API
        ValueTask<RedisText> ExecLuaAsync(string luaBody, string[] keys, string[] args, CancellationToken cancellationToken = default);
        ValueTask<RedisText> ExecLuaShaAsync(string sha1, string[] args, CancellationToken cancellationToken = default);
        ValueTask<RedisText> ExecLuaShaAsync(string sha1, params string[] args); // convenience API
        ValueTask<RedisText> ExecLuaShaAsync(string sha1, string[] keys, string[] args, CancellationToken cancellationToken = default);

        ValueTask<string> ExecLuaAsStringAsync(string luaBody, string[] args, CancellationToken cancellationToken = default);
        ValueTask<string> ExecLuaAsStringAsync(string luaBody, params string[] args); // convenience API
        ValueTask<string> ExecLuaAsStringAsync(string luaBody, string[] keys, string[] args, CancellationToken cancellationToken = default);
        ValueTask<string> ExecLuaShaAsStringAsync(string sha1, string[] args, CancellationToken cancellationToken = default);
        ValueTask<string> ExecLuaShaAsStringAsync(string sha1, params string[] args); // convenience API
        ValueTask<string> ExecLuaShaAsStringAsync(string sha1, string[] keys, string[] args, CancellationToken cancellationToken = default);

        ValueTask<long> ExecLuaAsIntAsync(string luaBody, string[] args, CancellationToken cancellationToken = default);
        ValueTask<long> ExecLuaAsIntAsync(string luaBody, params string[] args); // convenience API
        ValueTask<long> ExecLuaAsIntAsync(string luaBody, string[] keys, string[] args, CancellationToken cancellationToken = default);
        ValueTask<long> ExecLuaShaAsIntAsync(string sha1, string[] args, CancellationToken cancellationToken = default);
        ValueTask<long> ExecLuaShaAsIntAsync(string sha1, params string[] args); // convenience API
        ValueTask<long> ExecLuaShaAsIntAsync(string sha1, string[] keys, string[] args, CancellationToken cancellationToken = default);

        ValueTask<List<string>> ExecLuaAsListAsync(string luaBody, string[] args, CancellationToken cancellationToken = default);
        ValueTask<List<string>> ExecLuaAsListAsync(string luaBody, params string[] args); // convenience API
        ValueTask<List<string>> ExecLuaAsListAsync(string luaBody, string[] keys, string[] args, CancellationToken cancellationToken = default);
        ValueTask<List<string>> ExecLuaShaAsListAsync(string sha1, string[] args, CancellationToken cancellationToken = default);
        ValueTask<List<string>> ExecLuaShaAsListAsync(string sha1, params string[] args); // convenience API
        ValueTask<List<string>> ExecLuaShaAsListAsync(string sha1, string[] keys, string[] args, CancellationToken cancellationToken = default);

        ValueTask<string> CalculateSha1Async(string luaBody, CancellationToken cancellationToken = default);

        ValueTask<bool> HasLuaScriptAsync(string sha1Ref, CancellationToken cancellationToken = default);
        ValueTask<Dictionary<string, bool>> WhichLuaScriptsExistsAsync(string[] sha1Refs, CancellationToken cancellationToken = default);
        ValueTask<Dictionary<string, bool>> WhichLuaScriptsExistsAsync(params string[] sha1Refs); // convenience API
        ValueTask RemoveAllLuaScriptsAsync(CancellationToken cancellationToken = default);
        ValueTask KillRunningLuaScriptAsync(CancellationToken cancellationToken = default);
        ValueTask<string> LoadLuaScriptAsync(string body, CancellationToken cancellationToken = default);

        #endregion

        ValueTask SlowlogResetAsync(CancellationToken cancellationToken = default);
        ValueTask<SlowlogItem[]> GetSlowlogAsync(int? numberOfRecords = null, CancellationToken cancellationToken = default);
    }
}