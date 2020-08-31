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
using ServiceStack.Redis.Pipeline;

namespace ServiceStack.Redis
{
    /// <summary>
    /// Interface to redis transaction
    /// </summary>
    public interface IRedisTransactionAsync
        : IRedisTransactionBaseAsync, IRedisQueueableOperationAsync, IAsyncDisposable
    {
        ValueTask<bool> CommitAsync(CancellationToken cancellationToken = default);
        ValueTask RollbackAsync(CancellationToken cancellationToken = default);
    }
}