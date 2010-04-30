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
using ServiceStack.DataAccess;
using ServiceStack.DesignPatterns.Model;

namespace ServiceStack.Redis.Generic
{
	public interface IRedisTypedClient<T>
		: IBasicPersistenceProvider<T>
	{
		IHasNamed<IRedisList<T>> Lists { get; set; }
		IHasNamed<IRedisSet<T>> Sets { get; set; }
		IHasNamed<IRedisSortedSet<T>> SortedSets { get; set; }
		IRedisHash<TKey, T> GetHash<TKey>(string hashId);

		IRedisTypedTransaction<T> CreateTransaction();

		int Db { get; set; }
		List<string> AllKeys { get; }

		T this[string key] { get; set; }

		string SequenceKey { get; set; }
		void SetSequence(int value);
		int GetNextSequence();
		RedisKeyType GetKeyType(string key);
		string NewRandomKey();

		void Set(string key, T value);
		bool SetIfNotExists(string key, T value);
		T Get(string key);
		T GetAndSet(string key, T value);
		bool ContainsKey(string key);
		bool Remove(string key);
		bool Remove(params string[] args);
		bool Remove(params IHasStringId[] entities);
		int Increment(string key);
		int IncrementBy(string key, int count);
		int Decrement(string key);
		int DecrementBy(string key, int count);
		bool ExpireKeyIn(string key, TimeSpan expiresAt);
		bool ExpireKeyAt(string key, DateTime dateTime);
		TimeSpan GetTimeToLive(string key);
		string Save();
		void SaveAsync();
		void FlushDb();
		void FlushAll();
		T[] GetKeys(string pattern);
		List<T> GetKeyValues(List<string> keys);

		List<T> SortSet(IRedisSet<T> fromSet, int startingFrom, int endingAt);
		HashSet<T> GetAllFromSet(IRedisSet<T> fromSet);
		void AddToSet(IRedisSet<T> toSet, T value);
		void RemoveFromSet(IRedisSet<T> fromSet, T value);
		T PopFromSet(IRedisSet<T> fromSet);
		void MoveBetweenSets(IRedisSet<T> fromSet, IRedisSet<T> toSet, T value);
		int GetSetCount(IRedisSet<T> set);
		bool SetContainsValue(IRedisSet<T> set, T value);
		HashSet<T> GetIntersectFromSets(params IRedisSet<T>[] sets);
		void StoreIntersectFromSets(IRedisSet<T> intoSet, params IRedisSet<T>[] sets);
		HashSet<T> GetUnionFromSets(params IRedisSet<T>[] sets);
		void StoreUnionFromSets(IRedisSet<T> intoSet, params IRedisSet<T>[] sets);
		HashSet<T> GetDifferencesFromSet(IRedisSet<T> fromSet, params IRedisSet<T>[] withSets);
		void StoreDifferencesFromSet(IRedisSet<T> intoSet, IRedisSet<T> fromSet, params IRedisSet<T>[] withSets);
		T GetRandomEntryFromSet(IRedisSet<T> fromSet);

		List<T> GetAllFromList(IRedisList<T> fromList);
		List<T> GetRangeFromList(IRedisList<T> fromList, int startingFrom, int endingAt);
		List<T> SortList(IRedisList<T> fromList, int startingFrom, int endingAt);
		void AddToList(IRedisList<T> fromList, T value);
		void PrependToList(IRedisList<T> fromList, T value);
		void RemoveAllFromList(IRedisList<T> fromList);
		void TrimList(IRedisList<T> fromList, int keepStartingFrom, int keepEndingAt);
		int RemoveValueFromList(IRedisList<T> fromList, T value);
		int RemoveValueFromList(IRedisList<T> fromList, T value, int noOfMatches);
		int GetListCount(IRedisList<T> fromList);
		T GetItemFromList(IRedisList<T> fromList, int listIndex);
		void SetItemInList(IRedisList<T> toList, int listIndex, T value);
		T DequeueFromList(IRedisList<T> fromList);
		T PopFromList(IRedisList<T> fromList);
		T PopAndPushBetweenLists(IRedisList<T> fromList, IRedisList<T> toList);

		void AddToSortedSet(IRedisSortedSet<T> toSet, T value);
		void AddToSortedSet(IRedisSortedSet<T> toSet, T value, double score);
		void RemoveFromSortedSet(IRedisSortedSet<T> fromSet, T value);
		T PopItemWithLowestScoreFromSortedSet(IRedisSortedSet<T> fromSet);
		T PopItemWithHighestScoreFromSortedSet(IRedisSortedSet<T> fromSet);
		bool SortedSetContainsValue(IRedisSortedSet<T> set, T value);
		double IncrementItemInSortedSet(IRedisSortedSet<T> set, T value, double incrementBy);
		int GetItemIndexInSortedSet(IRedisSortedSet<T> set, T value);
		int GetItemIndexInSortedSetDesc(IRedisSortedSet<T> set, T value);
		List<T> GetAllFromSortedSet(IRedisSortedSet<T> set);
		List<T> GetAllFromSortedSetDesc(IRedisSortedSet<T> set);
		List<T> GetRangeFromSortedSet(IRedisSortedSet<T> set, int fromRank, int toRank);
		List<T> GetRangeFromSortedSetDesc(IRedisSortedSet<T> set, int fromRank, int toRank);
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
		int RemoveRangeFromSortedSet(IRedisSortedSet<T> set, int minRank, int maxRank);
		int RemoveRangeFromSortedSetByScore(IRedisSortedSet<T> set, double fromScore, double toScore);
		int GetSortedSetCount(IRedisSortedSet<T> set);
		double GetItemScoreInSortedSet(IRedisSortedSet<T> set, T value);
		int StoreIntersectFromSortedSets(IRedisSortedSet<T> intoSetId, params IRedisSortedSet<T>[] setIds);
		int StoreUnionFromSortedSets(IRedisSortedSet<T> intoSetId, params IRedisSortedSet<T>[] setIds);

		bool HashContainsKey<TKey>(IRedisHash<TKey, T> hash, TKey key);
		bool SetItemInHash<TKey>(IRedisHash<TKey, T> hash, TKey key, T value);
		bool SetItemInHashIfNotExists<TKey>(IRedisHash<TKey, T> hash, TKey key, T value);
		void SetRangeInHash<TKey>(IRedisHash<TKey, T> hash, IEnumerable<KeyValuePair<TKey, T>> keyValuePairs);
		T GetItemFromHash<TKey>(IRedisHash<TKey, T> hash, TKey key);
		bool RemoveFromHash<TKey>(IRedisHash<TKey, T> hash, TKey key);
		int GetHashCount<TKey>(IRedisHash<TKey, T> hash);
		List<TKey> GetHashKeys<TKey>(IRedisHash<TKey, T> hash);
		List<T> GetHashValues<TKey>(IRedisHash<TKey, T> hash);
		Dictionary<TKey, T> GetAllFromHash<TKey>(IRedisHash<TKey, T> hash);
	}

}