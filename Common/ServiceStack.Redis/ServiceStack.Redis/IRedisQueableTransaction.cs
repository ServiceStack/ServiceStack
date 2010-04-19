using System;
using System.Collections.Generic;

namespace ServiceStack.Redis
{
	public interface IRedisQueableTransaction
	{
		void CompleteVoidQueuedCommand(Action voidReadCommand);
		void CompleteIntQueuedCommand(Func<int> intReadCommand);
		void CompleteBytesQueuedCommand(Func<byte[]> bytesReadCommand);
		void CompleteMultiBytesQueuedCommand(Func<byte[][]> multiBytesReadCommand);
		void CompleteStringQueuedCommand(Func<string> stringReadCommand);
		void CompleteMultiStringQueuedCommand(Func<List<string>> multiStringReadCommand);
		void CompleteDoubleQueuedCommand(Func<double> doubleReadCommand);
	}
}