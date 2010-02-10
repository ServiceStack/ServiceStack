using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.Redis.Generic;

namespace ServiceStack.Redis
{
	public class RedisPooledClientsManager
		: IRedisClient
	{
		private List<IPEndPoint> WriteOnlyHosts { get; set; }
		private List<IPEndPoint> ReadOnlyHosts { get; set; }

		public RedisPooledClientsManager(params string[] readWriteHosts)
		{
			ReadOnlyHosts = ConvertToIpEndPoints(readWriteHosts);

			WriteOnlyHosts = ConvertToIpEndPoints(readWriteHosts);
		}

		private static List<IPEndPoint> ConvertToIpEndPoints(IEnumerable<string> hosts)
		{
			return hosts.ToList().ConvertAll(x =>
				new IPEndPoint(Dns.GetHostAddresses(x)[0], RedisNativeClient.DefaultPort));
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public T GetById<T>(object id) where T : class, new()
		{
			throw new NotImplementedException();
		}

		public IList<T> GetByIds<T>(ICollection ids) where T : class, new()
		{
			throw new NotImplementedException();
		}

		public T Store<T>(T entity) where T : class, new()
		{
			throw new NotImplementedException();
		}

		public void StoreAll<TEntity>(IEnumerable<TEntity> entities) where TEntity : class, new()
		{
			throw new NotImplementedException();
		}

		public void Delete<T>(T entity) where T : class, new()
		{
			throw new NotImplementedException();
		}

		public void DeleteById<T>(object id) where T : class, new()
		{
			throw new NotImplementedException();
		}

		public void DeleteByIds<T>(ICollection ids) where T : class, new()
		{
			throw new NotImplementedException();
		}

		public void DeleteAll<TEntity>() where TEntity : class, new()
		{
			throw new NotImplementedException();
		}

		public string Host
		{
			get { throw new NotImplementedException(); }
		}

		public int Port
		{
			get { throw new NotImplementedException(); }
		}

		public int RetryTimeout
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public int RetryCount
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public int SendTimeout
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public string Password
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public IRedisTypedClient<T> GetTypedClient<T>()
		{
			throw new NotImplementedException();
		}

		public IHasNamedList<string> Lists
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public IHasNamedCollection<string> Sets
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public Dictionary<string, string> Info
		{
			get { throw new NotImplementedException(); }
		}

		public int Db
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public int DbSize
		{
			get { throw new NotImplementedException(); }
		}

		public DateTime LastSave
		{
			get { throw new NotImplementedException(); }
		}

		public string[] AllKeys
		{
			get { throw new NotImplementedException(); }
		}

		public string this[string key]
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public void SetString(string key, string value)
		{
			throw new NotImplementedException();
		}

		public bool SetIfNotExists(string key, string value)
		{
			throw new NotImplementedException();
		}

		public string GetString(string key)
		{
			throw new NotImplementedException();
		}

		public string GetAndSetString(string key, string value)
		{
			throw new NotImplementedException();
		}

		public bool ContainsKey(string key)
		{
			throw new NotImplementedException();
		}

		public bool Remove(string key)
		{
			throw new NotImplementedException();
		}

		public bool Remove(params string[] args)
		{
			throw new NotImplementedException();
		}

		public int Increment(string key)
		{
			throw new NotImplementedException();
		}

		public int IncrementBy(string key, int count)
		{
			throw new NotImplementedException();
		}

		public int Decrement(string key)
		{
			throw new NotImplementedException();
		}

		public int DecrementBy(string key, int count)
		{
			throw new NotImplementedException();
		}

		public RedisKeyType GetKeyType(string key)
		{
			throw new NotImplementedException();
		}

		public string NewRandomKey()
		{
			throw new NotImplementedException();
		}

		public bool ExpireKeyIn(string key, TimeSpan expiresAt)
		{
			throw new NotImplementedException();
		}

		public bool ExpireKeyAt(string key, DateTime dateTime)
		{
			throw new NotImplementedException();
		}

		public TimeSpan GetTimeToLive(string key)
		{
			throw new NotImplementedException();
		}

		public string Save()
		{
			throw new NotImplementedException();
		}

		public void SaveAsync()
		{
			throw new NotImplementedException();
		}

		public void Shutdown()
		{
			throw new NotImplementedException();
		}

		public void FlushDb()
		{
			throw new NotImplementedException();
		}

		public void FlushAll()
		{
			throw new NotImplementedException();
		}

		public string[] GetKeys(string pattern)
		{
			throw new NotImplementedException();
		}

		public List<string> GetKeyValues(List<string> keys)
		{
			throw new NotImplementedException();
		}

		public List<T> GetKeyValues<T>(List<string> keys)
		{
			throw new NotImplementedException();
		}

		public List<string> GetRangeFromSortedSet(string setId, int startingFrom, int endingAt)
		{
			throw new NotImplementedException();
		}

		public HashSet<string> GetAllFromSet(string setId)
		{
			throw new NotImplementedException();
		}

		public void AddToSet(string setId, string value)
		{
			throw new NotImplementedException();
		}

		public void RemoveFromSet(string setId, string value)
		{
			throw new NotImplementedException();
		}

		public string PopFromSet(string setId)
		{
			throw new NotImplementedException();
		}

		public void MoveBetweenSets(string fromSetId, string toSetId, string value)
		{
			throw new NotImplementedException();
		}

		public int GetSetCount(string setId)
		{
			throw new NotImplementedException();
		}

		public bool SetContainsValue(string setId, string value)
		{
			throw new NotImplementedException();
		}

		public HashSet<string> GetIntersectFromSets(params string[] setIds)
		{
			throw new NotImplementedException();
		}

		public void StoreIntersectFromSets(string intoSetId, params string[] setIds)
		{
			throw new NotImplementedException();
		}

		public HashSet<string> GetUnionFromSets(params string[] setIds)
		{
			throw new NotImplementedException();
		}

		public void StoreUnionFromSets(string intoSetId, params string[] setIds)
		{
			throw new NotImplementedException();
		}

		public HashSet<string> GetDifferencesFromSet(string fromSetId, params string[] withSetIds)
		{
			throw new NotImplementedException();
		}

		public void StoreDifferencesFromSet(string intoSetId, string fromSetId, params string[] withSetIds)
		{
			throw new NotImplementedException();
		}

		public string GetRandomEntryFromSet(string setId)
		{
			throw new NotImplementedException();
		}

		public List<string> GetAllFromList(string listId)
		{
			throw new NotImplementedException();
		}

		public List<string> GetRangeFromList(string listId, int startingFrom, int endingAt)
		{
			throw new NotImplementedException();
		}

		public List<string> GetRangeFromSortedList(string listId, int startingFrom, int endingAt)
		{
			throw new NotImplementedException();
		}

		public void AddToList(string listId, string value)
		{
			throw new NotImplementedException();
		}

		public void PrependToList(string listId, string value)
		{
			throw new NotImplementedException();
		}

		public void RemoveAllFromList(string listId)
		{
			throw new NotImplementedException();
		}

		public void TrimList(string listId, int keepStartingFrom, int keepEndingAt)
		{
			throw new NotImplementedException();
		}

		public int RemoveValueFromList(string listId, string value)
		{
			throw new NotImplementedException();
		}

		public int RemoveValueFromList(string listId, string value, int noOfMatches)
		{
			throw new NotImplementedException();
		}

		public int GetListCount(string setId)
		{
			throw new NotImplementedException();
		}

		public string GetItemFromList(string listId, int listIndex)
		{
			throw new NotImplementedException();
		}

		public void SetItemInList(string listId, int listIndex, string value)
		{
			throw new NotImplementedException();
		}

		public string DequeueFromList(string listId)
		{
			throw new NotImplementedException();
		}

		public string PopFromList(string listId)
		{
			throw new NotImplementedException();
		}

		public void PopAndPushBetweenLists(string fromListId, string toListId)
		{
			throw new NotImplementedException();
		}
	}
}