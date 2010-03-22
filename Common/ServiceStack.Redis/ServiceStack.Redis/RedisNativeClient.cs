//
// redis-sharp.cs: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
// Copyright 2010 Novell, Inc.
//
// Licensed under the same terms of reddis: new BSD license.
//
//#define DEBUG

using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
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
	public class RedisNativeClient
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


		#region Protocol helper methods
		private void Connect()
		{
			socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) {
				SendTimeout = SendTimeout
			};
			try
			{
				socket.Connect(Host, Port);

				if (!socket.Connected)
				{
					socket.Close();
					socket = null;
					return;
				}
				bstream = new BufferedStream(new NetworkStream(socket), 16 * 1024);

				if (Password != null)
					SendExpectSuccess("AUTH {0}\r\n", Password);

				db = 0;
				var ipEndpoint = socket.LocalEndPoint as IPEndPoint;
				clientPort = ipEndpoint != null ? ipEndpoint.Port : -1;
				lastCommand = null;
				lastSocketException = null;
				lastConnectedAtTimestamp = Stopwatch.GetTimestamp();
			}
			catch (SocketException ex)
			{
				HadExceptions = true;
				var throwEx = new InvalidOperationException("could not connect to redis Instance at " + Host + ":" + Port, ex);
				log.Error(throwEx.Message, ex);
				throw throwEx;
			}
		}

		protected string ReadLine()
		{
			var sb = new StringBuilder();

			int c;
			while ((c = bstream.ReadByte()) != -1)
			{
				if (c == '\r')
					continue;
				if (c == '\n')
					break;
				sb.Append((char)c);
			}
			return sb.ToString();
		}

		private bool AssertConnectedSocket()
		{
			if (lastConnectedAtTimestamp > 0)
			{
				var now = Stopwatch.GetTimestamp();
				var elapsedSecs = (now - lastConnectedAtTimestamp) / Stopwatch.Frequency;

				if (elapsedSecs > IdleTimeOutSecs && !socket.IsConnected())
				{
					return Reconnect();
				}
				lastConnectedAtTimestamp = now;
			}

			if (socket == null)
				Connect();

			var isConnected = socket != null;
			
			return isConnected;
		}

		private bool Reconnect()
		{
			var previousDb = db;
	
			SafeConnectionClose();
			Connect(); //sets db to 0

			if (previousDb != DefaultDb)
			{
				this.Db = previousDb;
			}

			return socket != null;
		}

		private bool HandleSocketException(SocketException ex)
		{
			HadExceptions = true;
			log.Error("SocketException: ", ex);

			lastSocketException = ex;

			// timeout?
			socket.Close();
			socket = null;

			return false;
		}

		private RedisResponseException CreateResponseError(string error)
		{
			HadExceptions = true;
			var throwEx = new RedisResponseException(
				string.Format("{0}, sPort: {1}, LastCommand: {2}",
					error, clientPort, lastCommand));
			log.Error(throwEx.Message);
			throw throwEx;
		}

		private Exception CreateConnectionError()
		{
			HadExceptions = true;
			var throwEx = new Exception(
				string.Format("Unable to Connect: sPort: {0}", 
					clientPort), lastSocketException);
			log.Error(throwEx.Message);
			throw throwEx;
		}

		private static string SafeKey(string key)
		{
			return key == null ? null : key.Replace(' ', '_')
				.Replace('\t','_').Replace('\n','_');
		}

		private static string SafeKeys(params string[] keys)
		{
			var sb = new StringBuilder();
			foreach (var key in keys)
			{
				if (sb.Length > 0)
					sb.Append(" ");

				sb.Append(SafeKey(key));
			}

			return sb.ToString();
		}

		protected bool SendDataCommand(byte[] data, string cmd, params object[] args)
		{
			if (!AssertConnectedSocket()) return false;

			var s = args.Length > 0 ? String.Format(cmd, args) : cmd;
			this.lastCommand = s;

			byte [] r = Encoding.UTF8.GetBytes(s);
			try
			{
				Log("S: " + String.Format(cmd, args));
	
				socket.Send(r);

				if (data != null)
				{
					socket.Send(data);
					socket.Send(endData);
				}
			}
			catch (SocketException ex)
			{
				return HandleSocketException(ex);
			}
			return true;
		}

		protected bool SendCommand(string cmd, params object[] args)
		{
			if (!AssertConnectedSocket()) return false;

			var s = args != null && args.Length > 0 ? String.Format(cmd, args) : cmd;
			this.lastCommand = s;

			byte [] r = Encoding.UTF8.GetBytes(s);
			try
			{
				Log("S: " + String.Format(cmd, args));
				socket.Send(r);
			}
			catch (SocketException ex)
			{
				return HandleSocketException(ex);
			}
			return true;
		}

		protected string SendExpectString(string cmd, params object[] args)
		{
			if (!SendCommand(cmd, args))
				throw CreateConnectionError();

			var c = SafeReadByte();
			if (c == -1)
				throw CreateResponseError("No more data");

			var s = ReadLine();
			
			Log("R: " + s);
			
			if (c == '-')
				throw CreateResponseError(s.StartsWith("ERR") ? s.Substring(4) : s);
			
			if (c == '+')
				return s;

			throw CreateResponseError("Unknown reply on integer request: " + c + s);
		}

		private int SafeReadByte()
		{
			return bstream.ReadByte();
		}

		protected string SendGetString(string cmd, params object[] args)
		{
			if (!SendCommand(cmd, args))
				throw CreateConnectionError();

			return ReadLine();
		}

		[Conditional("DEBUG")]
		protected void Log(string fmt, params object[] args)
		{
			log.DebugFormat("{0}", string.Format(fmt, args).Trim());
		}

		protected void ExpectSuccess()
		{
			int c = SafeReadByte();
			if (c == -1)
				throw CreateResponseError("No more data");

			var s = ReadLine();
	
			Log((char)c + s);

			if (c == '-')
				throw CreateResponseError(s.StartsWith("ERR") ? s.Substring(4) : s);
		}

		protected void SendExpectSuccess(string cmd, params object[] args)
		{
			if (!SendCommand(cmd, args))
				throw CreateConnectionError();

			ExpectSuccess();
		}

		protected int SendExpectInt(string cmd, params object[] args)
		{
			if (!SendCommand(cmd, args))
				throw CreateConnectionError();

			return ReadInt();
		}

		protected double SendExpectDouble(string cmd, params object[] args)
		{
			if (!SendCommand(cmd, args))
				throw CreateConnectionError();

			return ReadDouble();
		}

		protected byte[] SendExpectData(byte[] data, string cmd, params object[] args)
		{
			if (!SendDataCommand(data, cmd, args))
				throw CreateConnectionError();

			return ReadData();
		}

		private int ReadInt()
		{
			int c = SafeReadByte();
			if (c == -1)
				throw CreateResponseError("No more data");

			var s = ReadLine();

			Log("R: " + s);

			if (c == '-')
				throw CreateResponseError(s.StartsWith("ERR") ? s.Substring(4) : s);

			if (c == ':')
			{
				int i;
				if (int.TryParse(s, out i))
					return i;
			}
			throw CreateResponseError("Unknown reply on double integer response: " + c + s);
		}

		private double ReadDouble()
		{
			int c = SafeReadByte();
			if (c == -1)
				throw CreateResponseError("No more data");

			var s = ReadLine();

			Log("R: " + s);

			if (c == '-')
				throw CreateResponseError(s.StartsWith("ERR") ? s.Substring(4) : s);

			if (c == ':')
			{
				double i;
				if (double.TryParse(s, out i))
					return i;
			}
			throw CreateResponseError("Unknown reply on expected double response: " + c + s);
		}

		private byte[] ReadData()
		{
			string r = ReadLine();
			Log("R: {0}", r);
			if (r.Length == 0)
				throw CreateResponseError("Zero length respose");

			char c = r[0];
			if (c == '-')
				throw CreateResponseError(r.StartsWith("-ERR") ? r.Substring(5) : r.Substring(1));

			if (c == '$')
			{
				if (r == "$-1")
					return null;
				int count;

				if (Int32.TryParse(r.Substring(1), out count))
				{
					var retbuf = new byte[count];

					var offset = 0;
					while (count > 0)
					{
						var readCount = bstream.Read(retbuf, offset, count);
						if (readCount <= 0)
							throw CreateResponseError("Unexpected end of Stream");

						offset += readCount;
						count -= readCount;
					}
					
					if (bstream.ReadByte() != '\r' || bstream.ReadByte() != '\n')
						throw CreateResponseError("Invalid termination");

					return retbuf;
				}
				throw CreateResponseError("Invalid length");
			}
			throw CreateResponseError("Unexpected reply: " + r);
		}

		private byte[][] ReadMultiData()
		{
			int c = SafeReadByte();
			if (c == -1)
				throw CreateResponseError("No more data");

			var s = ReadLine();
			Log("R: " + s);
			if (c == '-')
				throw CreateResponseError(s.StartsWith("ERR") ? s.Substring(4) : s);
			if (c == '*')
			{
				int count;
				if (int.TryParse(s, out count))
				{
					if (count == -1)
					{
						//redis is in an invalid state
						return new byte[0][];
					}

					var result = new byte[count][];

					for (int i = 0; i < count; i++)
						result[i] = ReadData();

					return result;
				}
			}
			throw CreateResponseError("Unknown reply on multi-request: " + c + s);
		}
		#endregion


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
				SendExpectSuccess("SELECT {0}\r\n", db);
			}
		}

		public int DbSize
		{
			get
			{
				return SendExpectInt("DBSIZE\r\n");
			}
		}

		public DateTime LastSave
		{
			get
			{
				int t = SendExpectInt("LASTSAVE\r\n");
				return DateTimeExtensions.FromUnixTime(t);
			}
		}

		public Dictionary<string, string> Info
		{
			get
			{
				byte [] r = SendExpectData(null, "INFO\r\n");
				var dict = new Dictionary<string, string>();

				foreach (var line in Encoding.UTF8.GetString(r)
					.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
				{
					int p = line.IndexOf(':');
					if (p == -1)
						continue;
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

			return SendExpectString("TYPE {0}\r\n", SafeKey(key));
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

			if (!SendDataCommand(value, "SET {0} {1}\r\n", SafeKey(key), value.Length))
				throw CreateConnectionError();
			ExpectSuccess();
		}

		public int SetNX(string key, byte[] value)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			value = value ?? new byte[0];

			if (value.Length > OneGb)
				throw new ArgumentException("value exceeds 1G", "value");

			if (!SendDataCommand(value, "SETNX {0} {1}\r\n", SafeKey(key), value.Length))
				throw CreateConnectionError();
			return ReadInt();
		}

		public byte[] Get(string key)
		{
			return GetBytes(key);
		}

		public byte[] GetBytes(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			return SendExpectData(null, "GET " + SafeKey(key) + "\r\n");
		}

		public byte[] GetSet(string key, byte[] value)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			value = value ?? new byte[0];

			if (value.Length > OneGb)
				throw new ArgumentException("value exceeds 1G", "value");

			if (!SendDataCommand(value, "GETSET {0} {1}\r\n", SafeKey(key), value.Length))
				throw CreateConnectionError();

			return ReadData();
		}

		public int Exists(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			return SendExpectInt("EXISTS " + SafeKey(key) + "\r\n");
		}

		public int Del(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			return SendExpectInt("DEL " + SafeKey(key) + "\r\n", key);
		}

		public int Del(params string[] keys)
		{
			if (keys == null)
				throw new ArgumentNullException("keys");

			return SendExpectInt("DEL " + SafeKeys(keys) + "\r\n");
		}

		public int Incr(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			return SendExpectInt("INCR " + SafeKey(key) + "\r\n");
		}

		public int IncrBy(string key, int count)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			return SendExpectInt("INCRBY {0} {1}\r\n", SafeKey(key), count);
		}

		public int Decr(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			return SendExpectInt("DECR " + SafeKey(key) + "\r\n");
		}

		public int DecrBy(string key, int count)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			return SendExpectInt("DECRBY {0} {1}\r\n", SafeKey(key), count);
		}

		public string RandomKey()
		{
			return SendExpectString("RANDOMKEY\r\n");
		}

		public bool Rename(string oldKeyname, string newKeyname)
		{
			if (oldKeyname == null)
				throw new ArgumentNullException("oldKeyname");
			if (newKeyname == null)
				throw new ArgumentNullException("newKeyname");
			return SendGetString("RENAME {0} {1}\r\n", SafeKey(oldKeyname), SafeKey(newKeyname))[0] == '+';
		}

		public int Expire(string key, int seconds)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			return SendExpectInt("EXPIRE {0} {1}\r\n", SafeKey(key), seconds);
		}

		public int ExpireAt(string key, long unixTime)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			return SendExpectInt("EXPIREAT {0} {1}\r\n", SafeKey(key), unixTime);
		}

		public int Ttl(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			return SendExpectInt("TTL {0}\r\n", SafeKey(key));
		}

		public string Save()
		{
			return SendGetString("SAVE\r\n");
		}

		public void SaveAsync()
		{
			BgSave();
		}

		public void BgSave()
		{
			SendGetString("BGSAVE\r\n");
		}

		public void Shutdown()
		{
			SendGetString("SHUTDOWN\r\n");
		}

		public void Quit()
		{
			SendCommand("QUIT\r\n");
		}

		public void FlushDb()
		{
			SendExpectSuccess("FLUSHDB\r\n");
		}

		public void FlushAll()
		{
			SendExpectSuccess("FLUSHALL\r\n");
		}

		public byte[] Keys(string pattern)
		{
			if (pattern == null)
				throw new ArgumentNullException("pattern");

			return SendExpectData(null, "KEYS {0}\r\n", pattern);
		}

		public byte[][] MGet(params string[] keys)
		{
			if (keys == null)
				throw new ArgumentNullException("keys");
			if (keys.Length == 0)
				throw new ArgumentException("keys");

			if (!SendDataCommand(null, "MGET {0}\r\n", SafeKeys(keys)))
				throw CreateConnectionError();
			return ReadMultiData();
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
		
		SendDataCommand (ms.ToArray (), "MSET\r\n");
		ExpectSuccess ();
	}
#endif

		internal void Multi()
		{
			if (!SendDataCommand(null, "MULTI\r\n"))
				throw CreateConnectionError();
			ReadData();
		}

		internal void Exec()
		{
			if (!SendDataCommand(null, "EXEC\r\n"))
				throw CreateConnectionError();
			ReadData();
		}

		internal void Discard()
		{
			if (!SendDataCommand(null, "DISCARD\r\n"))
				throw CreateConnectionError();
			ReadData();
		}

		#endregion


		#region Set Operations

		public byte[][] SMembers(string setId)
		{
			if (!SendDataCommand(null, "SMEMBERS {0}\r\n", setId))
				throw CreateConnectionError();
			return ReadMultiData();
		}

		public void SAdd(string setId, byte[] value)
		{
			if (setId == null)
				throw new ArgumentNullException("setId");
			if (value == null)
				throw new ArgumentNullException("value");

			if (!SendDataCommand(value, "SADD {0} {1}\r\n", SafeKey(setId), value.Length))
				throw CreateConnectionError();
			ExpectSuccess();
		}

		public void SRem(string setId, byte[] value)
		{
			if (setId == null)
				throw new ArgumentNullException("setId");
			if (!SendDataCommand(value, "SREM {0} {1}\r\n", SafeKey(setId), value.Length))
				throw CreateConnectionError();

			ExpectSuccess();
		}

		public byte[] SPop(string setId)
		{
			if (!SendDataCommand(null, "SPOP {0}\r\n", SafeKey(setId)))
				throw CreateConnectionError();
			return ReadData();
		}

		public void SMove(string fromSetId, string toSetId, byte[] value)
		{
			if (fromSetId == null)
				throw new ArgumentNullException("fromSetId");
			if (toSetId == null)
				throw new ArgumentNullException("toSetId");

			if (!SendDataCommand(value, "SMOVE {0} {1} {2}\r\n", SafeKey(fromSetId), SafeKey(toSetId), value.Length))
				throw CreateConnectionError();

			ExpectSuccess();
		}

		public int SCard(string setId)
		{
			if (setId == null)
				throw new ArgumentNullException("setId");

			return SendExpectInt("SCARD {0}\r\n", SafeKey(setId));
		}

		public int SIsMember(string setId, byte[] value)
		{
			if (setId == null)
				throw new ArgumentNullException("setId");
			if (!SendDataCommand(value, "SISMEMBER {0} {1}\r\n", SafeKey(setId), value.Length))
				throw CreateConnectionError();

			return ReadInt();
		}

		public byte[][] SInter(params string[] setIds)
		{
			

			if (!SendDataCommand(null, "SINTER {0}\r\n", SafeKeys(setIds)))
				throw CreateConnectionError();

			return ReadMultiData();
		}

		public void SInterStore(string intoSetId, params string[] setIds)
		{
			if (!SendDataCommand(null, "SINTERSTORE {0} {1}\r\n", SafeKey(intoSetId), SafeKeys(setIds)))
				throw CreateConnectionError();

			ExpectSuccess();
		}

		public byte[][] SUnion(params string[] setIds)
		{
			if (!SendDataCommand(null, "SUNION {0}\r\n", SafeKeys(setIds)))
				throw CreateConnectionError();

			return ReadMultiData();
		}

		public void SUnionStore(string intoSetId, params string[] setIds)
		{
			if (!SendDataCommand(null, "SUNIONSTORE {0} {1}\r\n", SafeKey(intoSetId), SafeKeys(setIds)))
				throw CreateConnectionError();

			ExpectSuccess();
		}

		public byte[][] SDiff(string fromSetId, params string[] withSetIds)
		{
			if (!SendDataCommand(null, "SDIFF {0} {1}\r\n", SafeKey(fromSetId), SafeKeys(withSetIds)))
				throw CreateConnectionError();

			return ReadMultiData();
		}

		public void SDiffStore(string intoSetId, string fromSetId, params string[] withSetIds)
		{
			if (!SendDataCommand(null, "SDIFFSTORE {0} {1} {2}\r\n", SafeKey(intoSetId), SafeKey(fromSetId), SafeKeys(withSetIds)))
				throw CreateConnectionError();

			ExpectSuccess();
		}

		public byte[] SRandMember(string setId)
		{
			if (!SendDataCommand(null, "SRANDMEMBER {0}\r\n", SafeKey(setId)))
				throw CreateConnectionError();
			return ReadData();
		}
		#endregion


		#region List Operations

		public byte[][] LRange(string listId, int startingFrom, int endingAt)
		{
			if (!SendDataCommand(null, "LRANGE {0} {1} {2}\r\n", SafeKey(listId), startingFrom, endingAt))
				throw CreateConnectionError();

			return ReadMultiData();
		}

		public byte[][] Sort(string listOrSetId, int startingFrom, int endingAt, bool sortAlpha, bool sortDesc)
		{
			var sortAlphaOption = sortAlpha ? " ALPHA" : "";
			var sortDescOption = sortDesc ? " DESC" : "";

			if (!SendDataCommand(null, "SORT {0} LIMIT {1} {2}{3}{4}\r\n", 
				SafeKey(listOrSetId), startingFrom, endingAt, sortAlphaOption, sortDescOption))
				throw CreateConnectionError();

			return ReadMultiData();
		}

		public void RPush(string listId, byte[] value)
		{
			if (listId == null)
				throw new ArgumentNullException("listId");
			if (value == null)
				throw new ArgumentNullException("value");

			if (!SendDataCommand(value, "RPUSH {0} {1}\r\n", SafeKey(listId), value.Length))
				throw CreateConnectionError();
			ExpectSuccess();
		}

		public void LPush(string listId, byte[] value)
		{
			if (listId == null)
				throw new ArgumentNullException("listId");
			if (value == null)
				throw new ArgumentNullException("value");

			if (!SendDataCommand(value, "LPUSH {0} {1}\r\n", SafeKey(listId), value.Length))
				throw CreateConnectionError();
			ExpectSuccess();
		}

		public void LTrim(string listId, int keepStartingFrom, int keepEndingAt)
		{
			if (!SendCommand("LTRIM {0} {1} {2}\r\n", SafeKey(listId), keepStartingFrom, keepEndingAt))
				throw CreateConnectionError();

			ExpectSuccess();
		}

		public int LRem(string listId, int removeNoOfMatches, byte[] value)
		{
			if (!SendDataCommand(value, "LREM {0} {1} {2}\r\n", SafeKey(listId), removeNoOfMatches, value.Length))
				throw CreateConnectionError();

			return ReadInt();
		}

		public int LLen(string listId)
		{
			if (listId == null)
				throw new ArgumentNullException("listId");

			return SendExpectInt("LLEN {0}\r\n", SafeKey(listId));
		}

		public byte[] LIndex(string listId, int listIndex)
		{
			if (!SendCommand("LINDEX {0} {1}\r\n", SafeKey(listId), listIndex))
				throw CreateConnectionError();
			return ReadData();
		}

		public void LSet(string listId, int listIndex, byte[] value)
		{
			if (!SendDataCommand(value, "LSET {0} {1} {2}\r\n", SafeKey(listId), listIndex, value.Length))
				throw CreateConnectionError();

			ExpectSuccess();
		}

		public byte[] LPop(string listId)
		{
			if (!SendCommand("LPOP {0}\r\n", SafeKey(listId)))
				throw CreateConnectionError();
			return ReadData();
		}

		public byte[] RPop(string listId)
		{
			if (!SendCommand("RPOP {0}\r\n", SafeKey(listId)))
				throw CreateConnectionError();
			return ReadData();
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
				if (!SendDataCommand(value, "RPOPLPUSH {0} {1}\r\n", SafeKey(fromListId), value.Length))
					throw CreateConnectionError();

				return ReadData();
			}

			if (!SendCommand("RPOPLPUSH {0} {1}\r\n", SafeKey(fromListId), SafeKey(toListId)))
				throw CreateConnectionError();

			return ReadData();
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

			if (!SendDataCommand(value, "ZADD {0} {1} {2}\r\n", 
				SafeKey(setId), score, value.Length))
				throw CreateConnectionError();

			return ReadInt();
		}

		public int ZRem(string setId, byte[] value)
		{
			AssertSetIdAndValue(setId, value);

			if (!SendDataCommand(value, "ZREM {0} {1}\r\n", SafeKey(setId), value.Length))
				throw CreateConnectionError();

			return ReadInt();
		}

		public double ZIncrBy(string setId, double incrBy, byte[] value)
		{
			AssertSetIdAndValue(setId, value);

			if (!SendDataCommand(value, "ZADD {0} {1} {2}\r\n",
				SafeKey(setId), incrBy, value.Length))
				throw CreateConnectionError();

			return ReadDouble();
		}

		public int ZRank(string setId, byte[] value)
		{
			AssertSetIdAndValue(setId, value);

			if (!SendDataCommand(value, "ZRANK {0} {1}\r\n", SafeKey(setId), value.Length))
				throw CreateConnectionError();

			return ReadInt();
		}

		public int ZRevRank(string setId, byte[] value)
		{
			AssertSetIdAndValue(setId, value);

			if (!SendDataCommand(value, "ZRANK {0} {1}\r\n", SafeKey(setId), value.Length))
				throw CreateConnectionError();

			return ReadInt();
		}

		private byte[][] GetRange(string commandText, string setId, int min, int max, bool withScores)
		{
			if (string.IsNullOrEmpty(setId))
				throw new ArgumentNullException("setId");

			var withScoresString = withScores ? " WITHSCORES" : "";
			if (!SendDataCommand(null, "{0} {1} {2} {3}{4}\r\n",
				commandText, SafeKey(setId), min, max, withScoresString))
				throw CreateConnectionError();

			return ReadMultiData();
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

			if (!SendDataCommand(null, "{0} {1} {2} {3}{4}{5}\r\n",
								 commandText, SafeKey(setId), min, max, 
								 limitString, withScoresString))
				throw CreateConnectionError();

			return ReadMultiData();
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

			if (!SendDataCommand(null, "ZREMRANGEBYRANK {0} {1} {2}\r\n", 
				SafeKey(setId), min, max))
				throw CreateConnectionError();

			return ReadInt();
		}

		public int ZCard(string setId)
		{
			if (setId == null)
				throw new ArgumentNullException("setId");

			return SendExpectInt("ZCARD {0}\r\n", SafeKey(setId));
		}

		public double ZScore(string setId, byte[] value)
		{
			if (setId == null)
				throw new ArgumentNullException("setId");

			return SendExpectDouble("ZSCORE {0}\r\n", SafeKey(setId));
		}

		public int ZUnion(string intoSetId, params string[] setIds)
		{
			if (!SendDataCommand(null, "ZUNION {0} {1}\r\n", 
				SafeKey(intoSetId), SafeKeys(setIds)))
				throw CreateConnectionError();

			return ReadInt();
		}

		public int ZInter(string intoSetId, params string[] setIds)
		{
			if (!SendDataCommand(null, "ZINTER {0} {1}\r\n",
				SafeKey(intoSetId), SafeKeys(setIds)))
				throw CreateConnectionError();

			return ReadInt();
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

			if (!SendDataCommand(value, "HSET {0} {1} {2}\r\n",
				SafeKey(hashId), SafeKeys(key), value.Length))
				throw CreateConnectionError();

			return ReadInt();
		}

		public byte[] HGet(string hashId, string key)
		{
			AssertHashIdAndKey(hashId, key);

			if (!SendDataCommand(null, "HGET {0} {1}\r\n",
				SafeKey(hashId), SafeKeys(key)))
				throw CreateConnectionError();

			return ReadData();
		}

		public int HDel(string hashId, string key)
		{
			AssertHashIdAndKey(hashId, key);

			if (!SendDataCommand(null, "HDEL {0} {1}\r\n",
				SafeKey(hashId), SafeKeys(key)))
				throw CreateConnectionError();

			return ReadInt();
		}

		public bool HExists(string hashId, string key)
		{
			AssertHashIdAndKey(hashId, key);

			if (!SendDataCommand(null, "HDEL {0} {1}\r\n",
				SafeKey(hashId), SafeKeys(key)))
				throw CreateConnectionError();

			return ReadInt() == Success;
		}

		public int HLen(string hashId)
		{
			if (string.IsNullOrEmpty(hashId))
				throw new ArgumentNullException("hashId");

			return SendExpectInt("HLEN {0}\r\n", SafeKey(hashId));
		}

		public byte[][] HKeys(string hashId)
		{
			if (hashId == null)
				throw new ArgumentNullException("hashId");

			if (!SendDataCommand(null, "HKEYS {0}\r\n", SafeKeys(hashId)))
				throw CreateConnectionError();

			return ReadMultiData();
		}

		public byte[][] HValues(string hashId)
		{
			if (hashId == null)
				throw new ArgumentNullException("hashId");

			if (!SendDataCommand(null, "HVALUES {0}\r\n", SafeKeys(hashId)))
				throw CreateConnectionError();

			return ReadMultiData();
		}

		public byte[][] HGetAll(string hashId)
		{
			if (hashId == null)
				throw new ArgumentNullException("hashId");

			if (!SendDataCommand(null, "HGETALL {0}\r\n", SafeKeys(hashId)))
				throw CreateConnectionError();

			return ReadMultiData();
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
