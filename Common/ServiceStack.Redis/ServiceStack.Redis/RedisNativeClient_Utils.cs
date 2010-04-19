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

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServiceStack.Redis
{
	public partial class RedisNativeClient
	{

		private void Connect()
		{
			socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
			{
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
				LastConnectedAtTimestamp = Stopwatch.GetTimestamp();
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
			if (LastConnectedAtTimestamp > 0)
			{
				var now = Stopwatch.GetTimestamp();
				var elapsedSecs = (now - LastConnectedAtTimestamp) / Stopwatch.Frequency;

				if (elapsedSecs > IdleTimeOutSecs && !socket.IsConnected())
				{
					return Reconnect();
				}
				LastConnectedAtTimestamp = now;
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
				.Replace('\t', '_').Replace('\n', '_');
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

		private static byte[] GetBytes(char cmdPrefix, int noOfLines, string cmdSuffix)
		{
			var cmd = cmdPrefix.ToString() + noOfLines.ToString() + cmdSuffix;
			return cmd.ToUtf8Bytes();
		}

		/// <summary>
		/// Command to set multuple binary safe arguments
		/// </summary>
		/// <param name="cmd"></param>
		/// <param name="safeBinaryValues"></param>
		/// <returns></returns>
		protected bool SendMultiDataCommand(string cmd, params byte[][] safeBinaryValues)
		{
			if (!AssertConnectedSocket()) return false;

			var cmdBytes = Encoding.UTF8.GetBytes(cmd);
			this.lastCommand = cmd;

			try
			{
				Log("S: " + cmd);

				//Total command lines count
				socket.Send(GetBytes('*', safeBinaryValues.Length + 1, "\r\n"));

				//Size of command, then command
				socket.Send(GetBytes('$', cmdBytes.Length, "\r\n"));
				socket.Send(cmdBytes);
				socket.Send(endData);

				foreach (var safeBinaryValue in safeBinaryValues)
				{					
					socket.Send(GetBytes('$', safeBinaryValue.Length, "\r\n"));
					socket.Send(safeBinaryValue);
					socket.Send(endData);
				}
			}
			catch (SocketException ex)
			{
				return HandleSocketException(ex);
			}
			return true;
		}

		protected bool SendDataCommand(byte[] data, string cmd, params object[] args)
		{
			if (!AssertConnectedSocket()) return false;

			var s = args.Length > 0 ? String.Format(cmd, args) : cmd;
			s += "\r\n";
			this.lastCommand = s;

			byte[] r = Encoding.UTF8.GetBytes(s);
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
			s += "\r\n";
			this.lastCommand = s;

			byte[] r = Encoding.UTF8.GetBytes(s);
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

		private int SafeReadByte()
		{
			return bstream.ReadByte();
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

		private double SendDataExpectDataAsDouble(byte[] data, string cmd, params object[] args)
		{
			if (!SendDataCommand(data, cmd, args))
				throw CreateConnectionError();

			return ReadDataAsDouble();
		}

		private int SendMultiDataExpectInt(string cmd, params byte[][] safeBinaryLines)
		{
			if (!SendMultiDataCommand(cmd, safeBinaryLines))
				throw CreateConnectionError();

			if (this.CurrentTransaction != null)
			{
				this.CurrentTransaction.CompleteIntQueuedCommand(ReadInt);
				ExpectQueued();
				return default(int);
			}
			return ReadInt();
		}

		private byte[] SendMultiDataExpectData(string cmd, params byte[][] safeBinaryLines)
		{
			if (!SendMultiDataCommand(cmd, safeBinaryLines))
				throw CreateConnectionError();

			if (this.CurrentTransaction != null)
			{
				this.CurrentTransaction.CompleteBytesQueuedCommand(ReadData);
				ExpectQueued();
				return null;
			}
			return ReadData();
		}

		private byte[][] SendMultiDataExpectMultiData(string cmd, params byte[][] safeBinaryLines)
		{
			if (!SendMultiDataCommand(cmd, safeBinaryLines))
				throw CreateConnectionError();

			if (this.CurrentTransaction != null)
			{
				this.CurrentTransaction.CompleteMultiBytesQueuedCommand(ReadMultiData);
				ExpectQueued();
				return null;
			}
			return ReadMultiData();
		}

		private int SendDataExpectInt(byte[] data, string cmd, params object[] args)
		{
			if (!SendDataCommand(data, cmd, args))
				throw CreateConnectionError();

			if (this.CurrentTransaction != null)
			{
				this.CurrentTransaction.CompleteIntQueuedCommand(ReadInt);
				ExpectQueued();
				return default(int);
			}
			return ReadInt();
		}

		private void SendDataExpectSuccess(byte[] data, string cmd, params object[] args)
		{
			if (!SendDataCommand(data, cmd, args))
				throw CreateConnectionError();

			if (this.CurrentTransaction != null)
			{
				this.CurrentTransaction.CompleteVoidQueuedCommand(ExpectSuccess);
				ExpectQueued();
				return;
			}
			ExpectSuccess();
		}

		protected string SendGetString(string cmd, params object[] args)
		{
			if (!SendCommand(cmd, args))
				throw CreateConnectionError();

			if (this.CurrentTransaction != null)
			{
				this.CurrentTransaction.CompleteStringQueuedCommand(ReadLine);
				ExpectQueued();
				return null;
			}
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

		private void ExpectWord(string word)
		{
			int c = SafeReadByte();
			if (c == -1)
				throw CreateResponseError("No more data");

			var s = ReadLine();

			Log((char)c + s);

			if (c == '-')
				throw CreateResponseError(s.StartsWith("ERR") ? s.Substring(4) : s);

			if (s != word)
				throw CreateResponseError(string.Format("Expected '{0}' got '{1}'", word, s));
		}

		protected void ExpectOk()
		{
			ExpectWord("OK");
		}

		protected void ExpectQueued()
		{
			ExpectWord("QUEUED");
		}

		protected void SendExpectSuccess(string cmd, params object[] args)
		{
			if (!SendCommand(cmd, args))
				throw CreateConnectionError();

			if (this.CurrentTransaction != null)
			{
				this.CurrentTransaction.CompleteVoidQueuedCommand(ExpectSuccess);
				ExpectQueued();
				return;
			}
			ExpectSuccess();
		}

		protected int SendExpectInt(string cmd, params object[] args)
		{
			if (!SendCommand(cmd, args))
				throw CreateConnectionError();

			if (this.CurrentTransaction != null)
			{
				this.CurrentTransaction.CompleteIntQueuedCommand(ReadInt);
				ExpectQueued();
				return default(int);
			}
			return ReadInt();
		}

		protected double SendExpectDouble(string cmd, params object[] args)
		{
			if (!SendCommand(cmd, args))
				throw CreateConnectionError();

			if (this.CurrentTransaction != null)
			{
				this.CurrentTransaction.CompleteDoubleQueuedCommand(ReadDataAsDouble);
				ExpectQueued();
				return default(double);
			}
			return ReadDataAsDouble();
		}

		protected double SendDataExpectDouble(byte[] data, string cmd, params object[] args)
		{
			if (!SendDataCommand(data, cmd, args))
				throw CreateConnectionError();

			if (this.CurrentTransaction != null)
			{
				this.CurrentTransaction.CompleteDoubleQueuedCommand(ReadDataAsDouble);
				ExpectQueued();
				return default(double);
			}
			return ReadDataAsDouble();
		}

		protected void SendExpectOk(string cmd, params object[] args)
		{
			if (!SendCommand(cmd, args))
				throw CreateConnectionError();

			ExpectOk();
		}

		protected byte[] SendExpectData(string cmd, params object[] args)
		{
			if (!SendCommand(cmd, args))
				throw CreateConnectionError();

			if (this.CurrentTransaction != null)
			{
				this.CurrentTransaction.CompleteBytesQueuedCommand(ReadData);
				ExpectQueued();
				return null;
			}
			return ReadData();
		}

		protected byte[] SendDataExpectData(byte[] data, string cmd, params object[] args)
		{
			if (!SendDataCommand(data, cmd, args))
				throw CreateConnectionError();

			if (this.CurrentTransaction != null)
			{
				this.CurrentTransaction.CompleteBytesQueuedCommand(ReadData);
				ExpectQueued();
				return null;
			}
			return ReadData();
		}

		protected byte[][] SendExpectMultiData(string cmd, params object[] args)
		{
			if (!SendCommand(cmd, args))
				throw CreateConnectionError();

			if (this.CurrentTransaction != null)
			{
				this.CurrentTransaction.CompleteMultiBytesQueuedCommand(ReadMultiData);
				ExpectQueued();
				return null;
			}
			return ReadMultiData();
		}

		protected byte[][] SendDataExpectMultiData(byte[] data, string cmd, params object[] args)
		{
			if (!SendDataCommand(data, cmd, args))
				throw CreateConnectionError();

			if (this.CurrentTransaction != null)
			{
				this.CurrentTransaction.CompleteMultiBytesQueuedCommand(ReadMultiData);
				ExpectQueued();
				return null;
			}
			return ReadMultiData();
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

			if (c == ':' || c == '$')//really strange why ZRANK needs the '$' here
			{
				int i;
				if (int.TryParse(s, out i))
					return i;
			}
			throw CreateResponseError("Unknown reply on integer response: " + c + s);
		}

		private double ReadDataAsDouble()
		{
			var doubleBytes = ReadData();
			var doubleString = Encoding.UTF8.GetString(doubleBytes);

			double d;
			double.TryParse(doubleString, out d);

			return d;
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

		private int ReadMultiDataResultCount()
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
					return count;
				}
			}
			throw CreateResponseError("Unknown reply on multi-request: " + c + s);
		}

		private static void AssertListIdAndValue(string listId, byte[] value)
		{
			if (listId == null)
				throw new ArgumentNullException("listId");
			if (value == null)
				throw new ArgumentNullException("value");
		}
	}
}