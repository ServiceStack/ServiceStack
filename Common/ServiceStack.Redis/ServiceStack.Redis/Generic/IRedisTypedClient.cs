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
using ServiceStack.DataAccess;
using ServiceStack.DesignPatterns.Model;

namespace ServiceStack.Redis.Generic
{
	public interface IRedisTypedClient<T> 
		: IBasicPersistenceProvider<T>
	{
		IHasNamed<IRedisList<T>> Lists { get; set; }
		IHasNamed<IRedisSet<T>> Sets { get; set; }

		int Db { get; set; }
		string[] AllKeys { get; }

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
		
		List<T> GetRangeFromSortedSet(IRedisSet<T> fromSet, int startingFrom, int endingAt);
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
		List<T> GetRangeFromSortedList(IRedisList<T> fromList, int startingFrom, int endingAt);
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
	}
}