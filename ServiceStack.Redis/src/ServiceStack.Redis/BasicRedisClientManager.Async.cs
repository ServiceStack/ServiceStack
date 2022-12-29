//
// https://github.com/ServiceStack/ServiceStack.Redis
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2013 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using ServiceStack.Caching;
using ServiceStack.Redis.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis
{
    /// <summary>
    /// Provides thread-safe retrieval of redis clients since each client is a new one.
    /// Allows the configuration of different ReadWrite and ReadOnly hosts
    /// </summary>
    public partial class BasicRedisClientManager
        : IRedisClientsManagerAsync, ICacheClientAsync
    {
        private ValueTask<ICacheClientAsync> GetCacheClientAsync(in CancellationToken _)
            => new RedisClientManagerCacheClient(this).AsValueTaskResult<ICacheClientAsync>();

        private ValueTask<ICacheClientAsync> GetReadOnlyCacheClientAsync(in CancellationToken _)
            => ConfigureRedisClientAsync(this.GetReadOnlyClientImpl()).AsValueTaskResult<ICacheClientAsync>();

        private IRedisClientAsync ConfigureRedisClientAsync(IRedisClientAsync client)
            => client;

        ValueTask<ICacheClientAsync> IRedisClientsManagerAsync.GetCacheClientAsync(CancellationToken token)
            => GetCacheClientAsync(token);

        ValueTask<IRedisClientAsync> IRedisClientsManagerAsync.GetClientAsync(CancellationToken token)
            => GetClientImpl().AsValueTaskResult<IRedisClientAsync>();

        ValueTask<ICacheClientAsync> IRedisClientsManagerAsync.GetReadOnlyCacheClientAsync(CancellationToken token)
            => GetReadOnlyCacheClientAsync(token);

        ValueTask<IRedisClientAsync> IRedisClientsManagerAsync.GetReadOnlyClientAsync(CancellationToken token)
            => GetReadOnlyClientImpl().AsValueTaskResult<IRedisClientAsync>();

        ValueTask IAsyncDisposable.DisposeAsync()
        {
            Dispose();
            return default;
        }

        async Task<T> ICacheClientAsync.GetAsync<T>(string key, CancellationToken token)
        {
            await using var client = await GetReadOnlyCacheClientAsync(token).ConfigureAwait(false);
            return await client.GetAsync<T>(key, token).ConfigureAwait(false);
        }

        async Task<bool> ICacheClientAsync.SetAsync<T>(string key, T value, CancellationToken token)
        {
            await using var client = await GetCacheClientAsync(token).ConfigureAwait(false);
            return await client.SetAsync<T>(key, value, token).ConfigureAwait(false);
        }

        async Task<bool> ICacheClientAsync.SetAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token)
        {
            await using var client = await GetCacheClientAsync(token).ConfigureAwait(false);
            return await client.SetAsync<T>(key, value, expiresAt, token).ConfigureAwait(false);
        }

        async Task<bool> ICacheClientAsync.SetAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token)
        {
            await using var client = await GetCacheClientAsync(token).ConfigureAwait(false);
            return await client.SetAsync<T>(key, value, expiresIn, token).ConfigureAwait(false);
        }

        async Task ICacheClientAsync.FlushAllAsync(CancellationToken token)
        {
            await using var client = await GetCacheClientAsync(token).ConfigureAwait(false);
            await client.FlushAllAsync(token).ConfigureAwait(false);
        }

        async Task<IDictionary<string, T>> ICacheClientAsync.GetAllAsync<T>(IEnumerable<string> keys, CancellationToken token)
        {
            await using var client = await GetReadOnlyCacheClientAsync(token).ConfigureAwait(false);
            return await client.GetAllAsync<T>(keys, token).ConfigureAwait(false);
        }

        async Task ICacheClientAsync.SetAllAsync<T>(IDictionary<string, T> values, CancellationToken token)
        {
            await using var client = await GetCacheClientAsync(token).ConfigureAwait(false);
            await client.SetAllAsync<T>(values, token).ConfigureAwait(false);
        }

        async Task<bool> ICacheClientAsync.RemoveAsync(string key, CancellationToken token)
        {
            await using var client = await GetCacheClientAsync(token).ConfigureAwait(false);
            return await client.RemoveAsync(key, token).ConfigureAwait(false);
        }

        async Task ICacheClientAsync.RemoveAllAsync(IEnumerable<string> keys, CancellationToken token)
        {
            await using var client = await GetCacheClientAsync(token).ConfigureAwait(false);
            await client.RemoveAllAsync(keys, token).ConfigureAwait(false);
        }

        async Task<long> ICacheClientAsync.IncrementAsync(string key, uint amount, CancellationToken token)
        {
            await using var client = await GetCacheClientAsync(token).ConfigureAwait(false);
            return await client.IncrementAsync(key, amount, token).ConfigureAwait(false);
        }

        async Task<long> ICacheClientAsync.DecrementAsync(string key, uint amount, CancellationToken token)
        {
            await using var client = await GetCacheClientAsync(token).ConfigureAwait(false);
            return await client.DecrementAsync(key, amount, token).ConfigureAwait(false);
        }

        async Task<bool> ICacheClientAsync.AddAsync<T>(string key, T value, CancellationToken token)
        {
            await using var client = await GetCacheClientAsync(token).ConfigureAwait(false);
            return await client.AddAsync<T>(key, value, token).ConfigureAwait(false);
        }

        async Task<bool> ICacheClientAsync.ReplaceAsync<T>(string key, T value, CancellationToken token)
        {
            await using var client = await GetCacheClientAsync(token).ConfigureAwait(false);
            return await client.ReplaceAsync<T>(key, value, token).ConfigureAwait(false);
        }

        async Task<bool> ICacheClientAsync.AddAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token)
        {
            await using var client = await GetCacheClientAsync(token).ConfigureAwait(false);
            return await client.AddAsync<T>(key, value, expiresAt, token).ConfigureAwait(false);
        }

        async Task<bool> ICacheClientAsync.ReplaceAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token)
        {
            await using var client = await GetCacheClientAsync(token).ConfigureAwait(false);
            return await client.ReplaceAsync<T>(key, value, expiresAt, token).ConfigureAwait(false);
        }

        async Task<bool> ICacheClientAsync.AddAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token)
        {
            await using var client = await GetCacheClientAsync(token).ConfigureAwait(false);
            return await client.AddAsync<T>(key, value, expiresIn, token).ConfigureAwait(false);
        }

        async Task<bool> ICacheClientAsync.ReplaceAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token)
        {
            await using var client = await GetCacheClientAsync(token).ConfigureAwait(false);
            return await client.ReplaceAsync<T>(key, value, expiresIn, token).ConfigureAwait(false);
        }

        async Task<TimeSpan?> ICacheClientAsync.GetTimeToLiveAsync(string key, CancellationToken token)
        {
            await using var client = await GetReadOnlyCacheClientAsync(token).ConfigureAwait(false);
            return await client.GetTimeToLiveAsync(key, token).ConfigureAwait(false);
        }

        async IAsyncEnumerable<string> ICacheClientAsync.GetKeysByPatternAsync(string pattern, [EnumeratorCancellation] CancellationToken token)
        {
            await using var client = await GetReadOnlyCacheClientAsync(token).ConfigureAwait(false);
            await foreach (var key in client.GetKeysByPatternAsync(pattern, token).ConfigureAwait(false).WithCancellation(token))
            {
                yield return key;
            }
        }

        async Task ICacheClientAsync.RemoveExpiredEntriesAsync(CancellationToken token)
        {
            await using var client = await GetCacheClientAsync(token).ConfigureAwait(false);
            await client.RemoveExpiredEntriesAsync(token).ConfigureAwait(false);
        }
    }
}