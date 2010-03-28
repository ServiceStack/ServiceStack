//
// redis-sharp.cs: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
// Copyright 2010 Novell, Inc.
//
// Licensed under the same terms of Redis: new BSD license.
//
//#define DEBUG

using System;
using System.IO;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using ServiceStack.Common.Extensions;
using ServiceStack.Logging;

namespace ServiceStack.Redis
{
	/// <summary>
	/// This class contains all the common operations for the RedisClient.
	/// The client contains a 1:1 mapping of c# methods to redis operations of the same name.
	/// 
	/// Not threadsafe use a pooled manager
	/// </summary>
	public partial class RedisNativeClient
		: IRedisNativeClient
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(RedisNativeClient));

		public const int DefaultDb = 0;
		public const int DefaultPort = 6379;
		public const string DefaultHost = "localhost";

		internal const int Success = 1;
		internal const int OneGb = 1073741824;
		private readonly byte [] endData = new[] { (byte)'\r', (byte)'\n' };

		private int clientPort;
		private string lastCommand;
		private SocketException lastSocketException;
		public bool HadExceptions { get; protected set; }

		protected Socket socket;
		protected BufferedStream bstream;

		/// <summary>
		/// Used to manage connection pooling
		/// </summary>
		internal bool Active { get; set; }
		internal PooledRedisClientManager ClientManager { get; set; }
		
		internal int IdleTimeOutSecs = 240; //default on redis is 300
		internal long lastConnectedAtTimestamp;

		public string Host { get; private set; }
		public int Port { get; private set; }
		public int RetryTimeout { get; set; }
		public int RetryCount { get; set; }
		public int SendTimeout { get; set; }
		public string Password { get; set; }

		internal RedisTransaction CurrentTransaction { get; set; }

		public RedisNativeClient(string host)
			: this(host, DefaultPort)
		{
		}

		public RedisNativeClient(string host, int port)
		{
			if (host == null)
				throw new ArgumentNullException("host");

			Host = host;
			Port = port;
			SendTimeout = -1;
		}

		public RedisNativeClient()
			: this(DefaultHost, DefaultPort)
		{
		}



		#region Common Operations

		int db;
		public int Db
		{
			get
			{
				return db;
			}

			set
			{
				db = value;
				SendExpectSuccess("SELECT {0}", db);
			}
		}

		public int DbSize
		{
			get
			{
				return SendExpectInt("DBSIZE");
			}
		}

		public DateTime LastSave
		{
			get
			{
				int t = SendExpectInt("LASTSAVE");
				return DateTimeExtensions.FromUnixTime(t);
			}
		}

		public Dictionary<string, string> Info
		{
			get
			{
				byte [] r = SendExpectData("INFO");
				var dict = new Dictionary<string, string>();

				foreach (var line in Encoding.UTF8.GetString(r)
					.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
				{
					var p = line.IndexOf(':');
					if (p == -1) continue;

					dict.Add(line.Substring(0, p), line.Substring(p + 1));
				}
				return dict;
			}
		}

		public string ServerVersion
		{
			get
			{
				string version;
				this.Info.TryGetValue("redis_version", out version);
				return version;
			}
		}

		public string Type(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			return SendExpectString("TYPE {0}", SafeKey(key));
		}

		public RedisKeyType GetKeyType(string key)
		{
			switch (Type(key))
			{
				case "none":
					return RedisKeyType.None;
				case "string":
					return RedisKeyType.String;
				case "set":
					return RedisKeyType.Set;
				case "list":
					return RedisKeyType.List;
				case "sortedset":
					return RedisKeyType.SortedSet;
				case "hash":
					return RedisKeyType.Hash;
			}
			throw CreateResponseError("Invalid value");
		}

		public void Set(string key, byte[] value)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			value = value ?? new byte[0];

			if (value.Length > OneGb)
				throw new ArgumentException("value exceeds 1G", "value");

			SendDataExpectSuccess(value, "SET {0} {1}", SafeKey(key), value.Length);
		}

		public int SetNX(string key, byte[] value)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			value = value ?? new byte[0];

			if (value.Length > OneGb)
				throw new ArgumentException("value exceeds 1G", "value");

			return SendDataExpectInt(value, "SETNX {0} {1}", SafeKey(key), value.Length);
		}

		public byte[] Get(string key)
		{
			return GetBytes(key);
		}

		public byte[] GetBytes(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			return SendExpectData("GET " + SafeKey(key));
		}

		public byte[] GetSet(string key, byte[] value)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			value = value ?? new byte[0];

			if (value.Length > OneGb)
				throw new ArgumentException("value exceeds 1G", "value");

			return SendDataExpectData(value, "GETSET {0} {1}", SafeKey(key), value.Length);
		}

		public int Exists(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			return SendExpectInt("EXISTS " + SafeKey(key));
		}

		public int Del(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			return SendExpectInt("DEL " + SafeKey(key), key);
		}

		public int Del(params string[] keys)
		{
			if (keys == null)
				throw new ArgumentNullException("keys");

			return SendExpectInt("DEL " + SafeKeys(keys));
		}

		public int Incr(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			return SendExpectInt("INCR " + SafeKey(key));
		}

		public int IncrBy(string key, int count)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			return SendExpectInt("INCRBY {0} {1}", SafeKey(key), count);
		}

		public int Decr(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			return SendExpectInt("DECR " + SafeKey(key));
		}

		public int DecrBy(string key, int count)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			return SendExpectInt("DECRBY {0} {1}", SafeKey(key), count);
		}

		public string RandomKey()
		{
			return SendExpectString("RANDOMKEY");
		}

		public bool Rename(string oldKeyname, string newKeyname)
		{
			if (oldKeyname == null)
				throw new ArgumentNullException("oldKeyname");
			if (newKeyname == null)
				throw new ArgumentNullException("newKeyname");

			return SendGetString("RENAME {0} {1}", SafeKey(oldKeyname), SafeKey(newKeyname))[0] == '+';
		}

		public int Expire(string key, int seconds)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			return SendExpectInt("EXPIRE {0} {1}", SafeKey(key), seconds);
		}

		public int ExpireAt(string key, long unixTime)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			return SendExpectInt("EXPIREAT {0} {1}", SafeKey(key), unixTime);
		}

		public int Ttl(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			return SendExpectInt("TTL {0}", SafeKey(key));
		}

		public string Save()
		{
			return SendGetString("SAVE");
		}

		public void SaveAsync()
		{
			BgSave();
		}

		public void BgSave()
		{
			SendGetString("BGSAVE");
		}

		public void Shutdown()
		{
			SendGetString("SHUTDOWN");
		}

		public void Quit()
		{
			SendCommand("QUIT");
		}

		public void FlushDb()
		{
			SendExpectSuccess("FLUSHDB");
		}

		public void FlushAll()
		{
			SendExpectSuccess("FLUSHALL");
		}

		//Old behaviour pre 1.3.7
		public byte[] KeysV125(string pattern)
		{
			if (pattern == null)
				throw new ArgumentNullException("pattern");

			return SendExpectData("KEYS {0}", pattern);
		}

		public byte[][] Keys(string pattern)
		{
			if (pattern == null)
				throw new ArgumentNullException("pattern");

			return SendExpectMultiData("KEYS {0}", pattern);
		}

		public byte[][] MGet(params string[] keys)
		{
			if (keys == null)
				throw new ArgumentNullException("keys");
			if (keys.Length == 0)
				throw new ArgumentException("keys");

			return SendExpectMultiData("MGET {0}", SafeKeys(keys));
		}

#if false
	// Not well documented how bulk operations work
	public void Set (IDictionary<string,byte []> dict)
	{
		if (dict == null)
			throw new ArgumentNullException ("dict");

		var ms = new MemoryStream ();
		foreach (var key in dict.Keys){
			var val = dict [key];
			
			var s = "$" + val.Length.ToString () + "\r\n";
			var b = Encoding.UTF8.GetBytes (s);
			ms.Write (b, 0, b.Length);
			ms.Write (val, 0, val.Length);
		}
		
		SendDataCommand (ms.ToArray (), "MSET");
		ExpectSuccess ();
	}
#endif

		internal void Multi()
		{
			SendExpectOk("MULTI");
		}

		/// <summary>
		/// Requires custom result parsing
		/// </summary>
		/// <returns>Number of results</returns>
		internal int Exec()
		{
			if (!SendCommand("EXEC"))
				throw CreateConnectionError();

			return this.ReadMultiDataResultCount();
		}

		internal void Discard()
		{
			SendExpectSuccess("DISCARD");
		}

		#endregion


		#region Set Operations

		public byte[][] SMembers(string setId)
		{
			return SendExpectMultiData("SMEMBERS {0}", setId);
		}

		public void SAdd(string setId, byte[] value)
		{
			AssertSetIdAndValue(setId, value);

			SendDataExpectSuccess(value, "SADD {0} {1}", SafeKey(setId), value.Length);
		}

		public void SRem(string setId, byte[] value)
		{
			AssertSetIdAndValue(setId, value);

			SendDataExpectSuccess(value, "SREM {0} {1}", SafeKey(setId), value.Length);
		}

		public byte[] SPop(string setId)
		{
			if (setId == null)
				throw new ArgumentNullException("setId");

			return SendExpectData("SPOP {0}", SafeKey(setId));
		}

		public void SMove(string fromSetId, string toSetId, byte[] value)
		{
			if (fromSetId == null)
				throw new ArgumentNullException("fromSetId");
			if (toSetId == null)
				throw new ArgumentNullException("toSetId");

			SendDataExpectSuccess(value, "SMOVE {0} {1} {2}", SafeKey(fromSetId), SafeKey(toSetId), value.Length);
		}

		public int SCard(string setId)
		{
			if (setId == null)
				throw new ArgumentNullException("setId");

			return SendExpectInt("SCARD {0}", SafeKey(setId));
		}

		public int SIsMember(string setId, byte[] value)
		{
			if (setId == null)
				throw new ArgumentNullException("setId");

			return SendDataExpectInt(value, "SISMEMBER {0} {1}", SafeKey(setId), value.Length);
		}

		public byte[][] SInter(params string[] setIds)
		{
			return SendExpectMultiData("SINTER {0}", SafeKeys(setIds));
		}

		public void SInterStore(string intoSetId, params string[] setIds)
		{
			SendExpectSuccess("SINTERSTORE {0} {1}", SafeKey(intoSetId), SafeKeys(setIds));
		}

		public byte[][] SUnion(params string[] setIds)
		{
			return SendExpectMultiData("SUNION {0}", SafeKeys(setIds));
		}

		public void SUnionStore(string intoSetId, params string[] setIds)
		{
			SendExpectSuccess("SUNIONSTORE {0} {1}", SafeKey(intoSetId), SafeKeys(setIds));
		}

		public byte[][] SDiff(string fromSetId, params string[] withSetIds)
		{
			return SendExpectMultiData("SDIFF {0} {1}", SafeKey(fromSetId), SafeKeys(withSetIds));
		}

		public void SDiffStore(string intoSetId, string fromSetId, params string[] withSetIds)
		{
			SendExpectSuccess("SDIFFSTORE {0} {1} {2}", SafeKey(intoSetId), SafeKey(fromSetId), SafeKeys(withSetIds));
		}

		public byte[] SRandMember(string setId)
		{
			return SendExpectData("SRANDMEMBER {0}", SafeKey(setId));
		}

		#endregion


		#region List Operations

		public byte[][] LRange(string listId, int startingFrom, int endingAt)
		{
			return SendExpectMultiData("LRANGE {0} {1} {2}", SafeKey(listId), startingFrom, endingAt);
		}

		public byte[][] Sort(string listOrSetId, int startingFrom, int endingAt, bool sortAlpha, bool sortDesc)
		{
			var sortAlphaOption = sortAlpha ? " ALPHA" : "";
			var sortDescOption = sortDesc ? " DESC" : "";

			return SendExpectMultiData("SORT {0} LIMIT {1} {2}{3}{4}",
				SafeKey(listOrSetId), startingFrom, endingAt, sortAlphaOption, sortDescOption);
		}

		public void RPush(string listId, byte[] value)
		{
			AssertListIdAndValue(listId, value);

			SendDataExpectSuccess(value, "RPUSH {0} {1}", SafeKey(listId), value.Length);
		}

		public void LPush(string listId, byte[] value)
		{
			AssertListIdAndValue(listId, value);

			SendDataExpectSuccess(value, "LPUSH {0} {1}", SafeKey(listId), value.Length);
		}

		public void LTrim(string listId, int keepStartingFrom, int keepEndingAt)
		{
			if (listId == null)
				throw new ArgumentNullException("listId");

			SendExpectSuccess("LTRIM {0} {1} {2}", SafeKey(listId), keepStartingFrom, keepEndingAt);
		}

		public int LRem(string listId, int removeNoOfMatches, byte[] value)
		{
			if (listId == null)
				throw new ArgumentNullException("listId");

			return SendDataExpectInt(value, "LREM {0} {1} {2}", SafeKey(listId), removeNoOfMatches, value.Length);
		}

		public int LLen(string listId)
		{
			if (listId == null)
				throw new ArgumentNullException("listId");

			return SendExpectInt("LLEN {0}", SafeKey(listId));
		}

		public byte[] LIndex(string listId, int listIndex)
		{
			if (listId == null)
				throw new ArgumentNullException("listId");

			return SendExpectData("LINDEX {0} {1}", SafeKey(listId), listIndex);
		}

		public void LSet(string listId, int listIndex, byte[] value)
		{
			if (listId == null)
				throw new ArgumentNullException("listId");

			SendDataExpectSuccess(value, "LSET {0} {1} {2}", SafeKey(listId), listIndex, value.Length);
		}

		public byte[] LPop(string listId)
		{
			if (listId == null)
				throw new ArgumentNullException("listId");

			return SendExpectData("LPOP {0}", SafeKey(listId));
		}

		public byte[] RPop(string listId)
		{
			if (listId == null)
				throw new ArgumentNullException("listId");

			return SendExpectData("RPOP {0}", SafeKey(listId));
		}

		public byte[] RPopLPush(string fromListId, string toListId)
		{
			if (fromListId == null)
				throw new ArgumentNullException("fromListId");
			if (toListId == null)
				throw new ArgumentNullException("toListId");

			var hasBug = this.ServerVersion.CompareTo("1.2.0") <= 0;
			if (hasBug)
			{
				var value = Encoding.UTF8.GetBytes(toListId);
				return SendDataExpectData(value, "RPOPLPUSH {0} {1}", SafeKey(fromListId), value.Length);
			}

			return SendExpectData("RPOPLPUSH {0} {1}", SafeKey(fromListId), SafeKey(toListId));
		}

		#endregion


		#region Sorted Set Operations

		private static void AssertSetIdAndValue(string setId, byte[] value)
		{
			if (setId == null)
				throw new ArgumentNullException("setId");
			if (value == null)
				throw new ArgumentNullException("value");
		}

		public int ZAdd(string setId, double score, byte[] value)
		{
			AssertSetIdAndValue(setId, value);

			return SendDataExpectInt(value, "ZADD {0} {1} {2}", SafeKey(setId), score, value.Length);
		}

		public int ZRem(string setId, byte[] value)
		{
			AssertSetIdAndValue(setId, value);

			return SendDataExpectInt(value, "ZREM {0} {1}", SafeKey(setId), value.Length);
		}

		public double ZIncrBy(string setId, double incrBy, byte[] value)
		{
			AssertSetIdAndValue(setId, value);

			return SendDataExpectDataAsDouble(value, "ZADD {0} {1} {2}", SafeKey(setId), incrBy, value.Length);
		}

		public int ZRank(string setId, byte[] value)
		{
			AssertSetIdAndValue(setId, value);

			return SendDataExpectInt(value, "ZRANK {0} {1}", SafeKey(setId), value.Length);
		}

		public int ZRevRank(string setId, byte[] value)
		{
			AssertSetIdAndValue(setId, value);

			return SendDataExpectInt(value, "ZREVRANK {0} {1}", SafeKey(setId), value.Length);
		}

		private byte[][] GetRange(string commandText, string setId, int min, int max, bool withScores)
		{
			if (string.IsNullOrEmpty(setId))
				throw new ArgumentNullException("setId");

			var withScoresString = withScores ? " WITHSCORES" : "";
			return SendExpectMultiData("{0} {1} {2} {3}{4}", commandText, SafeKey(setId), min, max, withScoresString);
		}

		public byte[][] ZRange(string setId, int min, int max)
		{
			return GetRange("ZRANGE", setId, min, max, false);
		}

		public byte[][] ZRangeWithScores(string setId, int min, int max)
		{
			return GetRange("ZRANGE", setId, min, max, true);
		}

		public byte[][] ZRevRange(string setId, int min, int max)
		{
			return GetRange("ZREVRANGE", setId, min, max, false);
		}

		public byte[][] ZRevRangeWithScores(string setId, int min, int max)
		{
			return GetRange("ZREVRANGE", setId, min, max, true);
		}

		private byte[][] GetRangeByScore(string commandText,
			string setId, double min, double max, int? skip, int? take, bool withScores)
		{
			if (setId == null)
				throw new ArgumentNullException("setId");

			var limitString = skip.HasValue || take.HasValue
				? " LIMIT " + skip.GetValueOrDefault(0) + " " + take.GetValueOrDefault(0)
				: "";

			var withScoresString = withScores ? " WITHSCORES" : "";
			return SendExpectMultiData("{0} {1} {2} {3}{4}{5}",
				commandText, SafeKey(setId), min, max, limitString, withScoresString);
		}

		public byte[][] ZRangeByScore(string setId, double min, double max, int? skip, int? take)
		{
			return GetRangeByScore("ZRANGEBYSCORE", setId, min, max, skip, take, false);
		}

		public byte[][] ZRangeByScoreWithScores(string setId, double min, double max, int? skip, int? take)
		{
			return GetRangeByScore("ZRANGEBYSCORE", setId, min, max, skip, take, true);
		}

		public byte[][] ZRevRangeByScore(string setId, double min, double max, int? skip, int? take)
		{
			return GetRangeByScore("ZREVRANGEBYSCORE", setId, min, max, skip, take, false);
		}

		public byte[][] ZRevRangeByScoreWithScores(string setId, double min, double max, int? skip, int? take)
		{
			return GetRangeByScore("ZREVRANGEBYSCORE", setId, min, max, skip, take, true);
		}

		public int ZRemRangeByRank(string setId, int min, int max)
		{
			if (setId == null)
				throw new ArgumentNullException("setId");

			return SendExpectInt("ZREMRANGEBYRANK {0} {1} {2}", SafeKey(setId), min, max);
		}

		public int ZRemRangeByScore(string setId, double fromScore, double toScore)
		{
			if (setId == null)
				throw new ArgumentNullException("setId");

			return SendExpectInt("ZREMRANGEBYSCORE {0} {1} {2}", SafeKey(setId), fromScore, toScore);
		}

		public int ZCard(string setId)
		{
			if (setId == null)
				throw new ArgumentNullException("setId");

			return SendExpectInt("ZCARD {0}", SafeKey(setId));
		}

		public double ZScore(string setId, byte[] value)
		{
			if (setId == null)
				throw new ArgumentNullException("setId");

			return SendDataExpectDouble(value, "ZSCORE {0} {1}", SafeKey(setId), value.Length);
		}

		public int ZUnion(string intoSetId, params string[] setIds)
		{
			return SendExpectInt("ZUNION {0} {1} {2}", SafeKey(intoSetId), setIds.Length, SafeKeys(setIds));
		}

		public int ZInter(string intoSetId, params string[] setIds)
		{
			return SendExpectInt("ZINTER {0} {1} {2}", SafeKey(intoSetId), setIds.Length, SafeKeys(setIds));
		}

		#endregion


		#region Hash Operations

		private static void AssertHashIdAndKey(string hashId, string key)
		{
			if (hashId == null)
				throw new ArgumentNullException("hashId");
			if (key == null)
				throw new ArgumentNullException("key");
		}

		public int HSet(string hashId, string key, byte[] value)
		{
			AssertHashIdAndKey(hashId, key);

			return SendDataExpectInt(value, "HSET {0} {1} {2}", SafeKey(hashId), SafeKeys(key), value.Length);
		}

		public byte[] HGet(string hashId, string key)
		{
			AssertHashIdAndKey(hashId, key);

			return SendExpectData("HGET {0} {1}", SafeKey(hashId), SafeKeys(key));
		}

		public int HDel(string hashId, string key)
		{
			AssertHashIdAndKey(hashId, key);

			var keyBytes = key.ToUtf8Bytes();
			return SendDataExpectInt(keyBytes, "HDEL {0} {1}", SafeKey(hashId), keyBytes.Length);
		}

		public bool HExists(string hashId, string key)
		{
			AssertHashIdAndKey(hashId, key);

			var keyBytes = key.ToUtf8Bytes();
			return SendDataExpectInt(keyBytes, "HEXISTS {0} {1}", SafeKey(hashId), keyBytes.Length) == Success;
		}

		public int HLen(string hashId)
		{
			if (string.IsNullOrEmpty(hashId))
				throw new ArgumentNullException("hashId");

			return SendExpectInt("HLEN {0}", SafeKey(hashId));
		}

		public byte[][] HKeys(string hashId)
		{
			if (hashId == null)
				throw new ArgumentNullException("hashId");

			return SendExpectMultiData("HKEYS {0}", SafeKeys(hashId));
		}

		public byte[][] HValues(string hashId)
		{
			if (hashId == null)
				throw new ArgumentNullException("hashId");

			return SendExpectMultiData("HVALUES {0}", SafeKeys(hashId));
		}

		public byte[][] HGetAll(string hashId)
		{
			if (hashId == null)
				throw new ArgumentNullException("hashId");

			return SendExpectMultiData("HGETALL {0}", SafeKeys(hashId));
		}

		#endregion


		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~RedisNativeClient()
		{
			Dispose(false);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				//dispose managed resources
			}

			if (ClientManager != null)
			{
				ClientManager.DisposeClient(this);
				return;
			}

			DisposeConnection();
		}

		internal void DisposeConnection()
		{
			if (socket == null) return;

			try
			{
				Quit();
			}
			catch (Exception ex)
			{
				log.Error("Error when trying to Quit()", ex);
			}
			finally
			{
				SafeConnectionClose();
			}
		}

		private void SafeConnectionClose()
		{
			try {
				// workaround for a .net bug: http://support.microsoft.com/kb/821625
				if (bstream != null)
					bstream.Close();
			} catch { }
			try {
				if (socket != null)
					socket.Close();
			}
			catch { }
			bstream = null;
			socket = null;
		}
	}
}
