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

namespace ServiceStack.Redis.Generic
{
    /// <summary>
    /// Redis transaction for typed client
    /// </summary>
    /// <typeparam name="T"></typeparam>
	public interface IRedisTypedTransaction<T>: IRedisTypedQueueableOperation<T>, IDisposable
	{
		bool Commit();
		void Rollback();
	}
}