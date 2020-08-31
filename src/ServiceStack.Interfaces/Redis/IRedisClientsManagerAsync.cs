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
using ServiceStack.Caching;

namespace ServiceStack.Redis
{
    public interface IRedisClientsManagerAsync : IAsyncDisposable
    {
        /// <summary>
        /// Returns a Read/Write client (The default) using the hosts defined in ReadWriteHosts
        /// </summary>
        /// <returns></returns>
        ValueTask<IRedisClientAsync> GetClientAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a ReadOnly client using the hosts defined in ReadOnlyHosts.
        /// </summary>
        /// <returns></returns>
        ValueTask<IRedisClientAsync> GetReadOnlyClientAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a Read/Write ICacheClient (The default) using the hosts defined in ReadWriteHosts
        /// </summary>
        /// <returns></returns>
        ValueTask<ICacheClientAsync> GetCacheClientAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a ReadOnly ICacheClient using the hosts defined in ReadOnlyHosts.
        /// </summary>
        /// <returns></returns>
        ValueTask<ICacheClientAsync> GetReadOnlyCacheClientAsync(CancellationToken cancellationToken = default);
    }
}