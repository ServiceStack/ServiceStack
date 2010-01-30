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
using ServiceStack.DesignPatterns.Model;

namespace ServiceStack.Redis.Generic
{
	public interface IRedisGenericClient<T> 
		: IDisposable
	{
		string Host { get; }
		int Port { get; }
		int RetryTimeout { get; set; }
		int RetryCount { get; set; }
		int SendTimeout { get; set; }
		string Password { get; set; }

		IHasNamedList<T> Lists { get; set; }
		IHasNamedCollection<T> Sets { get; set; }

		Dictionary<string, string> Info { get; }
		int Db { get; set; }
		int DbSize { get; }
		DateTime LastSave { get; }
		string[] AllKeys { get; }

		T this[string key] { get; set; }
		void Set(string key, T value);
		bool SetIfNotExists(string key, T value);
		T Get(string key);
		T GetAndSet(string key, T value);
		bool ContainsKey(string key);
		bool Remove(string key);
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
		void FlushAll();
		T[] GetKeys(string pattern);
		List<T> GetKeyValues(List<string> keys);
		List<T> GetRangeFromSortedSet(string setId, int startingFrom, int endingAt);
		HashSet<T> GetAllFromSet(string setId);
		void AddToSet(string setId, T value);
		void RemoveFromSet(string setId, T value);
		T PopFromSet(string setId);
		void MoveBetweenSets(string fromSetId, string toSetId, T value);
		int GetSetCount(string setId);
		bool SetContainsValue(string setId, T value);
		HashSet<T> GetIntersectFromSets(params string[] setIds);
		void StoreIntersectFromSets(string intoSetId, params string[] setIds);
		HashSet<T> GetUnionFromSets(params string[] setIds);
		void StoreUnionFromSets(string intoSetId, params string[] setIds);
		HashSet<T> GetDifferencesFromSet(string fromSetId, params string[] withSetIds);
		void StoreDifferencesFromSet(string intoSetId, string fromSetId, params string[] withSetIds);
		T GetRandomEntryFromSet(string setId);
		List<T> GetAllFromList(string listId);
		List<T> GetRangeFromList(string listId, int startingFrom, int endingAt);
		List<T> GetRangeFromSortedList(string listId, int startingFrom, int endingAt);
		void AddToList(string listId, T value);
		void PrependToList(string listId, T value);
		void RemoveAllFromList(string listId);
		void TrimList(string listId, int keepStartingFrom, int keepEndingAt);
		int RemoveValueFromList(string listId, T value);
		int RemoveValueFromList(string listId, T value, int noOfMatches);
		int GetListCount(string setId);
		T GetItemFromList(string listId, int listIndex);
		void SetItemInList(string listId, int listIndex, T value);
		T DequeueFromList(string listId);
		T PopFromList(string listId);
		void PopAndPushBetweenLists(string fromListId, string toListId);
	}
}