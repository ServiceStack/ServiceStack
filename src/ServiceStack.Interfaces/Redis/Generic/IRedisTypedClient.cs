//
// https://github.com/ServiceStack/ServiceStack.Redis
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
using ServiceStack.Data;
using ServiceStack.Model;

namespace ServiceStack.Redis.Generic
{
    public interface IRedisTypedClient<T>
        : IEntityStore<T>
    {
        IHasNamed<IRedisList<T>> Lists { get; set; }
        IHasNamed<IRedisSet<T>> Sets { get; set; }
        IHasNamed<IRedisSortedSet<T>> SortedSets { get; set; }
        IRedisHash<TKey, T> GetHash<TKey>(string hashId);

        IRedisTypedTransaction<T> CreateTransaction();
        IRedisTypedPipeline<T> CreatePipeline();

        IRedisClient RedisClient { get; }

        IDisposable AcquireLock();
        IDisposable AcquireLock(TimeSpan timeOut);

        long Db { get; set; }
        List<string> GetAllKeys();
        IRedisSet TypeIdsSet { get; }

        T this[string key] { get; set; }

        string SequenceKey { get; set; }
        void SetSequence(int value);
        long GetNextSequence();
        long GetNextSequence(int incrBy);
        RedisKeyType GetEntryType(string key);
        string GetRandomKey();

        void SetEntry(string key, T value);
        void SetEntry(string key, T value, TimeSpan expireIn);
        bool SetEntryIfNotExists(string key, T value);
        T GetValue(string key);
        T GetAndSetValue(string key, T value);
        bool ContainsKey(string key);
        bool RemoveEntry(string key);
        bool RemoveEntry(params string[] args);
        bool RemoveEntry(params IHasStringId[] entities);
        long IncrementValue(string key);
        long IncrementValueBy(string key, int count);
        long DecrementValue(string key);
        long DecrementValueBy(string key, int count);

        bool ExpireIn(object id, TimeSpan expiresAt);
        bool ExpireAt(object id, DateTime dateTime);
        bool ExpireEntryIn(string key, TimeSpan expiresAt);
        bool ExpireEntryAt(string key, DateTime dateTime);

        TimeSpan GetTimeToLive(string key);
        void Save();
        void SaveAsync();
        void FlushDb();
        void FlushAll();
        T[] SearchKeys(string pattern);
        List<T> GetValues(List<string> keys);
        List<T> GetSortedEntryValues(IRedisSet<T> fromSet, int startingFrom, int endingAt);

        void StoreAsHash(T entity);
        T GetFromHash(object id);

        //Set operations
        HashSet<T> GetAllItemsFromSet(IRedisSet<T> fromSet);
        void AddItemToSet(IRedisSet<T> toSet, T item);
        void RemoveItemFromSet(IRedisSet<T> fromSet, T item);
        T PopItemFromSet(IRedisSet<T> fromSet);
        void MoveBetweenSets(IRedisSet<T> fromSet, IRedisSet<T> toSet, T item);
        long GetSetCount(IRedisSet<T> set);
        bool SetContainsItem(IRedisSet<T> set, T item);
        HashSet<T> GetIntersectFromSets(params IRedisSet<T>[] sets);
        void StoreIntersectFromSets(IRedisSet<T> intoSet, params IRedisSet<T>[] sets);
        HashSet<T> GetUnionFromSets(params IRedisSet<T>[] sets);
        void StoreUnionFromSets(IRedisSet<T> intoSet, params IRedisSet<T>[] sets);
        HashSet<T> GetDifferencesFromSet(IRedisSet<T> fromSet, params IRedisSet<T>[] withSets);
        void StoreDifferencesFromSet(IRedisSet<T> intoSet, IRedisSet<T> fromSet, params IRedisSet<T>[] withSets);
        T GetRandomItemFromSet(IRedisSet<T> fromSet);

        //List operations
        List<T> GetAllItemsFromList(IRedisList<T> fromList);
        List<T> GetRangeFromList(IRedisList<T> fromList, int startingFrom, int endingAt);
        List<T> SortList(IRedisList<T> fromList, int startingFrom, int endingAt);
        void AddItemToList(IRedisList<T> fromList, T value);
        void PrependItemToList(IRedisList<T> fromList, T value);
        T RemoveStartFromList(IRedisList<T> fromList);
        T BlockingRemoveStartFromList(IRedisList<T> fromList, TimeSpan? timeOut);
        T RemoveEndFromList(IRedisList<T> fromList);
        void RemoveAllFromList(IRedisList<T> fromList);
        void TrimList(IRedisList<T> fromList, int keepStartingFrom, int keepEndingAt);
        long RemoveItemFromList(IRedisList<T> fromList, T value);
        long RemoveItemFromList(IRedisList<T> fromList, T value, int noOfMatches);
        long GetListCount(IRedisList<T> fromList);
        T GetItemFromList(IRedisList<T> fromList, int listIndex);
        void SetItemInList(IRedisList<T> toList, int listIndex, T value);
        void InsertBeforeItemInList(IRedisList<T> toList, T pivot, T value);
        void InsertAfterItemInList(IRedisList<T> toList, T pivot, T value);

        //Queue operations
        void EnqueueItemOnList(IRedisList<T> fromList, T item);
        T DequeueItemFromList(IRedisList<T> fromList);
        T BlockingDequeueItemFromList(IRedisList<T> fromList, TimeSpan? timeOut);

        //Stack operations
        void PushItemToList(IRedisList<T> fromList, T item);
        T PopItemFromList(IRedisList<T> fromList);
        T BlockingPopItemFromList(IRedisList<T> fromList, TimeSpan? timeOut);
        T PopAndPushItemBetweenLists(IRedisList<T> fromList, IRedisList<T> toList);
        T BlockingPopAndPushItemBetweenLists(IRedisList<T> fromList, IRedisList<T> toList, TimeSpan? timeOut);

        //Sorted Set operations
        void AddItemToSortedSet(IRedisSortedSet<T> toSet, T value);
        void AddItemToSortedSet(IRedisSortedSet<T> toSet, T value, double score);
        bool RemoveItemFromSortedSet(IRedisSortedSet<T> fromSet, T value);
        T PopItemWithLowestScoreFromSortedSet(IRedisSortedSet<T> fromSet);
        T PopItemWithHighestScoreFromSortedSet(IRedisSortedSet<T> fromSet);
        bool SortedSetContainsItem(IRedisSortedSet<T> set, T value);
        double IncrementItemInSortedSet(IRedisSortedSet<T> set, T value, double incrementBy);
        long GetItemIndexInSortedSet(IRedisSortedSet<T> set, T value);
        long GetItemIndexInSortedSetDesc(IRedisSortedSet<T> set, T value);
        List<T> GetAllItemsFromSortedSet(IRedisSortedSet<T> set);
        List<T> GetAllItemsFromSortedSetDesc(IRedisSortedSet<T> set);
        List<T> GetRangeFromSortedSet(IRedisSortedSet<T> set, int fromRank, int toRank);
        List<T> GetRangeFromSortedSetDesc(IRedisSortedSet<T> set, int fromRank, int toRank);
        IDictionary<T, double> GetAllWithScoresFromSortedSet(IRedisSortedSet<T> set);
        IDictionary<T, double> GetRangeWithScoresFromSortedSet(IRedisSortedSet<T> set, int fromRank, int toRank);
        IDictionary<T, double> GetRangeWithScoresFromSortedSetDesc(IRedisSortedSet<T> set, int fromRank, int toRank);
        List<T> GetRangeFromSortedSetByLowestScore(IRedisSortedSet<T> set, string fromStringScore, string toStringScore);
        List<T> GetRangeFromSortedSetByLowestScore(IRedisSortedSet<T> set, string fromStringScore, string toStringScore, int? skip, int? take);
        List<T> GetRangeFromSortedSetByLowestScore(IRedisSortedSet<T> set, double fromScore, double toScore);
        List<T> GetRangeFromSortedSetByLowestScore(IRedisSortedSet<T> set, double fromScore, double toScore, int? skip, int? take);
        IDictionary<T, double> GetRangeWithScoresFromSortedSetByLowestScore(IRedisSortedSet<T> set, string fromStringScore, string toStringScore);
        IDictionary<T, double> GetRangeWithScoresFromSortedSetByLowestScore(IRedisSortedSet<T> set, string fromStringScore, string toStringScore, int? skip, int? take);
        IDictionary<T, double> GetRangeWithScoresFromSortedSetByLowestScore(IRedisSortedSet<T> set, double fromScore, double toScore);
        IDictionary<T, double> GetRangeWithScoresFromSortedSetByLowestScore(IRedisSortedSet<T> set, double fromScore, double toScore, int? skip, int? take);
        List<T> GetRangeFromSortedSetByHighestScore(IRedisSortedSet<T> set, string fromStringScore, string toStringScore);
        List<T> GetRangeFromSortedSetByHighestScore(IRedisSortedSet<T> set, string fromStringScore, string toStringScore, int? skip, int? take);
        List<T> GetRangeFromSortedSetByHighestScore(IRedisSortedSet<T> set, double fromScore, double toScore);
        List<T> GetRangeFromSortedSetByHighestScore(IRedisSortedSet<T> set, double fromScore, double toScore, int? skip, int? take);
        IDictionary<T, double> GetRangeWithScoresFromSortedSetByHighestScore(IRedisSortedSet<T> set, string fromStringScore, string toStringScore);
        IDictionary<T, double> GetRangeWithScoresFromSortedSetByHighestScore(IRedisSortedSet<T> set, string fromStringScore, string toStringScore, int? skip, int? take);
        IDictionary<T, double> GetRangeWithScoresFromSortedSetByHighestScore(IRedisSortedSet<T> set, double fromScore, double toScore);
        IDictionary<T, double> GetRangeWithScoresFromSortedSetByHighestScore(IRedisSortedSet<T> set, double fromScore, double toScore, int? skip, int? take);
        long RemoveRangeFromSortedSet(IRedisSortedSet<T> set, int minRank, int maxRank);
        long RemoveRangeFromSortedSetByScore(IRedisSortedSet<T> set, double fromScore, double toScore);
        long GetSortedSetCount(IRedisSortedSet<T> set);
        double GetItemScoreInSortedSet(IRedisSortedSet<T> set, T value);
        long StoreIntersectFromSortedSets(IRedisSortedSet<T> intoSetId, params IRedisSortedSet<T>[] setIds);
        long StoreIntersectFromSortedSets(IRedisSortedSet<T> intoSetId, IRedisSortedSet<T>[] setIds, string[] args);
        long StoreUnionFromSortedSets(IRedisSortedSet<T> intoSetId, params IRedisSortedSet<T>[] setIds);
        long StoreUnionFromSortedSets(IRedisSortedSet<T> intoSetId, IRedisSortedSet<T>[] setIds, string[] args);

        //Hash operations
        bool HashContainsEntry<TKey>(IRedisHash<TKey, T> hash, TKey key);
        bool SetEntryInHash<TKey>(IRedisHash<TKey, T> hash, TKey key, T value);
        bool SetEntryInHashIfNotExists<TKey>(IRedisHash<TKey, T> hash, TKey key, T value);
        void SetRangeInHash<TKey>(IRedisHash<TKey, T> hash, IEnumerable<KeyValuePair<TKey, T>> keyValuePairs);
        T GetValueFromHash<TKey>(IRedisHash<TKey, T> hash, TKey key);
        bool RemoveEntryFromHash<TKey>(IRedisHash<TKey, T> hash, TKey key);
        long GetHashCount<TKey>(IRedisHash<TKey, T> hash);
        List<TKey> GetHashKeys<TKey>(IRedisHash<TKey, T> hash);
        List<T> GetHashValues<TKey>(IRedisHash<TKey, T> hash);
        Dictionary<TKey, T> GetAllEntriesFromHash<TKey>(IRedisHash<TKey, T> hash);

        //Useful common app-logic 
        void StoreRelatedEntities<TChild>(object parentId, List<TChild> children);
        void StoreRelatedEntities<TChild>(object parentId, params TChild[] children);
        void DeleteRelatedEntities<TChild>(object parentId);
        void DeleteRelatedEntity<TChild>(object parentId, object childId);
        List<TChild> GetRelatedEntities<TChild>(object parentId);
        long GetRelatedEntitiesCount<TChild>(object parentId);
        void AddToRecentsList(T value);
        List<T> GetLatestFromRecentsList(int skip, int take);
        List<T> GetEarliestFromRecentsList(int skip, int take);
    }

}