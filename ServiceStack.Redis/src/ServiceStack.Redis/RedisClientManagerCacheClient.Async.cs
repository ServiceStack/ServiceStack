using ServiceStack.Caching;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis
{
    partial class RedisClientManagerCacheClient : ICacheClientAsync, IRemoveByPatternAsync, IAsyncDisposable
    {
        ValueTask IAsyncDisposable.DisposeAsync()
        {
            Dispose();
            return default;
        }

        private ValueTask<IRedisClientAsync> GetClientAsync(in CancellationToken token)
        {
            AssertNotReadOnly();
            return redisManager.GetClientAsync(token);
        }

        async Task<T> ICacheClientAsync.GetAsync<T>(string key, CancellationToken token)
        {
            await using var client = await redisManager.GetReadOnlyClientAsync(token).ConfigureAwait(false);
            return await client.GetAsync<T>(key, token).ConfigureAwait(false);
        }

        async Task<bool> ICacheClientAsync.SetAsync<T>(string key, T value, CancellationToken token)
        {
            await using var client = await GetClientAsync(token).ConfigureAwait(false);
            return await client.SetAsync(key, value, token).ConfigureAwait(false);
        }

        async Task<bool> ICacheClientAsync.SetAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token)
        {
            await using var client = await GetClientAsync(token).ConfigureAwait(false);
            return await client.SetAsync(key, value, expiresAt, token).ConfigureAwait(false);
        }

        async Task<bool> ICacheClientAsync.SetAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token)
        {
            await using var client = await GetClientAsync(token).ConfigureAwait(false);
            return await client.SetAsync(key, value, expiresIn, token).ConfigureAwait(false);
        }

        async Task ICacheClientAsync.FlushAllAsync(CancellationToken token)
        {
            await using var client = await GetClientAsync(token).ConfigureAwait(false);
            await client.FlushAllAsync(token).ConfigureAwait(false);
        }

        async Task<IDictionary<string, T>> ICacheClientAsync.GetAllAsync<T>(IEnumerable<string> keys, CancellationToken token)
        {
            await using var client = await redisManager.GetReadOnlyClientAsync(token).ConfigureAwait(false);
            return await client.GetAllAsync<T>(keys, token).ConfigureAwait(false);
        }

        async Task ICacheClientAsync.SetAllAsync<T>(IDictionary<string, T> values, CancellationToken token)
        {
            await using var client = await GetClientAsync(token).ConfigureAwait(false);
            await client.SetAllAsync(values, token).ConfigureAwait(false);
        }

        async Task<bool> ICacheClientAsync.RemoveAsync(string key, CancellationToken token)
        {
            await using var client = await GetClientAsync(token).ConfigureAwait(false);
            return await client.RemoveAsync(key, token).ConfigureAwait(false);
        }

        async Task<TimeSpan?> ICacheClientAsync.GetTimeToLiveAsync(string key, CancellationToken token)
        {
            await using var client = await redisManager.GetReadOnlyClientAsync(token).ConfigureAwait(false);
            return await client.GetTimeToLiveAsync(key, token).ConfigureAwait(false);
        }

        async IAsyncEnumerable<string> ICacheClientAsync.GetKeysByPatternAsync(string pattern, [EnumeratorCancellation] CancellationToken token)
        {
            await using var client = await redisManager.GetReadOnlyClientAsync(token).ConfigureAwait(false);
            await foreach (var key in client.GetKeysByPatternAsync(pattern, token).ConfigureAwait(false).WithCancellation(token))
            {
                yield return key;
            }
        }

        Task ICacheClientAsync.RemoveExpiredEntriesAsync(CancellationToken token)
        {
            //Redis automatically removed expired Cache Entries
            return Task.CompletedTask;
        }

        async Task IRemoveByPatternAsync.RemoveByPatternAsync(string pattern, CancellationToken token)
        {
            await using var client = await GetClientAsync(token).ConfigureAwait(false);
            if (client is IRemoveByPatternAsync redisClient)
            {
                await redisClient.RemoveByPatternAsync(pattern, token).ConfigureAwait(false);
            }
        }

        async Task IRemoveByPatternAsync.RemoveByRegexAsync(string regex, CancellationToken token)
        {
            await using var client = await GetClientAsync(token).ConfigureAwait(false);
            if (client is IRemoveByPatternAsync redisClient)
            {
                await redisClient.RemoveByRegexAsync(regex, token).ConfigureAwait(false);
            }
        }

        async Task ICacheClientAsync.RemoveAllAsync(IEnumerable<string> keys, CancellationToken token)
        {
            await using var client = await GetClientAsync(token).ConfigureAwait(false);
            await client.RemoveAllAsync(keys, token).ConfigureAwait(false);
        }

        async Task<long> ICacheClientAsync.IncrementAsync(string key, uint amount, CancellationToken token)
        {
            await using var client = await GetClientAsync(token).ConfigureAwait(false);
            return await client.IncrementAsync(key, amount, token).ConfigureAwait(false);
        }

        async Task<long> ICacheClientAsync.DecrementAsync(string key, uint amount, CancellationToken token)
        {
            await using var client = await GetClientAsync(token).ConfigureAwait(false);
            return await client.DecrementAsync(key, amount, token).ConfigureAwait(false);
        }

        async Task<bool> ICacheClientAsync.AddAsync<T>(string key, T value, CancellationToken token)
        {
            await using var client = await GetClientAsync(token).ConfigureAwait(false);
            return await client.AddAsync(key, value, token).ConfigureAwait(false);
        }

        async Task<bool> ICacheClientAsync.ReplaceAsync<T>(string key, T value, CancellationToken token)
        {
            await using var client = await GetClientAsync(token).ConfigureAwait(false);
            return await client.ReplaceAsync(key, value, token).ConfigureAwait(false);
        }

        async Task<bool> ICacheClientAsync.AddAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token)
        {
            await using var client = await GetClientAsync(token).ConfigureAwait(false);
            return await client.AddAsync(key, value, expiresAt, token).ConfigureAwait(false);
        }

        async Task<bool> ICacheClientAsync.ReplaceAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token)
        {
            await using var client = await GetClientAsync(token).ConfigureAwait(false);
            return await client.ReplaceAsync(key, value, expiresAt, token).ConfigureAwait(false);
        }

        async Task<bool> ICacheClientAsync.AddAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token)
        {
            await using var client = await GetClientAsync(token).ConfigureAwait(false);
            return await client.AddAsync(key, value, expiresIn, token).ConfigureAwait(false);
        }

        async Task<bool> ICacheClientAsync.ReplaceAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token)
        {
            await using var client = await GetClientAsync(token).ConfigureAwait(false);
            return await client.ReplaceAsync(key, value, expiresIn, token).ConfigureAwait(false);
        }
    }
}