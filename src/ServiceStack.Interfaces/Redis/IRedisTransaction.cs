//
// https://github.com/mythz/ServiceStack.Redis
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2013 ServiceStack.
//
// Licensed under the same terms of Redis and ServiceStack: new BSD license.
//

using System;
using ServiceStack.Redis.Pipeline;

namespace ServiceStack.Redis
{
    /// <summary>
    /// Interface to redis transaction
    /// </summary>
	public interface IRedisTransaction
        : IRedisTransactionBase, IRedisQueueableOperation, IDisposable
	{
		bool Commit();
		void Rollback();
	}
}