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
	public interface IRedisTransaction 
		: IDisposable
	{
		void QueueCommand(Action<IRedisClient> command);
		void QueueCommand(Action<IRedisClient> command, Action onSuccessCallback);
		void QueueCommand(Action<IRedisClient> command, Action onSuccessCallback, Action<Exception> onErrorCallback);

		void QueueCommand(Func<IRedisClient, double> command);
		void QueueCommand(Func<IRedisClient, double> command, Action<double> onSuccessCallback);
		void QueueCommand(Func<IRedisClient, double> command, Action<double> onSuccessCallback, Action<Exception> onErrorCallback);

		void QueueCommand(Func<IRedisClient, int> command);
		void QueueCommand(Func<IRedisClient, int> command, Action<int> onSuccessCallback);
		void QueueCommand(Func<IRedisClient, int> command, Action<int> onSuccessCallback, Action<Exception> onErrorCallback);

		void QueueCommand(Func<IRedisClient, byte[]> command);
		void QueueCommand(Func<IRedisClient, byte[]> command, Action<byte[]> onSuccessCallback);
		void QueueCommand(Func<IRedisClient, byte[]> command, Action<byte[]> onSuccessCallback, Action<Exception> onErrorCallback);

		void QueueCommand(Func<IRedisClient, string> command);
		void QueueCommand(Func<IRedisClient, string> command, Action<string> onSuccessCallback);
		void QueueCommand(Func<IRedisClient, string> command, Action<string> onSuccessCallback, Action<Exception> onErrorCallback);

		void QueueCommand(Func<IRedisClient, List<string>> command);
		void QueueCommand(Func<IRedisClient, List<string>> command, Action<List<string>> onSuccessCallback);
		void QueueCommand(Func<IRedisClient, List<string>> command, Action<List<string>> onSuccessCallback, Action<Exception> onErrorCallback);
		
		void Commit();
		void Rollback();
	}
}