//
// https://github.com/ServiceStack/ServiceStack.Redis
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2017 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Generic
{
    /// <summary>
    /// Redis transaction for typed client
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IRedisTypedTransactionAsync<T> : IRedisTypedQueueableOperationAsync<T>, IAsyncDisposable
    {
        ValueTask<bool> CommitAsync(CancellationToken cancellationToken = default);
        ValueTask RollbackAsync(CancellationToken cancellationToken = default);
    }
}