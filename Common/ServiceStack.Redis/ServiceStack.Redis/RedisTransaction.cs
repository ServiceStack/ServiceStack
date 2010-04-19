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

namespace ServiceStack.Redis
{
	/// <summary>
	/// Adds support for Redis Transactions (i.e. MULTI/EXEC/DISCARD operations).
	/// </summary>
	public class RedisTransaction
		: IRedisTransaction, IRedisQueableTransaction
	{
		private readonly List<QueuedRedisOperation> queuedCommands = new List<QueuedRedisOperation>();

		private readonly RedisClient redisClient;
		private QueuedRedisOperation currentQueuedOperation;

		public RedisTransaction(RedisClient redisClient)
		{
			this.redisClient = redisClient;

			if (redisClient.CurrentTransaction != null)
				throw new InvalidOperationException("An atomic command is already in use");

			redisClient.CurrentTransaction = this;
			redisClient.Multi();
		}

		private void BeginQueuedCommand(QueuedRedisOperation queuedRedisOperation)
		{
			if (currentQueuedOperation != null)
				throw new InvalidOperationException("The previous queued operation has not been commited");

			currentQueuedOperation = queuedRedisOperation;
		}

		private void AssertCurrentOperation()
		{
			if (currentQueuedOperation == null)
				throw new InvalidOperationException("No queued operation is currently set");
		}

		private void AddCurrentQueuedOperation()
		{
			this.queuedCommands.Add(currentQueuedOperation);
			currentQueuedOperation = null;
		}

		public void CompleteVoidQueuedCommand(Action voidReadCommand)
		{
			AssertCurrentOperation();

			currentQueuedOperation.VoidReadCommand = voidReadCommand;
			AddCurrentQueuedOperation();
		}

		public void CompleteIntQueuedCommand(Func<int> intReadCommand)
		{
			AssertCurrentOperation();

			currentQueuedOperation.IntReadCommand = intReadCommand;
			AddCurrentQueuedOperation();
		}

		public void CompleteBytesQueuedCommand(Func<byte[]> bytesReadCommand)
		{
			AssertCurrentOperation();

			currentQueuedOperation.BytesReadCommand = bytesReadCommand;
			AddCurrentQueuedOperation();
		}

		public void CompleteMultiBytesQueuedCommand(Func<byte[][]> multiBytesReadCommand)
		{
			AssertCurrentOperation();

			currentQueuedOperation.MultiBytesReadCommand = multiBytesReadCommand;
			AddCurrentQueuedOperation();
		}

		public void CompleteStringQueuedCommand(Func<string> stringReadCommand)
		{
			AssertCurrentOperation();

			currentQueuedOperation.StringReadCommand = stringReadCommand;
			AddCurrentQueuedOperation();
		}

		public void CompleteMultiStringQueuedCommand(Func<List<string>> multiStringReadCommand)
		{
			AssertCurrentOperation();

			currentQueuedOperation.MultiStringReadCommand = multiStringReadCommand;
			AddCurrentQueuedOperation();
		}

		public void CompleteDoubleQueuedCommand(Func<double> doubleReadCommand)
		{
			AssertCurrentOperation();

			currentQueuedOperation.DoubleReadCommand = doubleReadCommand;
			AddCurrentQueuedOperation();
		}


		public void QueueCommand(Action<IRedisClient> command)
		{
			QueueCommand(command, null, null);
		}

		public void QueueCommand(Action<IRedisClient> command, Action onSuccessCallback)
		{
			QueueCommand(command, onSuccessCallback, null);
		}

		public void QueueCommand(Action<IRedisClient> command, Action onSuccessCallback, Action<Exception> onErrorCallback)
		{
			BeginQueuedCommand(new QueuedRedisOperation
			{
				OnSuccessVoidCallback = onSuccessCallback,
				OnErrorCallback = onErrorCallback
			});
			command(redisClient);
		}


		public void QueueCommand(Func<IRedisClient, int> command)
		{
			QueueCommand(command, null, null);
		}

		public void QueueCommand(Func<IRedisClient, int> command, Action<int> onSuccessCallback)
		{
			QueueCommand(command, onSuccessCallback, null);
		}

		public void QueueCommand(Func<IRedisClient, int> command, Action<int> onSuccessCallback, Action<Exception> onErrorCallback)
		{
			BeginQueuedCommand(new QueuedRedisOperation
			{
				OnSuccessIntCallback = onSuccessCallback,
				OnErrorCallback = onErrorCallback
			});
			command(redisClient);
		}


		public void QueueCommand(Func<IRedisClient, bool> command)
		{
			QueueCommand(command, null, null);
		}

		public void QueueCommand(Func<IRedisClient, bool> command, Action<bool> onSuccessCallback)
		{
			QueueCommand(command, onSuccessCallback, null);
		}

		public void QueueCommand(Func<IRedisClient, bool> command, Action<bool> onSuccessCallback, Action<Exception> onErrorCallback)
		{
			BeginQueuedCommand(new QueuedRedisOperation
			{
				OnSuccessBoolCallback = onSuccessCallback,
				OnErrorCallback = onErrorCallback
			});
			command(redisClient);
		}


		public void QueueCommand(Func<IRedisClient, double> command)
		{
			QueueCommand(command, null, null);
		}

		public void QueueCommand(Func<IRedisClient, double> command, Action<double> onSuccessCallback)
		{
			QueueCommand(command, onSuccessCallback, null);
		}

		public void QueueCommand(Func<IRedisClient, double> command, Action<double> onSuccessCallback, Action<Exception> onErrorCallback)
		{
			BeginQueuedCommand(new QueuedRedisOperation
			{
				OnSuccessDoubleCallback = onSuccessCallback,
				OnErrorCallback = onErrorCallback
			});
			command(redisClient);
		}


		public void QueueCommand(Func<IRedisClient, byte[]> command)
		{
			QueueCommand(command, null, null);
		}

		public void QueueCommand(Func<IRedisClient, byte[]> command, Action<byte[]> onSuccessCallback)
		{
			QueueCommand(command, onSuccessCallback, null);
		}

		public void QueueCommand(Func<IRedisClient, byte[]> command, Action<byte[]> onSuccessCallback, Action<Exception> onErrorCallback)
		{
			BeginQueuedCommand(new QueuedRedisOperation
			{
				OnSuccessBytesCallback = onSuccessCallback,
				OnErrorCallback = onErrorCallback
			});
			command(redisClient);
		}


		public void QueueCommand(Func<IRedisClient, string> command)
		{
			QueueCommand(command, null, null);
		}

		public void QueueCommand(Func<IRedisClient, string> command, Action<string> onSuccessCallback)
		{
			QueueCommand(command, onSuccessCallback, null);
		}

		public void QueueCommand(Func<IRedisClient, string> command, Action<string> onSuccessCallback, Action<Exception> onErrorCallback)
		{
			BeginQueuedCommand(new QueuedRedisOperation
			{
				OnSuccessStringCallback = onSuccessCallback,
				OnErrorCallback = onErrorCallback
			});
			command(redisClient);
		}


		public void QueueCommand(Func<IRedisClient, byte[][]> command)
		{
			QueueCommand(command, null, null);
		}

		public void QueueCommand(Func<IRedisClient, byte[][]> command, Action<byte[][]> onSuccessCallback)
		{
			QueueCommand(command, onSuccessCallback, null);
		}

		public void QueueCommand(Func<IRedisClient, byte[][]> command, Action<byte[][]> onSuccessCallback, Action<Exception> onErrorCallback)
		{
			BeginQueuedCommand(new QueuedRedisOperation
			{
				OnSuccessMultiBytesCallback = onSuccessCallback,
				OnErrorCallback = onErrorCallback
			});
			command(redisClient);
		}


		public void QueueCommand(Func<IRedisClient, List<string>> command)
		{
			QueueCommand(command, null, null);
		}

		public void QueueCommand(Func<IRedisClient, List<string>> command, Action<List<string>> onSuccessCallback)
		{
			QueueCommand(command, onSuccessCallback, null);
		}

		public void QueueCommand(Func<IRedisClient, List<string>> command, Action<List<string>> onSuccessCallback, Action<Exception> onErrorCallback)
		{
			BeginQueuedCommand(new QueuedRedisOperation
			{
				OnSuccessMultiStringCallback = onSuccessCallback,
				OnErrorCallback = onErrorCallback
			});
			command(redisClient);
		}


		public void Commit()
		{
			try
			{
				var resultCount = redisClient.Exec();
				if (resultCount != queuedCommands.Count)
					throw new InvalidOperationException(string.Format(
						"Invalid results received from 'EXEC', expected '{0}' received '{1}'"
						+ "\nWarning: Transaction was committed",
						queuedCommands.Count, resultCount));

				foreach (var queuedCommand in queuedCommands)
				{
					queuedCommand.ProcessResult();
				}
			}
			finally
			{
				redisClient.CurrentTransaction = null;
			}
		}

		public void Rollback()
		{
			if (redisClient.CurrentTransaction == null) 
				throw new InvalidOperationException("There is no current transaction to Rollback");

			redisClient.CurrentTransaction = null;
			redisClient.Discard();
		}

		public void Dispose()
		{
			if (redisClient.CurrentTransaction == null) return;
			Rollback();
		}
	}
}