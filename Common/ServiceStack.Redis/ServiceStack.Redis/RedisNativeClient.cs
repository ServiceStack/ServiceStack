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
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Redis
{
	/// <summary>
	/// This class contains all the common operations for the RedisClient.
	/// The client contains a 1:1 mapping of c# methods to redis operations of the same name.
	/// </summary>
	public class RedisNativeClient
		: IRedisNativeClient
	{
		internal const int Success = 1;
		internal const int OneGb = 1073741824;
		private readonly byte [] endData = new[] { (byte)'\r', (byte)'\n' };

		protected Socket socket;
		protected BufferedStream bstream;

		public string Host { get; private set; }
		public int Port { get; private set; }
		public int RetryTimeout { get; set; }
		public int RetryCount { get; set; }
		public int SendTimeout { get; set; }
		public string Password { get; set; }

		public RedisNativeClient(string host, int port)
		{
			if (host == null)
				throw new ArgumentNullException("host");

			Host = host;
			Port = port;
			SendTimeout = -1;
		}

		public RedisNativeClient()
			: this("localhost", 6379)
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
			}
			catch (SocketException ex)
			{
				throw new InvalidOperationException("could not connect to redis instance at " + Host + ":" + Port, ex);
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

		protected bool SendDataCommand(byte[] data, string cmd, params object[] args)
		{
			if (socket == null)
				Connect();
			if (socket == null)
				return false;

			var s = args.Length > 0 ? String.Format(cmd, args) : cmd;
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
			catch (SocketException)
			{
				// timeout;
				socket.Close();
				socket = null;

				return false;
			}
			return true;
		}

		protected bool SendCommand(string cmd, params object[] args)
		{
			if (socket == null)
				Connect();
			if (socket == null)
				return false;

			var s = args != null && args.Length > 0 ? String.Format(cmd, args) : cmd;
			byte [] r = Encoding.UTF8.GetBytes(s);
			try
			{
				Log("S: " + String.Format(cmd, args));
				socket.Send(r);
			}
			catch (SocketException)
			{
				// timeout;
				socket.Close();
				socket = null;

				return false;
			}
			return true;
		}

		protected string SendExpectString(string cmd, params object[] args)
		{
			if (!SendCommand(cmd, args))
				throw new Exception("Unable to connect");

			int c = bstream.ReadByte();
			if (c == -1)
				throw new RedisResponseException("No more data");

			var s = ReadLine();
			Log("R: " + s);
			if (c == '-')
				throw new RedisResponseException(s.StartsWith("ERR") ? s.Substring(4) : s);
			if (c == '+')
				return s;

			throw new RedisResponseException("Unknown reply on integer request: " + c + s);
		}

		//
		// This one does not throw errors
		//
		protected string SendGetString(string cmd, params object[] args)
		{
			if (!SendCommand(cmd, args))
				throw new Exception("Unable to connect");

			return ReadLine();
		}

		[Conditional("DEBUG")]
		protected void Log(string fmt, params object[] args)
		{
			Console.WriteLine("{0}", String.Format(fmt, args).Trim());
		}

		protected void ExpectSuccess()
		{
			int c = bstream.ReadByte();
			if (c == -1)
				throw new RedisResponseException("No more data");

			var s = ReadLine();
			Log((char)c + s);
			if (c == '-')
				throw new RedisResponseException(s.StartsWith("ERR") ? s.Substring(4) : s);
		}

		protected void SendExpectSuccess(string cmd, params object[] args)
		{
			if (!SendCommand(cmd, args))
				throw new Exception("Unable to connect");

			ExpectSuccess();
		}

		protected int SendExpectInt(string cmd, params object[] args)
		{
			if (!SendCommand(cmd, args))
				throw new Exception("Unable to connect");

			return ReadInt();
		}

		protected byte[] SendExpectData(byte[] data, string cmd, params object[] args)
		{
			if (!SendDataCommand(data, cmd, args))
				throw new Exception("Unable to connect");

			return ReadData();
		}

		private int ReadInt()
		{
			int c = bstream.ReadByte();
			if (c == -1)
				throw new RedisResponseException("No more data");

			var s = ReadLine();
			Log("R: " + s);
			if (c == '-')
				throw new RedisResponseException(s.StartsWith("ERR") ? s.Substring(4) : s);
			if (c == ':')
			{
				int i;
				if (int.TryParse(s, out i))
					return i;
			}
			throw new RedisResponseException("Unknown reply on integer request: " + c + s);
		}

		private byte[] ReadData()
		{
			string r = ReadLine();
			Log("R: {0}", r);
			if (r.Length == 0)
				throw new RedisResponseException("Zero length respose");

			char c = r[0];
			if (c == '-')
				throw new RedisResponseException(r.StartsWith("-ERR") ? r.Substring(5) : r.Substring(1));
			if (c == '$')
			{
				if (r == "$-1")
					return null;
				int n;

				if (Int32.TryParse(r.Substring(1), out n))
				{
					byte[] retbuf = new byte[n];
					bstream.Read(retbuf, 0, n);
					if (bstream.ReadByte() != '\r' || bstream.ReadByte() != '\n')
						throw new RedisResponseException("Invalid termination");
					return retbuf;
				}
				throw new RedisResponseException("Invalid length");
			}
			throw new RedisResponseException("Unexpected reply: " + r);
		}

		private byte[][] ReadMultiData()
		{
			int c = bstream.ReadByte();
			if (c == -1)
				throw new RedisResponseException("No more data");

			var s = ReadLine();
			Log("R: " + s);
			if (c == '-')
				throw new RedisResponseException(s.StartsWith("ERR") ? s.Substring(4) : s);
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

					byte [][] result = new byte[count][];

					for (int i = 0; i < count; i++)
						result[i] = ReadData();

					return result;
				}
			}
			throw new RedisResponseException("Unknown reply on multi-request: " + c + s);
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

			return SendExpectString("TYPE {0}\r\n", key);
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
			throw new RedisResponseException("Invalid value");
		}

		public void Set(string key, byte[] value)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			if (value == null)
				throw new ArgumentNullException("value");

			if (value.Length > OneGb)
				throw new ArgumentException("value exceeds 1G", "value");

			if (!SendDataCommand(value, "SET {0} {1}\r\n", key, value.Length))
				throw new Exception("Unable to connect");
			ExpectSuccess();
		}

		public int SetNX(string key, byte[] value)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			if (value == null)
				throw new ArgumentNullException("value");

			if (value.Length > OneGb)
				throw new ArgumentException("value exceeds 1G", "value");

			if (!SendDataCommand(value, "SETNX {0} {1}\r\n", key, value.Length))
				throw new Exception("Unable to connect");
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
			return SendExpectData(null, "GET " + key + "\r\n");
		}

		public byte[] GetSet(string key, byte[] value)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			if (value == null)
				throw new ArgumentNullException("value");

			if (value.Length > OneGb)
				throw new ArgumentException("value exceeds 1G", "value");

			if (!SendDataCommand(value, "GETSET {0} {1}\r\n", key, value.Length))
				throw new Exception("Unable to connect");

			return ReadData();
		}
		public int Exists(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			return SendExpectInt("EXISTS " + key + "\r\n");
		}

		public int Del(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			return SendExpectInt("DEL " + key + "\r\n", key);
		}

		public int Del(params string[] args)
		{
			if (args == null)
				throw new ArgumentNullException("args");
			return SendExpectInt("DEL " + string.Join(" ", args) + "\r\n");
		}

		public int Incr(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			return SendExpectInt("INCR " + key + "\r\n");
		}

		public int IncrBy(string key, int count)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			return SendExpectInt("INCRBY {0} {1}\r\n", key, count);
		}

		public int Decr(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			return SendExpectInt("DECR " + key + "\r\n");
		}

		public int DecrBy(string key, int count)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			return SendExpectInt("DECRBY {0} {1}\r\n", key, count);
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
			return SendGetString("RENAME {0} {1}\r\n", oldKeyname, newKeyname)[0] == '+';
		}

		public int Expire(string key, int seconds)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			return SendExpectInt("EXPIRE {0} {1}\r\n", key, seconds);
		}

		public int ExpireAt(string key, long unixTime)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			return SendExpectInt("EXPIREAT {0} {1}\r\n", key, unixTime);
		}

		public int Ttl(string key)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			return SendExpectInt("TTL {0}\r\n", key);
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

			if (!SendDataCommand(null, "MGET {0}\r\n", string.Join(" ", keys)))
				throw new Exception("Unable to connect");
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

		#endregion


		#region Set Operations

		public byte[][] SMembers(string setId)
		{
			if (!SendDataCommand(null, "SMEMBERS {0}\r\n", setId))
				throw new Exception("Unable to connect");
			return ReadMultiData();
		}

		public void SAdd(string setId, byte[] value)
		{
			if (setId == null)
				throw new ArgumentNullException("setId");
			if (value == null)
				throw new ArgumentNullException("value");

			if (!SendDataCommand(value, "SADD {0} {1}\r\n", setId, value.Length))
				throw new Exception("Unable to connect");
			ExpectSuccess();
		}

		public void SRem(string setId, byte[] value)
		{
			if (setId == null)
				throw new ArgumentNullException("setId");
			if (!SendDataCommand(value, "SREM {0} {1}\r\n", setId, value.Length))
				throw new Exception("Unable to connect");

			ExpectSuccess();
		}

		public byte[] SPop(string setId)
		{
			if (!SendDataCommand(null, "SPOP {0}\r\n", setId))
				throw new Exception("Unable to connect");
			return ReadData();
		}

		public void SMove(string fromSetId, string toSetId, byte[] value)
		{
			if (fromSetId == null)
				throw new ArgumentNullException("fromSetId");
			if (toSetId == null)
				throw new ArgumentNullException("toSetId");

			if (!SendDataCommand(value, "SMOVE {0} {1} {2}\r\n", fromSetId, toSetId, value.Length))
				throw new Exception("Unable to connect");

			ExpectSuccess();
		}

		public int SCard(string setId)
		{
			if (setId == null)
				throw new ArgumentNullException("setId");

			return SendExpectInt("SCARD {0}\r\n", setId);
		}

		public int SIsMember(string setId, byte[] value)
		{
			if (setId == null)
				throw new ArgumentNullException("setId");
			if (!SendDataCommand(value, "SISMEMBER {0} {1}\r\n", setId, value.Length))
				throw new Exception("Unable to connect");

			return ReadInt();
		}

		public byte[][] SInter(params string[] setIds)
		{
			if (!SendDataCommand(null, "SINTER {0}\r\n", string.Join(" ", setIds)))
				throw new Exception("Unable to connect");

			return ReadMultiData();
		}

		public void SInterStore(string intoSetId, params string[] setIds)
		{
			if (!SendDataCommand(null, "SINTERSTORE {0} {1}\r\n", intoSetId, string.Join(" ", setIds)))
				throw new Exception("Unable to connect");

			ExpectSuccess();
		}

		public byte[][] SUnion(params string[] setIds)
		{
			if (!SendDataCommand(null, "SUNION {0}\r\n", string.Join(" ", setIds)))
				throw new Exception("Unable to connect");

			return ReadMultiData();
		}

		public void SUnionStore(string intoSetId, params string[] setIds)
		{
			if (!SendDataCommand(null, "SUNIONSTORE {0} {1}\r\n", intoSetId, string.Join(" ", setIds)))
				throw new Exception("Unable to connect");

			ExpectSuccess();
		}

		public byte[][] SDiff(string fromSetId, params string[] withSetIds)
		{
			if (!SendDataCommand(null, "SDIFF {0} {1}\r\n", fromSetId, string.Join(" ", withSetIds)))
				throw new Exception("Unable to connect");

			return ReadMultiData();
		}

		public void SDiffStore(string intoSetId, string fromSetId, params string[] withSetIds)
		{
			if (!SendDataCommand(null, "SDIFFSTORE {0} {1} {2}\r\n", intoSetId, fromSetId, string.Join(" ", withSetIds)))
				throw new Exception("Unable to connect");

			ExpectSuccess();
		}

		public byte[] SRandMember(string setId)
		{
			if (!SendDataCommand(null, "SRANDMEMBER {0}\r\n", setId))
				throw new Exception("Unable to connect");
			return ReadData();
		}
		#endregion


		#region List Operations

		public byte[][] LRange(string listId, int startingFrom, int endingAt)
		{
			if (!SendDataCommand(null, "LRANGE {0} {1} {2}\r\n", listId, startingFrom, endingAt))
				throw new Exception("Unable to connect");

			return ReadMultiData();
		}

		public byte[][] Sort(string listOrSetId, int startingFrom, int endingAt, bool sortAlpha, bool sortDesc)
		{
			var sortAlphaOption = sortAlpha ? " ALPHA" : "";
			var sortDescOption = sortDesc ? " DESC" : "";

			if (!SendDataCommand(null, "SORT {0} LIMIT {1} {2}{3}{4}\r\n", listOrSetId, startingFrom, endingAt,
								 sortAlphaOption, sortDescOption))
				throw new Exception("Unable to connect");

			return ReadMultiData();
		}

		public void RPush(string listId, byte[] value)
		{
			if (listId == null)
				throw new ArgumentNullException("listId");
			if (value == null)
				throw new ArgumentNullException("value");

			if (!SendDataCommand(value, "RPUSH {0} {1}\r\n", listId, value.Length))
				throw new Exception("Unable to connect");
			ExpectSuccess();
		}

		public void LPush(string listId, byte[] value)
		{
			if (listId == null)
				throw new ArgumentNullException("listId");
			if (value == null)
				throw new ArgumentNullException("value");

			if (!SendDataCommand(value, "LPUSH {0} {1}\r\n", listId, value.Length))
				throw new Exception("Unable to connect");
			ExpectSuccess();
		}

		public void LTrim(string listId, int keepStartingFrom, int keepEndingAt)
		{
			if (!SendCommand("LTRIM {0} {1} {2}\r\n", listId, keepStartingFrom, keepEndingAt))
				throw new Exception("Unable to connect");

			ExpectSuccess();
		}

		public int LRem(string listId, int removeNoOfMatches, byte[] value)
		{
			if (!SendDataCommand(value, "LREM {0} {1} {2}\r\n", listId, removeNoOfMatches, value.Length))
				throw new Exception("Unable to connect");

			return ReadInt();
		}

		public int LLen(string listId)
		{
			if (listId == null)
				throw new ArgumentNullException("listId");

			return SendExpectInt("LLEN {0}\r\n", listId);
		}

		public byte[] LIndex(string listId, int listIndex)
		{
			if (!SendCommand("LINDEX {0} {1}\r\n", listId, listIndex))
				throw new Exception("Unable to connect");
			return ReadData();
		}

		public void LSet(string listId, int listIndex, byte[] value)
		{
			if (!SendDataCommand(value, "LSET {0} {1} {2}\r\n", listId, listIndex, value.Length))
				throw new Exception("Unable to connect");

			ExpectSuccess();
		}

		public byte[] LPop(string listId)
		{
			if (!SendCommand("LPOP {0}\r\n", listId))
				throw new Exception("Unable to connect");
			return ReadData();
		}

		public byte[] RPop(string listId)
		{
			if (!SendCommand("RPOP {0}\r\n", listId))
				throw new Exception("Unable to connect");
			return ReadData();
		}

		public void RPopLPush(string fromListId, string toListId)
		{
			if (fromListId == null)
				throw new ArgumentNullException("fromListId");
			if (toListId == null)
				throw new ArgumentNullException("toListId");

			var hasBug = this.ServerVersion.CompareTo("1.2.0") <= 0;
			if (hasBug)
			{
				var value = Encoding.UTF8.GetBytes(toListId);
				if (!SendDataCommand(value, "RPOPLPUSH {0} {1}\r\n", fromListId, value.Length))
					throw new Exception("Unable to connect");

				ReadData();
				return;
			}

			if (!SendCommand("RPOPLPUSH {0} {1}\r\n", fromListId, toListId))
				throw new Exception("Unable to connect");

			ExpectSuccess();
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
				Quit();
				if (socket != null)
					socket.Close();
				socket = null;
			}
		}
	}
}
