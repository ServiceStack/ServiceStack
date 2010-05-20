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
using ServiceStack.Text;

namespace ServiceStack.Redis
{
	public partial class RedisNativeClient
	{
		private void Connect()
		{
			Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) {
				SendTimeout = SendTimeout
			};
			try
			{
				Socket.Connect(Host, Port);

				if (!Socket.Connected)
				{
					Socket.Close();
					Socket = null;
					return;
				}
				Bstream = new BufferedStream(new NetworkStream(Socket), 16 * 1024);

				if (Password != null)
					SendExpectSuccess(Commands.Auth, Password.ToUtf8Bytes());

				db = 0;
				var ipEndpoint = Socket.LocalEndPoint as IPEndPoint;
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
			while ((c = Bstream.ReadByte()) != -1)
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

				if (elapsedSecs > IdleTimeOutSecs && !Socket.IsConnected())
				{
					return Reconnect();
				}
				LastConnectedAtTimestamp = now;
			}

			if (Socket == null)
				Connect();

			var isConnected = Socket != null;

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

			return Socket != null;
		}

		private bool HandleSocketException(SocketException ex)
		{
			HadExceptions = true;
			log.Error("SocketException: ", ex);

			lastSocketException = ex;

			// timeout?
			Socket.Close();
			Socket = null;

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

		private static byte[] GetCmdBytes(char cmdPrefix, int noOfLines)
		{
			var strLines = noOfLines.ToString();
			var strLinesLength = strLines.Length;

			var cmdBytes = new byte[1 + strLinesLength + 2];
			cmdBytes[0] = (byte)cmdPrefix;

			for (var i = 0; i < strLinesLength; i++)
				cmdBytes[i + 1] = (byte)strLines[i];

			cmdBytes[1 + strLinesLength] = 0x0D; // \r
			cmdBytes[2 + strLinesLength] = 0x0A; // \n

			return cmdBytes;
		}

		/// <summary>
		/// Command to set multuple binary safe arguments
		/// </summary>
		/// <param name="cmdWithBinaryArgs"></param>
		/// <returns></returns>
		protected bool SendCommand(params byte[][] cmdWithBinaryArgs)
		{
			if (!AssertConnectedSocket()) return false;

			try
			{
				CmdLog(cmdWithBinaryArgs);

				//Total command lines count
				WriteToSendBuffer(GetCmdBytes('*', cmdWithBinaryArgs.Length));

				foreach (var safeBinaryValue in cmdWithBinaryArgs)
				{
					WriteToSendBuffer(GetCmdBytes('$', safeBinaryValue.Length));
					WriteToSendBuffer(safeBinaryValue);
					WriteToSendBuffer(endData);
				}

				FlushSendBuffer();
			}
			catch (SocketException ex)
			{
				return HandleSocketException(ex);
			}
			finally
			{
				cmdBufferIndex = 0;
			}
			return true;
		}

		byte[] cmdBuffer = new byte[32 * 1024];
		int cmdBufferIndex = 0;

		public void WriteToSendBuffer(byte[] cmdBytes)
		{
			if ((cmdBufferIndex + cmdBytes.Length) > cmdBuffer.Length)
			{
				const int breathingSpaceToReduceReallocations = (32 * 1024);
				var newLargerBuffer = new byte[cmdBufferIndex + cmdBytes.Length + breathingSpaceToReduceReallocations];
				Buffer.BlockCopy(cmdBuffer, 0, newLargerBuffer, 0, cmdBuffer.Length);
				cmdBuffer = newLargerBuffer;
			}

			Buffer.BlockCopy(cmdBytes, 0, cmdBuffer, cmdBufferIndex, cmdBytes.Length);
			cmdBufferIndex += cmdBytes.Length;
		}

		public void FlushSendBuffer()
		{
			Socket.Send(cmdBuffer, cmdBufferIndex, SocketFlags.None);
		}

		private int SafeReadByte()
		{
			return Bstream.ReadByte();
		}

		private void SendExpectSuccess(params byte[][] cmdWithBinaryArgs)
		{
			if (!SendCommand(cmdWithBinaryArgs))
				throw CreateConnectionError();

			if (this.CurrentTransaction != null)
			{
				this.CurrentTransaction.CompleteVoidQueuedCommand(ExpectSuccess);
				ExpectQueued();
				return;
			}
			ExpectSuccess();
		}

		private int SendExpectInt(params byte[][] cmdWithBinaryArgs)
		{
			if (!SendCommand(cmdWithBinaryArgs))
				throw CreateConnectionError();

			if (this.CurrentTransaction != null)
			{
				this.CurrentTransaction.CompleteIntQueuedCommand(ReadInt);
				ExpectQueued();
				return default(int);
			}
			return ReadInt();
		}

		private byte[] SendExpectData(params byte[][] cmdWithBinaryArgs)
		{
			if (!SendCommand(cmdWithBinaryArgs))
				throw CreateConnectionError();

			if (this.CurrentTransaction != null)
			{
				this.CurrentTransaction.CompleteBytesQueuedCommand(ReadData);
				ExpectQueued();
				return null;
			}
			return ReadData();
		}

		private string SendExpectString(params byte[][] cmdWithBinaryArgs)
		{
			var bytes = SendExpectData(cmdWithBinaryArgs);
			return bytes.FromUtf8Bytes();
		}

		private double SendExpectDouble(params byte[][] cmdWithBinaryArgs)
		{
			var doubleBytes = SendExpectData(cmdWithBinaryArgs);
			var doubleString = Encoding.UTF8.GetString(doubleBytes);

			double d;
			double.TryParse(doubleString, out d);

			return d;
		}

		private string SendExpectCode(params byte[][] cmdWithBinaryArgs)
		{
			if (!SendCommand(cmdWithBinaryArgs))
				throw CreateConnectionError();

			if (this.CurrentTransaction != null)
			{
				this.CurrentTransaction.CompleteBytesQueuedCommand(ReadData);
				ExpectQueued();
				return null;
			}

			return ExpectCode();
		}

		private byte[][] SendExpectMultiData(params byte[][] cmdWithBinaryArgs)
		{
			if (!SendCommand(cmdWithBinaryArgs))
				throw CreateConnectionError();

			if (this.CurrentTransaction != null)
			{
				this.CurrentTransaction.CompleteMultiBytesQueuedCommand(ReadMultiData);
				ExpectQueued();
				return null;
			}
			return ReadMultiData();
		}

		[Conditional("DEBUG")]
		protected void Log(string fmt, params object[] args)
		{
			log.DebugFormat("{0}", string.Format(fmt, args).Trim());
		}

		[Conditional("DEBUG")]
		protected void CmdLog(byte[][] args)
		{
			var sb = new StringBuilder();
			foreach (var arg in args)
			{
				if (sb.Length > 0)
					sb.Append(" ");

				sb.Append(arg.FromUtf8Bytes());
			}
			this.lastCommand = sb.ToString();
			if (this.lastCommand.Length > 100)
			{
				this.lastCommand = this.lastCommand.Substring(0, 100) + "...";
			}

			log.Debug("S: " + this.lastCommand);
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

		private string ExpectCode()
		{
			int c = SafeReadByte();
			if (c == -1)
				throw CreateResponseError("No more data");

			var s = ReadLine();

			Log((char)c + s);

			if (c == '-')
				throw CreateResponseError(s.StartsWith("ERR") ? s.Substring(4) : s);

			return s;
		}

		protected void ExpectOk()
		{
			ExpectWord("OK");
		}

		protected void ExpectQueued()
		{
			ExpectWord("QUEUED");
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
						var readCount = Bstream.Read(retbuf, offset, count);
						if (readCount <= 0)
							throw CreateResponseError("Unexpected end of Stream");

						offset += readCount;
						count -= readCount;
					}

					if (Bstream.ReadByte() != '\r' || Bstream.ReadByte() != '\n')
						throw CreateResponseError("Invalid termination");

					return retbuf;
				}
				throw CreateResponseError("Invalid length");
			}

			if (c == ':')
			{
				//match the return value
				return r.Substring(1).ToUtf8Bytes();
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

		private static byte[][] MergeCommandWithKeysAndValues(byte[] cmd, byte[][] keys, byte[][] values)
		{
			var firstParams = new[] { cmd };
			return MergeCommandWithKeysAndValues(firstParams, keys, values);
		}

		private static byte[][] MergeCommandWithKeysAndValues(byte[] cmd, byte[] firstArg, byte[][] keys, byte[][] values)
		{
			var firstParams = new[] { cmd, firstArg };
			return MergeCommandWithKeysAndValues(firstParams, keys, values);
		}

		private static byte[][] MergeCommandWithKeysAndValues(byte[][] firstParams,
			byte[][] keys, byte[][] values)
		{
			if (keys == null || keys.Length == 0)
				throw new ArgumentNullException("keys");
			if (values == null || values.Length == 0)
				throw new ArgumentNullException("values");
			if (keys.Length != values.Length)
				throw new ArgumentException("The number of values must be equal to the number of keys");

			var keyValueStartIndex = (firstParams != null) ? firstParams.Length : 0;

			var keysAndValuesLength = keys.Length * 2 + keyValueStartIndex;
			var keysAndValues = new byte[keysAndValuesLength][];

			for (var i = 0; i < keyValueStartIndex; i++)
			{
				keysAndValues[i] = firstParams[i];
			}

			var j = 0;
			for (var i = keyValueStartIndex; i < keysAndValuesLength; i += 2)
			{
				keysAndValues[i] = keys[j];
				keysAndValues[i + 1] = values[j];
				j++;
			}
			return keysAndValues;
		}

		private static byte[][] MergeCommandWithArgs(byte[] cmd, params string[] args)
		{
			var mergedBytes = new byte[1 + args.Length][];
			mergedBytes[0] = cmd;
			for (var i = 0; i < args.Length; i++)
			{
				mergedBytes[i + 1] = args[i].ToUtf8Bytes();
			}
			return mergedBytes;
		}

		private static byte[][] MergeCommandWithArgs(byte[] cmd, byte[] firstArg, params byte[][] args)
		{
			var mergedBytes = new byte[2 + args.Length][];
			mergedBytes[0] = cmd;
			mergedBytes[1] = firstArg;
			for (var i = 0; i < args.Length; i++)
			{
				mergedBytes[i + 2] = args[i];
			}
			return mergedBytes;
		}

		protected byte[][] ConvertToBytes(string[] keys)
		{
			var keyBytes = new byte[keys.Length][];
			for (var i = 0; i < keys.Length; i++)
			{
				var key = keys[i];
				keyBytes[i] = key != null ? key.ToUtf8Bytes() : new byte[0];
			}
			return keyBytes;
		}

	}
}