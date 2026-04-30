using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;
using ServiceStack.Caching;
using ServiceStack.Support;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using Azure.Data.Tables;
using Azure;

namespace ServiceStack.Azure.Storage;

public partial class AzureTableCacheClient
    : AdapterBase, ICacheClientAsync, IRemoveByPatternAsync, IAsyncDisposable
{
    private async Task<TableCacheEntry?> TryGetValueAsync(string key, CancellationToken token = default)
    {
        try
        {
            var response = await tableClient!.GetEntityAsync<TableCacheEntry>(partitionKey, key, cancellationToken: token);
            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!FlushOnDispose)
            return;

        await FlushAllAsync();
    }

    public async Task<bool> AddAsync<T>(string key, T value, CancellationToken token = default)
    {
        var sVal = serializer!.SerializeToString(value);
        var entry = CreateTableEntry(key, sVal, null);
        return await AddInternalAsync(key, entry, token);
    }

    public Task<bool> AddAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token = default)
    {
        return AddAsync(key, value, DateTime.UtcNow.Add(expiresIn), token);
    }

    public async Task<bool> AddAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token = default)
    {
        var sVal = serializer!.SerializeToString(value);
        var entry = CreateTableEntry(key, sVal, null, expiresAt);
        return await AddInternalAsync(key, entry, token);
    }

    public async Task<bool> AddInternalAsync(string key, TableCacheEntry entry, CancellationToken token = default)
    {
        try
        {
            await tableClient!.AddEntityAsync(entry, token);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.Conflict)
        {
            return false;
        }
    }

    public Task<long> DecrementAsync(string key, uint amount, CancellationToken token = default)
    {
        return AtomicIncDecAsync(key, amount * -1, token);
    }

    internal async Task<long> AtomicIncDecAsync(string key, long amount, CancellationToken token = default)
    {
        long count = 0;
        bool updated = false;

        await ExecUtils.RetryUntilTrueAsync(async () =>
        {
            var entry = await GetEntryAsync(key, token);

            if (entry == null)
            {
                count = amount;
                entry = CreateTableEntry(key, Serialize(count));
                try
                {
                    await tableClient!.AddEntityAsync(entry, token);
                    updated = true;
                }
                catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.Conflict)
                {
                    // concurrent insert, retry
                }
            }
            else
            {
                count = Deserialize<long>(entry.Data) + amount;
                entry.Data = Serialize(count);
                try
                {
                    await tableClient!.UpdateEntityAsync(entry, entry.ETag, TableUpdateMode.Replace, token);
                    updated = true;
                }
                catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.PreconditionFailed)
                {
                    // concurrent modification, retry
                }
            }

            return updated;
        }, TimeSpan.FromSeconds(30));

        return count;
    }

    public async Task FlushAllAsync(CancellationToken token = default)
    {
        await foreach (var key in GetKeysByPatternAsync("*", token))
        {
            await RemoveAsync(key, token);
        }
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken token = default)
    {
        var entry = await GetEntryAsync(key, token);
        if (entry != null)
            return Deserialize<T>(entry.Data);
        return default;
    }

    private async Task<TableCacheEntry?> GetEntryAsync(string key, CancellationToken token = default)
    {
        var entry = await TryGetValueAsync(key, token);
        if (entry != null)
        {
            if (entry.HasExpired)
            {
                await this.RemoveAsync(key, token);
                return null;
            }
            return entry;
        }
        return null;
    }

    public async Task<IDictionary<string, T?>> GetAllAsync<T>(IEnumerable<string> keys, CancellationToken token = default)
    {
        var valueMap = new Dictionary<string, T?>();
        foreach (var key in keys)
        {
            var value = await GetAsync<T>(key, token);
            valueMap[key] = value;
        }
        return valueMap;
    }

    public Task<long> IncrementAsync(string key, uint amount, CancellationToken token = default)
    {
        return AtomicIncDecAsync(key, amount, token);
    }

    public async Task<bool> RemoveAsync(string key, CancellationToken token = default)
    {
        if (await TryGetValueAsync(key, token) == null)
            return false;

        try
        {
            await tableClient!.DeleteEntityAsync(partitionKey, key, ETag.All, token);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task RemoveAllAsync(IEnumerable<string> keys, CancellationToken token = default)
    {
        foreach (var key in keys)
        {
            await RemoveAsync(key, token);
        }
    }

    public Task<bool> ReplaceAsync<T>(string key, T value, CancellationToken token = default)
    {
        return ReplaceInternalAsync(key, Serialize(value), null, token);
    }

    public Task<bool> ReplaceAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token = default)
    {
        return ReplaceInternalAsync(key, Serialize(value), DateTime.UtcNow.Add(expiresIn), token);
    }

    public Task<bool> ReplaceAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token = default)
    {
        return ReplaceInternalAsync(key, Serialize(value), expiresAt, token);
    }

    private async Task<bool> ReplaceInternalAsync(string key, string value, DateTime? expiresAt = null, CancellationToken token = default)
    {
        var existing = await TryGetValueAsync(key, token);
        if (existing == null)
            return false;

        var entry = CreateTableEntry(key, value, null, expiresAt);
        await tableClient!.UpdateEntityAsync(entry, ETag.All, TableUpdateMode.Replace, token);
        return true;
    }

    public Task<bool> SetAsync<T>(string key, T value, CancellationToken token = default)
    {
        var sVal = Serialize(value);
        var entry = CreateTableEntry(key, sVal);
        return SetInternalAsync(entry, token);
    }

    public Task<bool> SetAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token = default)
    {
        return SetAsync(key, value, DateTime.UtcNow.Add(expiresIn), token);
    }

    public Task<bool> SetAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token = default)
    {
        var sVal = Serialize(value);
        var entry = CreateTableEntry(key, sVal, null, expiresAt);
        return SetInternalAsync(entry, token);
    }

    private async Task<bool> SetInternalAsync(TableCacheEntry entry, CancellationToken token = default)
    {
        await tableClient!.UpsertEntityAsync(entry, TableUpdateMode.Replace, token);
        return true;
    }

    public async Task SetAllAsync<T>(IDictionary<string, T> values, CancellationToken token = default)
    {
        foreach (var key in values.Keys)
        {
            await SetAsync(key, values[key], token);
        }
    }

    public async Task<TimeSpan?> GetTimeToLiveAsync(string key, CancellationToken token = default)
    {
        var entry = await GetEntryAsync(key, token);
        if (entry != null)
        {
            if (entry.ExpiryDate == null)
                return TimeSpan.MaxValue;

            return entry.ExpiryDate - DateTime.UtcNow;
        }
        return null;
    }

#pragma warning disable CS8425
    public async IAsyncEnumerable<string> GetKeysByPatternAsync(string pattern, CancellationToken token = default)
    {
        await foreach (var entity in tableClient!.QueryAsync<TableCacheEntry>(cancellationToken: token))
        {
            if (entity.RowKey.Glob(pattern))
                yield return entity.RowKey;
        }
    }
#pragma warning restore CS8425

    public async Task RemoveExpiredEntriesAsync(CancellationToken token = default)
    {
        await foreach (var key in GetKeysByPatternAsync("*", token))
        {
            await GetEntryAsync(key, token); // removes if expired
        }
    }

#pragma warning disable CS8425
    public async IAsyncEnumerable<string> GetKeysByRegexAsync(string regex, CancellationToken token = default)
    {
        var re = new Regex(regex, RegexOptions.Compiled | RegexOptions.Singleline);
        await foreach (var entity in tableClient!.QueryAsync<TableCacheEntry>(cancellationToken: token))
        {
            if (re.IsMatch(entity.RowKey))
                yield return entity.RowKey;
        }
    }
#pragma warning restore CS8425

    public async Task RemoveByPatternAsync(string pattern, CancellationToken token = default)
    {
        await RemoveAllAsync(await GetKeysByPatternAsync(pattern, token).ToListAsync(token), token);
    }

    public async Task RemoveByRegexAsync(string regex, CancellationToken token = default)
    {
        await RemoveAllAsync(await GetKeysByRegexAsync(regex, token).ToListAsync(token), token);
    }
}
