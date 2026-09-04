using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Caching;

public class MultiCacheClient
    : ICacheClient, ICacheClientAsync
{
    private readonly List<ICacheClient> cacheClients;
    private readonly List<ICacheClientAsync> cacheClientsAsync;

    public MultiCacheClient(params ICacheClient[] cacheClients)
    {
        if (cacheClients == null || cacheClients.Length == 0)
            throw new ArgumentNullException(nameof(cacheClients));

        this.cacheClients = new List<ICacheClient>(cacheClients);
        this.cacheClientsAsync = cacheClients.Where(x => x != null).Select(x => x.AsAsync()).ToList();
    }

    public MultiCacheClient(List<ICacheClient> cacheClients, List<ICacheClientAsync> cacheClientsAsync)
    {
        this.cacheClients = cacheClients ?? new List<ICacheClient>();
        this.cacheClientsAsync = cacheClientsAsync ?? this.cacheClients.Where(x => x != null).Select(x => x.AsAsync()).ToList();
    }

    public void Dispose()
    {
        cacheClients.ExecAll(client => client.Dispose());
    }

    public bool Remove(string key)
    {
        var firstResult = default(bool);
        cacheClients.ExecAllWithFirstOut(client => client.Remove(key), ref firstResult);
        return firstResult;
    }

    public T Get<T>(string key)
    {
        return cacheClients.ExecReturnFirstWithResult(client => client.Get<T>(key));
    }

    public long Increment(string key, uint amount)
    {
        var firstResult = default(long);
        cacheClients.ExecAllWithFirstOut(client => client.Increment(key, amount), ref firstResult);
        return firstResult;
    }

    public long Decrement(string key, uint amount)
    {
        var firstResult = default(long);
        cacheClients.ExecAllWithFirstOut(client => client.Decrement(key, amount), ref firstResult);
        return firstResult;
    }

    public bool Add<T>(string key, T value)
    {
        var firstResult = default(bool);
        cacheClients.ExecAllWithFirstOut(client => client.Add(key, value), ref firstResult);
        return firstResult;
    }

    public bool Set<T>(string key, T value)
    {
        var firstResult = default(bool);
        cacheClients.ExecAllWithFirstOut(client => client.Set(key, value), ref firstResult);
        return firstResult;
    }

    public bool Replace<T>(string key, T value)
    {
        var firstResult = default(bool);
        cacheClients.ExecAllWithFirstOut(client => client.Replace(key, value), ref firstResult);
        return firstResult;
    }

    public bool Add<T>(string key, T value, DateTime expiresAt)
    {
        var firstResult = default(bool);
        cacheClients.ExecAllWithFirstOut(client => client.Add(key, value, expiresAt), ref firstResult);
        return firstResult;
    }

    public bool Set<T>(string key, T value, DateTime expiresAt)
    {
        var firstResult = default(bool);
        cacheClients.ExecAllWithFirstOut(client => client.Set(key, value, expiresAt), ref firstResult);
        return firstResult;
    }

    public bool Replace<T>(string key, T value, DateTime expiresAt)
    {
        var firstResult = default(bool);
        cacheClients.ExecAllWithFirstOut(client => client.Replace(key, value, expiresAt), ref firstResult);
        return firstResult;
    }

    public bool Add<T>(string key, T value, TimeSpan expiresIn)
    {
        var firstResult = default(bool);
        cacheClients.ExecAllWithFirstOut(client => client.Add(key, value, expiresIn), ref firstResult);
        return firstResult;
    }

    public bool Set<T>(string key, T value, TimeSpan expiresIn)
    {
        var firstResult = default(bool);
        cacheClients.ExecAllWithFirstOut(client => client.Set(key, value, expiresIn), ref firstResult);
        return firstResult;
    }

    public bool Replace<T>(string key, T value, TimeSpan expiresIn)
    {
        var firstResult = default(bool);
        cacheClients.ExecAllWithFirstOut(client => client.Replace(key, value, expiresIn), ref firstResult);
        return firstResult;
    }

    public void FlushAll()
    {
        cacheClients.ExecAll(client => client.FlushAll());
    }

    public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys)
    {
        if (keys == null) return new Dictionary<string, T>();
        foreach (var client in cacheClients)
        {
            try
            {
                var result = client.GetAll<T>(keys);
                if (result != null)
                {
                    return result;
                }
            }
            catch (Exception ex)
            {
                ExecUtils.LogError(client.GetType(), "Get", ex);
            }
        }

        return new Dictionary<string, T>();
    }

    public void RemoveAll(IEnumerable<string> keys)
    {
        if (keys == null) return;
        cacheClients.ExecAll(client => client.RemoveAll(keys));
    }

    public void SetAll<T>(IDictionary<string, T> values)
    {
        if (values == null) return;
        cacheClients.ExecAll(client => client.SetAll(values));
    }

    public async Task<bool> RemoveAsync(string key, CancellationToken token = default)
    {
        return await cacheClientsAsync.ExecAllReturnFirstAsync(cache => cache.RemoveAsync(key, token)).ConfigAwait();
    }

    public async Task RemoveAllAsync(IEnumerable<string> keys, CancellationToken token = default)
    {
        if (keys == null) return;
        await cacheClientsAsync.ExecAllAsync(cache => cache.RemoveAllAsync(keys, token)).ConfigAwait();
    }

    public async Task<T> GetAsync<T>(string key, CancellationToken token = default)
    {
        return await cacheClientsAsync.ExecReturnFirstWithResultAsync(client => client.GetAsync<T>(key, token)).ConfigAwait();
    }

    public async Task<long> IncrementAsync(string key, uint amount, CancellationToken token = default)
    {
        return await cacheClientsAsync.ExecAllReturnFirstAsync(client => client.IncrementAsync(key, amount, token)).ConfigAwait();
    }

    public async Task<long> DecrementAsync(string key, uint amount, CancellationToken token = default)
    {
        return await cacheClientsAsync.ExecAllReturnFirstAsync(client => client.DecrementAsync(key, amount, token)).ConfigAwait();
    }

    public async Task<bool> AddAsync<T>(string key, T value, CancellationToken token = default)
    {
        return await cacheClientsAsync.ExecAllReturnFirstAsync(client => client.AddAsync(key, value, token)).ConfigAwait();
    }

    public async Task<bool> SetAsync<T>(string key, T value, CancellationToken token = default)
    {
        return await cacheClientsAsync.ExecAllReturnFirstAsync(client => client.SetAsync(key, value, token)).ConfigAwait();
    }

    public async Task<bool> ReplaceAsync<T>(string key, T value, CancellationToken token = default)
    {
        return await cacheClientsAsync.ExecAllReturnFirstAsync(client => client.ReplaceAsync(key, value, token)).ConfigAwait();
    }

    public async Task<bool> AddAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token = default)
    {
        return await cacheClientsAsync.ExecAllReturnFirstAsync(client => client.AddAsync(key, value, expiresAt, token)).ConfigAwait();
    }

    public async Task<bool> SetAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token = default)
    {
        return await cacheClientsAsync.ExecAllReturnFirstAsync(client => client.SetAsync(key, value, expiresAt, token)).ConfigAwait();
    }

    public async Task<bool> ReplaceAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token = default)
    {
        return await cacheClientsAsync.ExecAllReturnFirstAsync(client => client.ReplaceAsync(key, value, expiresAt, token)).ConfigAwait();
    }

    public async Task<bool> AddAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token = default)
    {
        return await cacheClientsAsync.ExecAllReturnFirstAsync(client => client.AddAsync(key, value, expiresIn, token)).ConfigAwait();
    }

    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token = default)
    {
        return await cacheClientsAsync.ExecAllReturnFirstAsync(client => client.SetAsync(key, value, expiresIn, token)).ConfigAwait();
    }

    public async Task<bool> ReplaceAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token = default)
    {
        return await cacheClientsAsync.ExecAllReturnFirstAsync(client => client.ReplaceAsync(key, value, expiresIn, token)).ConfigAwait();
    }

    public async Task FlushAllAsync(CancellationToken token = default)
    {
        await cacheClientsAsync.ExecAllAsync(client => client.FlushAllAsync(token)).ConfigAwait();
    }

    public async Task<IDictionary<string, T>> GetAllAsync<T>(IEnumerable<string> keys, CancellationToken token = default)
    {
        if (keys == null) return new Dictionary<string, T>();
        var result = await cacheClientsAsync.ExecReturnFirstWithResultAsync(client => client.GetAllAsync<T>(keys, token)).ConfigAwait();
        return result ?? new Dictionary<string, T>();
    }

    public async Task SetAllAsync<T>(IDictionary<string, T> values, CancellationToken token = default)
    {
        if (values == null) return;
        await cacheClientsAsync.ExecAllAsync(client => client.SetAllAsync(values, token)).ConfigAwait();
    }

    public async Task<TimeSpan?> GetTimeToLiveAsync(string key, CancellationToken token = default)
    {
        return await cacheClientsAsync.ExecReturnFirstWithResultAsync(client => client.GetTimeToLiveAsync(key, token)).ConfigAwait();
    }

    public async Task RemoveExpiredEntriesAsync(CancellationToken token = default)
    {
        await cacheClientsAsync.ExecAllAsync(client => client.RemoveExpiredEntriesAsync(token)).ConfigAwait();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var cache in cacheClientsAsync)
        {
            try
            {
                await cache.DisposeAsync();
            }
            catch { }
        }
    }

    public async IAsyncEnumerable<string> GetKeysByPatternAsync(string pattern, [EnumeratorCancellation] CancellationToken token = default)
    {
        var results = cacheClientsAsync.ExecReturnFirstWithResult(client => 
            client.GetKeysByPatternAsync(pattern, token));
        if (results != null)
        {
            await foreach (var key in results.WithCancellation(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                yield return key;
            }
        }
    }
}