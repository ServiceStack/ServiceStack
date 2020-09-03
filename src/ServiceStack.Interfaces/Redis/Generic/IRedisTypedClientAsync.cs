//
// https://github.com/ServiceStack/ServiceStack.Redis
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2017 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Data;
using ServiceStack.Model;

namespace ServiceStack.Redis.Generic
{
    public interface IRedisTypedClientAsync<T> : IEntityStoreAsync<T>
    {
        IHasNamed<IRedisListAsync<T>> Lists { get; }
        IHasNamed<IRedisSetAsync<T>> Sets { get; }
        IHasNamed<IRedisSortedSetAsync<T>> SortedSets { get; }
        IRedisHashAsync<TKey, T> GetHash<TKey>(string hashId);
        IRedisSetAsync TypeIdsSet { get; }

        // not provided: use GetValueAsync/SetValueAsync instead
        // T this[string key] { get; set; }

        ValueTask<IRedisTypedTransactionAsync<T>> CreateTransactionAsync(CancellationToken token = default);
        IRedisTypedPipelineAsync<T> CreatePipeline();

        IRedisClientAsync RedisClient { get; }

        ValueTask<IAsyncDisposable> AcquireLockAsync(TimeSpan? timeOut = default, CancellationToken token = default);

        long Db { get; }
        ValueTask SelectAsync(long db, CancellationToken token = default);

        ValueTask<List<string>> GetAllKeysAsync(CancellationToken token = default);

        string UrnKey(T value);

        string SequenceKey { get; set; }
        ValueTask SetSequenceAsync(int value, CancellationToken token = default);
        ValueTask<long> GetNextSequenceAsync(CancellationToken token = default);
        ValueTask<long> GetNextSequenceAsync(int incrBy, CancellationToken token = default);
        ValueTask<RedisKeyType> GetEntryTypeAsync(string key, CancellationToken token = default);
        ValueTask<string> GetRandomKeyAsync(CancellationToken token = default);

        ValueTask SetValueAsync(string key, T entity, CancellationToken token = default);
        ValueTask SetValueAsync(string key, T entity, TimeSpan expireIn, CancellationToken token = default);
        ValueTask<bool> SetValueIfNotExistsAsync(string key, T entity, CancellationToken token = default);
        ValueTask<bool> SetValueIfExistsAsync(string key, T entity, CancellationToken token = default);

        ValueTask<T> StoreAsync(T entity, TimeSpan expireIn, CancellationToken token = default);

        ValueTask<T> GetValueAsync(string key, CancellationToken token = default);
        ValueTask<T> GetAndSetValueAsync(string key, T value, CancellationToken token = default);
        ValueTask<bool> ContainsKeyAsync(string key, CancellationToken token = default);
        ValueTask<bool> RemoveEntryAsync(string key, CancellationToken token = default);
        ValueTask<bool> RemoveEntryAsync(string[] args, CancellationToken token = default);
        ValueTask<bool> RemoveEntryAsync(params string[] args); // convenience API
        ValueTask<bool> RemoveEntryAsync(IHasStringId[] entities, CancellationToken token = default);
        ValueTask<bool> RemoveEntryAsync(params IHasStringId[] entities); // convenience API
        ValueTask<long> IncrementValueAsync(string key, CancellationToken token = default);
        ValueTask<long> IncrementValueByAsync(string key, int count, CancellationToken token = default);
        ValueTask<long> DecrementValueAsync(string key, CancellationToken token = default);
        ValueTask<long> DecrementValueByAsync(string key, int count, CancellationToken token = default);

        ValueTask<bool> ExpireInAsync(object id, TimeSpan expiresAt, CancellationToken token = default);
        ValueTask<bool> ExpireAtAsync(object id, DateTime dateTime, CancellationToken token = default);
        ValueTask<bool> ExpireEntryInAsync(string key, TimeSpan expiresAt, CancellationToken token = default);
        ValueTask<bool> ExpireEntryAtAsync(string key, DateTime dateTime, CancellationToken token = default);

        ValueTask<TimeSpan> GetTimeToLiveAsync(string key, CancellationToken token = default);
        ValueTask ForegroundSaveAsync(CancellationToken token = default);
        ValueTask BackgroundSaveAsync(CancellationToken token = default);
        ValueTask FlushDbAsync(CancellationToken token = default);
        ValueTask FlushAllAsync(CancellationToken token = default);
        ValueTask<T[]> SearchKeysAsync(string pattern, CancellationToken token = default);
        ValueTask<List<T>> GetValuesAsync(List<string> keys, CancellationToken token = default);
        ValueTask<List<T>> GetSortedEntryValuesAsync(IRedisSetAsync<T> fromSet, int startingFrom, int endingAt, CancellationToken token = default);
        ValueTask StoreAsHashAsync(T entity, CancellationToken token = default);
        ValueTask<T> GetFromHashAsync(object id, CancellationToken token = default);

        //Set operations
        ValueTask<HashSet<T>> GetAllItemsFromSetAsync(IRedisSetAsync<T> fromSet, CancellationToken token = default);
        ValueTask AddItemToSetAsync(IRedisSetAsync<T> toSet, T item, CancellationToken token = default);
        ValueTask RemoveItemFromSetAsync(IRedisSetAsync<T> fromSet, T item, CancellationToken token = default);
        ValueTask<T> PopItemFromSetAsync(IRedisSetAsync<T> fromSet, CancellationToken token = default);
        ValueTask MoveBetweenSetsAsync(IRedisSetAsync<T> fromSet, IRedisSetAsync<T> toSet, T item, CancellationToken token = default);
        ValueTask<long> GetSetCountAsync(IRedisSetAsync<T> set, CancellationToken token = default);
        ValueTask<bool> SetContainsItemAsync(IRedisSetAsync<T> set, T item, CancellationToken token = default);
        ValueTask<HashSet<T>> GetIntersectFromSetsAsync(IRedisSetAsync<T>[] sets, CancellationToken token = default);
        ValueTask<HashSet<T>> GetIntersectFromSetsAsync(params IRedisSetAsync<T>[] sets);
        ValueTask StoreIntersectFromSetsAsync(IRedisSetAsync<T> intoSet, IRedisSetAsync<T>[] sets, CancellationToken token = default);
        ValueTask StoreIntersectFromSetsAsync(IRedisSetAsync<T> intoSet, params IRedisSetAsync<T>[] sets); // convenience API
        ValueTask<HashSet<T>> GetUnionFromSetsAsync(IRedisSetAsync<T>[] sets, CancellationToken token = default);
        ValueTask<HashSet<T>> GetUnionFromSetsAsync(params IRedisSetAsync<T>[] sets); // convenience API
        ValueTask StoreUnionFromSetsAsync(IRedisSetAsync<T> intoSet, IRedisSetAsync<T>[] sets, CancellationToken token = default);
        ValueTask StoreUnionFromSetsAsync(IRedisSetAsync<T> intoSet, params IRedisSetAsync<T>[] sets); // convenience API
        ValueTask<HashSet<T>> GetDifferencesFromSetAsync(IRedisSetAsync<T> fromSet, IRedisSetAsync<T>[] withSets, CancellationToken token = default);
        ValueTask<HashSet<T>> GetDifferencesFromSetAsync(IRedisSetAsync<T> fromSet, params IRedisSetAsync<T>[] withSets); // convenience API
        ValueTask StoreDifferencesFromSetAsync(IRedisSetAsync<T> intoSet, IRedisSetAsync<T> fromSet, IRedisSetAsync<T>[] withSets, CancellationToken token = default);
        ValueTask StoreDifferencesFromSetAsync(IRedisSetAsync<T> intoSet, IRedisSetAsync<T> fromSet, params IRedisSetAsync<T>[] withSets); // convenience API
        ValueTask<T> GetRandomItemFromSetAsync(IRedisSetAsync<T> fromSet, CancellationToken token = default);

        //List operations
        ValueTask<List<T>> GetAllItemsFromListAsync(IRedisListAsync<T> fromList, CancellationToken token = default);
        ValueTask<List<T>> GetRangeFromListAsync(IRedisListAsync<T> fromList, int startingFrom, int endingAt, CancellationToken token = default);
        ValueTask<List<T>> SortListAsync(IRedisListAsync<T> fromList, int startingFrom, int endingAt, CancellationToken token = default);
        ValueTask AddItemToListAsync(IRedisListAsync<T> fromList, T value, CancellationToken token = default);
        ValueTask PrependItemToListAsync(IRedisListAsync<T> fromList, T value, CancellationToken token = default);
        ValueTask<T> RemoveStartFromListAsync(IRedisListAsync<T> fromList, CancellationToken token = default);
        ValueTask<T> BlockingRemoveStartFromListAsync(IRedisListAsync<T> fromList, TimeSpan? timeOut, CancellationToken token = default);
        ValueTask<T> RemoveEndFromListAsync(IRedisListAsync<T> fromList, CancellationToken token = default);
        ValueTask RemoveAllFromListAsync(IRedisListAsync<T> fromList, CancellationToken token = default);
        ValueTask TrimListAsync(IRedisListAsync<T> fromList, int keepStartingFrom, int keepEndingAt, CancellationToken token = default);
        ValueTask<long> RemoveItemFromListAsync(IRedisListAsync<T> fromList, T value, CancellationToken token = default);
        ValueTask<long> RemoveItemFromListAsync(IRedisListAsync<T> fromList, T value, int noOfMatches, CancellationToken token = default);
        ValueTask<long> GetListCountAsync(IRedisListAsync<T> fromList, CancellationToken token = default);
        ValueTask<T> GetItemFromListAsync(IRedisListAsync<T> fromList, int listIndex, CancellationToken token = default);
        ValueTask SetItemInListAsync(IRedisListAsync<T> toList, int listIndex, T value, CancellationToken token = default);
        ValueTask InsertBeforeItemInListAsync(IRedisListAsync<T> toList, T pivot, T value, CancellationToken token = default);
        ValueTask InsertAfterItemInListAsync(IRedisListAsync<T> toList, T pivot, T value, CancellationToken token = default);

        //Queue operations
        ValueTask EnqueueItemOnListAsync(IRedisListAsync<T> fromList, T item, CancellationToken token = default);
        ValueTask<T> DequeueItemFromListAsync(IRedisListAsync<T> fromList, CancellationToken token = default);
        ValueTask<T> BlockingDequeueItemFromListAsync(IRedisListAsync<T> fromList, TimeSpan? timeOut, CancellationToken token = default);

        //Stack operations
        ValueTask PushItemToListAsync(IRedisListAsync<T> fromList, T item, CancellationToken token = default);
        ValueTask<T> PopItemFromListAsync(IRedisListAsync<T> fromList, CancellationToken token = default);
        ValueTask<T> BlockingPopItemFromListAsync(IRedisListAsync<T> fromList, TimeSpan? timeOut, CancellationToken token = default);
        ValueTask<T> PopAndPushItemBetweenListsAsync(IRedisListAsync<T> fromList, IRedisListAsync<T> toList, CancellationToken token = default);
        ValueTask<T> BlockingPopAndPushItemBetweenListsAsync(IRedisListAsync<T> fromList, IRedisListAsync<T> toList, TimeSpan? timeOut, CancellationToken token = default);

        //Sorted Set operations
        ValueTask AddItemToSortedSetAsync(IRedisSortedSetAsync<T> toSet, T value, CancellationToken token = default);
        ValueTask AddItemToSortedSetAsync(IRedisSortedSetAsync<T> toSet, T value, double score, CancellationToken token = default);
        ValueTask<bool> RemoveItemFromSortedSetAsync(IRedisSortedSetAsync<T> fromSet, T value, CancellationToken token = default);
        ValueTask<T> PopItemWithLowestScoreFromSortedSetAsync(IRedisSortedSetAsync<T> fromSet, CancellationToken token = default);
        ValueTask<T> PopItemWithHighestScoreFromSortedSetAsync(IRedisSortedSetAsync<T> fromSet, CancellationToken token = default);
        ValueTask<bool> SortedSetContainsItemAsync(IRedisSortedSetAsync<T> set, T value, CancellationToken token = default);
        ValueTask<double> IncrementItemInSortedSetAsync(IRedisSortedSetAsync<T> set, T value, double incrementBy, CancellationToken token = default);
        ValueTask<long> GetItemIndexInSortedSetAsync(IRedisSortedSetAsync<T> set, T value, CancellationToken token = default);
        ValueTask<long> GetItemIndexInSortedSetDescAsync(IRedisSortedSetAsync<T> set, T value, CancellationToken token = default);
        ValueTask<List<T>> GetAllItemsFromSortedSetAsync(IRedisSortedSetAsync<T> set, CancellationToken token = default);
        ValueTask<List<T>> GetAllItemsFromSortedSetDescAsync(IRedisSortedSetAsync<T> set, CancellationToken token = default);
        ValueTask<List<T>> GetRangeFromSortedSetAsync(IRedisSortedSetAsync<T> set, int fromRank, int toRank, CancellationToken token = default);
        ValueTask<List<T>> GetRangeFromSortedSetDescAsync(IRedisSortedSetAsync<T> set, int fromRank, int toRank, CancellationToken token = default);
        ValueTask<IDictionary<T, double>> GetAllWithScoresFromSortedSetAsync(IRedisSortedSetAsync<T> set, CancellationToken token = default);
        ValueTask<IDictionary<T, double>> GetRangeWithScoresFromSortedSetAsync(IRedisSortedSetAsync<T> set, int fromRank, int toRank, CancellationToken token = default);
        ValueTask<IDictionary<T, double>> GetRangeWithScoresFromSortedSetDescAsync(IRedisSortedSetAsync<T> set, int fromRank, int toRank, CancellationToken token = default);
        ValueTask<List<T>> GetRangeFromSortedSetByLowestScoreAsync(IRedisSortedSetAsync<T> set, string fromStringScore, string toStringScore, CancellationToken token = default);
        ValueTask<List<T>> GetRangeFromSortedSetByLowestScoreAsync(IRedisSortedSetAsync<T> set, string fromStringScore, string toStringScore, int? skip, int? take, CancellationToken token = default);
        ValueTask<List<T>> GetRangeFromSortedSetByLowestScoreAsync(IRedisSortedSetAsync<T> set, double fromScore, double toScore, CancellationToken token = default);
        ValueTask<List<T>> GetRangeFromSortedSetByLowestScoreAsync(IRedisSortedSetAsync<T> set, double fromScore, double toScore, int? skip, int? take, CancellationToken token = default);
        ValueTask<IDictionary<T, double>> GetRangeWithScoresFromSortedSetByLowestScoreAsync(IRedisSortedSetAsync<T> set, string fromStringScore, string toStringScore, CancellationToken token = default);
        ValueTask<IDictionary<T, double>> GetRangeWithScoresFromSortedSetByLowestScoreAsync(IRedisSortedSetAsync<T> set, string fromStringScore, string toStringScore, int? skip, int? take, CancellationToken token = default);
        ValueTask<IDictionary<T, double>> GetRangeWithScoresFromSortedSetByLowestScoreAsync(IRedisSortedSetAsync<T> set, double fromScore, double toScore, CancellationToken token = default);
        ValueTask<IDictionary<T, double>> GetRangeWithScoresFromSortedSetByLowestScoreAsync(IRedisSortedSetAsync<T> set, double fromScore, double toScore, int? skip, int? take, CancellationToken token = default);
        ValueTask<List<T>> GetRangeFromSortedSetByHighestScoreAsync(IRedisSortedSetAsync<T> set, string fromStringScore, string toStringScore, CancellationToken token = default);
        ValueTask<List<T>> GetRangeFromSortedSetByHighestScoreAsync(IRedisSortedSetAsync<T> set, string fromStringScore, string toStringScore, int? skip, int? take, CancellationToken token = default);
        ValueTask<List<T>> GetRangeFromSortedSetByHighestScoreAsync(IRedisSortedSetAsync<T> set, double fromScore, double toScore, CancellationToken token = default);
        ValueTask<List<T>> GetRangeFromSortedSetByHighestScoreAsync(IRedisSortedSetAsync<T> set, double fromScore, double toScore, int? skip, int? take, CancellationToken token = default);
        ValueTask<IDictionary<T, double>> GetRangeWithScoresFromSortedSetByHighestScoreAsync(IRedisSortedSetAsync<T> set, string fromStringScore, string toStringScore, CancellationToken token = default);
        ValueTask<IDictionary<T, double>> GetRangeWithScoresFromSortedSetByHighestScoreAsync(IRedisSortedSetAsync<T> set, string fromStringScore, string toStringScore, int? skip, int? take, CancellationToken token = default);
        ValueTask<IDictionary<T, double>> GetRangeWithScoresFromSortedSetByHighestScoreAsync(IRedisSortedSetAsync<T> set, double fromScore, double toScore, CancellationToken token = default);
        ValueTask<IDictionary<T, double>> GetRangeWithScoresFromSortedSetByHighestScoreAsync(IRedisSortedSetAsync<T> set, double fromScore, double toScore, int? skip, int? take, CancellationToken token = default);
        ValueTask<long> RemoveRangeFromSortedSetAsync(IRedisSortedSetAsync<T> set, int minRank, int maxRank, CancellationToken token = default);
        ValueTask<long> RemoveRangeFromSortedSetByScoreAsync(IRedisSortedSetAsync<T> set, double fromScore, double toScore, CancellationToken token = default);
        ValueTask<long> GetSortedSetCountAsync(IRedisSortedSetAsync<T> set, CancellationToken token = default);
        ValueTask<double> GetItemScoreInSortedSetAsync(IRedisSortedSetAsync<T> set, T value, CancellationToken token = default);
        ValueTask<long> StoreIntersectFromSortedSetsAsync(IRedisSortedSetAsync<T> intoSetId, IRedisSortedSetAsync<T>[] setIds, CancellationToken token = default);
        ValueTask<long> StoreIntersectFromSortedSetsAsync(IRedisSortedSetAsync<T> intoSetId, params IRedisSortedSetAsync<T>[] setIds); // convenience API
        ValueTask<long> StoreIntersectFromSortedSetsAsync(IRedisSortedSetAsync<T> intoSetId, IRedisSortedSetAsync<T>[] setIds, string[] args, CancellationToken token = default);
        ValueTask<long> StoreUnionFromSortedSetsAsync(IRedisSortedSetAsync<T> intoSetId, IRedisSortedSetAsync<T>[] setIds, CancellationToken token = default);
        ValueTask<long> StoreUnionFromSortedSetsAsync(IRedisSortedSetAsync<T> intoSetId, params IRedisSortedSetAsync<T>[] setIds); // convenience API
        ValueTask<long> StoreUnionFromSortedSetsAsync(IRedisSortedSetAsync<T> intoSetId, IRedisSortedSetAsync<T>[] setIds, string[] args, CancellationToken token = default);

        //Hash operations
        ValueTask<bool> HashContainsEntryAsync<TKey>(IRedisHashAsync<TKey, T> hash, TKey key, CancellationToken token = default);
        ValueTask<bool> SetEntryInHashAsync<TKey>(IRedisHashAsync<TKey, T> hash, TKey key, T value, CancellationToken token = default);
        ValueTask<bool> SetEntryInHashIfNotExistsAsync<TKey>(IRedisHashAsync<TKey, T> hash, TKey key, T value, CancellationToken token = default);
        ValueTask SetRangeInHashAsync<TKey>(IRedisHashAsync<TKey, T> hash, IEnumerable<KeyValuePair<TKey, T>> keyValuePairs, CancellationToken token = default);
        ValueTask<T> GetValueFromHashAsync<TKey>(IRedisHashAsync<TKey, T> hash, TKey key, CancellationToken token = default);
        ValueTask<bool> RemoveEntryFromHashAsync<TKey>(IRedisHashAsync<TKey, T> hash, TKey key, CancellationToken token = default);
        ValueTask<long> GetHashCountAsync<TKey>(IRedisHashAsync<TKey, T> hash, CancellationToken token = default);
        ValueTask<List<TKey>> GetHashKeysAsync<TKey>(IRedisHashAsync<TKey, T> hash, CancellationToken token = default);
        ValueTask<List<T>> GetHashValuesAsync<TKey>(IRedisHashAsync<TKey, T> hash, CancellationToken token = default);
        ValueTask<Dictionary<TKey, T>> GetAllEntriesFromHashAsync<TKey>(IRedisHashAsync<TKey, T> hash, CancellationToken token = default);

        //Useful common app-logic 
        ValueTask StoreRelatedEntitiesAsync<TChild>(object parentId, List<TChild> children, CancellationToken token = default);
        ValueTask StoreRelatedEntitiesAsync<TChild>(object parentId, TChild[] children, CancellationToken token = default);
        ValueTask StoreRelatedEntitiesAsync<TChild>(object parentId, params TChild[] children); // convenience API
        ValueTask DeleteRelatedEntitiesAsync<TChild>(object parentId, CancellationToken token = default);
        ValueTask DeleteRelatedEntityAsync<TChild>(object parentId, object childId, CancellationToken token = default);
        ValueTask<List<TChild>> GetRelatedEntitiesAsync<TChild>(object parentId, CancellationToken token = default);
        ValueTask<long> GetRelatedEntitiesCountAsync<TChild>(object parentId, CancellationToken token = default);
        ValueTask AddToRecentsListAsync(T value, CancellationToken token = default);
        ValueTask<List<T>> GetLatestFromRecentsListAsync(int skip, int take, CancellationToken token = default);
        ValueTask<List<T>> GetEarliestFromRecentsListAsync(int skip, int take, CancellationToken token = default);
    }

}