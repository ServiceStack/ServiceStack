// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using ServiceStack.Aws.Support;
using ServiceStack.Caching;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDb;

public partial class DynamoDbCacheClient : ICacheClientAsync, IRemoveByPatternAsync
{
    public async Task InitSchemaAsync(CancellationToken token=default)
    {
        schema = Dynamo.GetTableSchema<CacheEntry>();

        if (schema == null)
        {
            await Dynamo.CreateTableIfMissingAsync(metadata, token).ConfigAwait();
            schema = Dynamo.GetTableSchema<CacheEntry>();
        }
    }

    private async Task<T> GetValueAsync<T>(string key, CancellationToken token=default)
    {
        var entry = await Dynamo.GetItemAsync<CacheEntry>(key, token).ConfigAwait();
        if (entry == null)
            return default;

        if (entry.ExpiryDate != null && DateTime.UtcNow > entry.ExpiryDate.Value.ToUniversalTime())
        {
            await RemoveAsync(key, token).ConfigAwait();
            return default;
        }

        return entry.Data.FromJson<T>();
    }

    public async Task RemoveExpiredEntriesAsync(CancellationToken token=default)
    {
        var expiredIds = await Dynamo.FromScan<CacheEntry>()
            .Filter(x => x.ExpiryDate < DateTime.UtcNow)
            .ExecColumnAsync(x => x.Id, token).ConfigAwait();

        await Dynamo.DeleteItemsAsync<CacheEntry>(expiredIds, token).ConfigAwait();
    }

    private async Task<bool> CacheAddAsync<T>(string key, T value, DateTime? expiresAt, CancellationToken token=default)
    {
        var entry = await GetValueAsync<T>(key, token).ConfigAwait();
        if (!Equals(entry, default(T)))
            return false;

        await CacheSetAsync(key, value, expiresAt, token).ConfigAwait();
        return true;
    }

    private async Task<bool> CacheReplaceAsync<T>(string key, T value, DateTime? expiresAt, CancellationToken token=default)
    {
        var entry = await GetValueAsync<T>(key, token).ConfigAwait();
        if (Equals(entry, default(T)))
            return false;

        await CacheSetAsync(key, value, expiresAt, token).ConfigAwait();
        return true;
    }

    private async Task<bool> CacheSetAsync<T>(string key, T value, DateTime? expiresAt, CancellationToken token=default)
    {
        var request = ToCacheEntryPutItemRequest(key, value, expiresAt);

        Exception lastEx = null;
        var i = 0;
        var firstAttempt = DateTime.UtcNow;
        while (DateTime.UtcNow - firstAttempt < Dynamo.MaxRetryOnExceptionTimeout)
        {
            i++;
            try
            {
                await ((PocoDynamo)Dynamo).ExecAsync(async () => 
                    await Dynamo.DynamoDb.PutItemAsync(request, token).ConfigAwait()).ConfigAwait();
                return true;
            }
            catch (ResourceNotFoundException ex)
            {
                lastEx = ex;
                await i.SleepBackOffMultiplierAsync(token).ConfigAwait(); //Table could temporarily not exist after a FlushAll()
            }
        }

        throw new TimeoutException($"Exceeded timeout of {Dynamo.MaxRetryOnExceptionTimeout}", lastEx);
    }

    private async Task<int>  UpdateCounterByAsync(string key, int amount, CancellationToken token=default)
    {
        return (int) await Dynamo.IncrementAsync<CacheEntry>(key, DataField, amount, token).ConfigAwait();
    }

    public async Task<bool> AddAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token=default)
    {
        return await CacheAddAsync(key, value, DateTime.UtcNow.Add(expiresIn), token).ConfigAwait();
    }

    public async Task<bool> AddAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token=default)
    {
        return await CacheAddAsync(key, value, expiresAt.ToUniversalTime(), token).ConfigAwait();
    }

    public async Task<bool> AddAsync<T>(string key, T value, CancellationToken token=default)
    {
        return await CacheAddAsync(key, value, null, token).ConfigAwait();
    }

    public async Task<long> DecrementAsync(string key, uint amount, CancellationToken token=default)
    {
        return await UpdateCounterByAsync(key, (int)-amount, token).ConfigAwait();
    }

    /// <summary>
    /// IMPORTANT: This method will delete and re-create the DynamoDB table in order to reduce read/write capacity costs, make sure the proper table name and throughput properties are set!
    /// TODO: This method may take upwards of a minute to complete, need to look into a faster implementation
    /// </summary>
    public async Task FlushAllAsync(CancellationToken token=default)
    {
        await Dynamo.DeleteTableAsync<CacheEntry>(token: token).ConfigAwait();
        await Dynamo.CreateTableIfMissingAsync<CacheEntry>(token: token).ConfigAwait();
    }

    public async Task<T> GetAsync<T>(string key, CancellationToken token=default)
    {
        return await GetValueAsync<T>(key, token).ConfigAwait();
    }

    public async Task<IDictionary<string, T>> GetAllAsync<T>(IEnumerable<string> keys, CancellationToken token=default)
    {
        var valueMap = new Dictionary<string, T>();
        foreach (var key in keys)
        {
            var value = await GetAsync<T>(key, token).ConfigAwait();
            valueMap[key] = value;
        }
        return valueMap;
    }

    public async Task<long> IncrementAsync(string key, uint amount, CancellationToken token=default)
    {
        return await UpdateCounterByAsync(key, (int)amount, token).ConfigAwait();
    }

    public async Task<bool> RemoveAsync(string key, CancellationToken token=default)
    {
        var existingItem = await Dynamo.DeleteItemAsync<CacheEntry>(key, ReturnItem.Old, token).ConfigAwait();
        return existingItem != null;
    }

    public async Task RemoveAllAsync(IEnumerable<string> keys, CancellationToken token=default)
    {
        foreach (var key in keys)
        {
            await RemoveAsync(key, token).ConfigAwait();
        }
    }

    public async Task<bool> ReplaceAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token=default)
    {
        return await CacheReplaceAsync(key, value, DateTime.UtcNow.Add(expiresIn), token).ConfigAwait();
    }

    public async Task<bool> ReplaceAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token=default)
    {
        return await CacheReplaceAsync(key, value, expiresAt.ToUniversalTime(), token).ConfigAwait();
    }

    public async Task<bool> ReplaceAsync<T>(string key, T value, CancellationToken token=default)
    {
        return await CacheReplaceAsync(key, value, null, token).ConfigAwait();
    }

    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token=default)
    {
        return await CacheSetAsync(key, value, DateTime.UtcNow.Add(expiresIn), token).ConfigAwait();
    }

    public async Task<bool> SetAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token=default)
    {
        return await CacheSetAsync(key, value, expiresAt.ToUniversalTime(), token).ConfigAwait();
    }

    public async Task<bool> SetAsync<T>(string key, T value, CancellationToken token=default)
    {
        return await CacheSetAsync(key, value, null, token).ConfigAwait();
    }

    public async Task SetAllAsync<T>(IDictionary<string, T> values, CancellationToken token=default)
    {
        foreach (var entry in values)
        {
            await SetAsync(entry.Key, entry.Value, token).ConfigAwait();
        }
    }

    public async Task<TimeSpan?> GetTimeToLiveAsync(string key, CancellationToken token=default)
    {
        var entry = await Dynamo.GetItemAsync<CacheEntry>(key, token).ConfigAwait();
        if (entry == null)
            return null;

        if (entry.ExpiryDate == null)
            return TimeSpan.MaxValue;

        return entry.ExpiryDate - DateTime.UtcNow;
    }

    public async Task<IEnumerable<string>> KeysByPatternAsync(string pattern, CancellationToken token=default)
    {
        if (pattern == "*")
            return await Dynamo.FromScan<CacheEntry>().ExecColumnAsync(x => x.Id, token).ConfigAwait();

        if (!pattern.EndsWith("*"))
            throw new NotImplementedException("DynamoDb only supports begins_with* patterns");

        var beginWith = pattern.Substring(0, pattern.Length - 1);
        if (beginWith.Contains("*"))
            throw new NotImplementedException("DynamoDb only supports begins_with* patterns");

        return await Dynamo.FromScan<CacheEntry>(x => x.Id.StartsWith(beginWith))
            .ExecColumnAsync(x => x.Id, token).ConfigAwait();
    }
        
    public async Task RemoveByPatternAsync(string pattern, CancellationToken token=default)
    {
        var idsToRemove = await KeysByPatternAsync(pattern, token).ConfigAwait();
        await RemoveAllAsync(idsToRemove, token).ConfigAwait();
    }

    public Task RemoveByRegexAsync(string regex, CancellationToken token=default)
    {
        throw new NotImplementedException();
    }

    public async IAsyncEnumerable<string> GetKeysByPatternAsync(string pattern, [EnumeratorCancellation] CancellationToken token=default)
    {
        if (pattern == "*")
        {
            foreach (var item in await Dynamo.FromScan<CacheEntry>().ExecColumnAsync(x => x.Id, token).ConfigAwait())
            {
                token.ThrowIfCancellationRequested();
                yield return item;
            }
        }
        else
        {
            if (!pattern.EndsWith("*"))
                throw new NotImplementedException("DynamoDb only supports begins_with* patterns");

            var beginWith = pattern.Substring(0, pattern.Length - 1);
            if (beginWith.IndexOf('*') >= 0)
                throw new NotImplementedException("DynamoDb only supports begins_with* patterns");

            foreach (var item in await Dynamo.FromScan<CacheEntry>(x => x.Id.StartsWith(beginWith))
                         .ExecColumnAsync(x => x.Id, token).ConfigAwait())
            {
                token.ThrowIfCancellationRequested();
                yield return item;
            }
        }
    }

    public ValueTask DisposeAsync() => default;
}