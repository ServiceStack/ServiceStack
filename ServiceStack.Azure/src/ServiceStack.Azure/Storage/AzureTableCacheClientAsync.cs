using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.Caching;
using ServiceStack.Logging;
using Microsoft.WindowsAzure.Storage;
using ServiceStack.Support;
using ServiceStack.Text;
using Microsoft.WindowsAzure.Storage.Table;
using ServiceStack.DataAnnotations;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace ServiceStack.Azure.Storage
{
    public partial class AzureTableCacheClient 
        : AdapterBase, ICacheClientAsync, IRemoveByPatternAsync, IAsyncDisposable
    {
        private async Task<TableCacheEntry> TryGetValueAsync(string key, CancellationToken token=default)
        {
            var op = TableOperation.Retrieve<TableCacheEntry>(partitionKey, key);
            var retrievedResult = await table.ExecuteAsync(op, token);
            return retrievedResult.Result as TableCacheEntry;
        }

        public async ValueTask DisposeAsync()
        {
            if (!FlushOnDispose) 
                return;

            await FlushAllAsync();
        }

        public async Task<bool> AddAsync<T>(string key, T value, CancellationToken token=default)
        {
            var sVal = serializer.SerializeToString(value);
            var entry = CreateTableEntry(key, sVal, null);
            return await AddInternalAsync(key, entry, token);
        }

        public Task<bool> AddAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token=default)
        {
            return AddAsync(key, value, DateTime.UtcNow.Add(expiresIn), token);
        }

        public async Task<bool> AddAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token=default)
        {
            var sVal = serializer.SerializeToString(value);

            var entry = CreateTableEntry(key, sVal, null, expiresAt);
            return await AddInternalAsync(key, entry, token);
        }

        public async Task<bool> AddInternalAsync(string key, TableCacheEntry entry, CancellationToken token=default)
        {
            var op = TableOperation.Insert(entry);
            var result = await table.ExecuteAsync(op, token);
            return result.HttpStatusCode == 200;
        }

        public Task<long> DecrementAsync(string key, uint amount, CancellationToken token=default)
        {
            return AtomicIncDecAsync(key, amount * -1, token);
        }

        internal async Task<long> AtomicIncDecAsync(string key, long amount, CancellationToken token=default)
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
                        updated = (await table.ExecuteAsync(TableOperation.Insert(entry), token)).HttpStatusCode == (int)HttpStatusCode.NoContent;
                    }
                    catch (StorageException ex)
                    {
                        if (!ex.HasStatus(HttpStatusCode.Conflict))
                            throw;
                    }
                }
                else
                {
                    count = Deserialize<long>(entry.Data) + amount;
                    entry.Data = Serialize(count);
                    var op = TableOperation.Replace(entry);
                    try
                    {
                        var result = (await table.ExecuteAsync(op, null, null, token)).HttpStatusCode;
                        updated = result == (int)HttpStatusCode.OK || result == (int)HttpStatusCode.NoContent;
                    }
                    catch (StorageException ex)
                    {
                        if (!ex.HasStatus(HttpStatusCode.PreconditionFailed))
                            throw;
                    }
                }

                return updated;
            }, TimeSpan.FromSeconds(30));

            return count;
        }

        public async Task FlushAllAsync(CancellationToken token=default)
        {
            await foreach (var key in GetKeysByPatternAsync("*", token))
            {
                await RemoveAsync(key, token);
            }
        }

        public async Task<T> GetAsync<T>(string key, CancellationToken token=default)
        {
            var entry = await GetEntryAsync(key, token);
            if (entry != null)
                return Deserialize<T>(entry.Data);
            return default;
        }

        private async Task<TableCacheEntry> GetEntryAsync(string key, CancellationToken token=default)
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

        public async Task<IDictionary<string, T>> GetAllAsync<T>(IEnumerable<string> keys, CancellationToken token=default)
        {
            var valueMap = new Dictionary<string, T>();
            foreach (var key in keys)
            {
                var value = await GetAsync<T>(key, token);
                valueMap[key] = value;
            }
            return valueMap;
        }

        public Task<long> IncrementAsync(string key, uint amount, CancellationToken token=default)
        {
            return AtomicIncDecAsync(key, amount, token);
        }

        public async Task<bool> RemoveAsync(string key, CancellationToken token=default)
        {
            var entry = CreateTableEntry(key);
            entry.ETag = "*";   // Avoids concurrency
            var op = TableOperation.Delete(entry);
            try
            {
                var result = await table.ExecuteAsync(op, token);
                return result.HttpStatusCode == 200 || result.HttpStatusCode == 204;
            }
            catch (Microsoft.WindowsAzure.Storage.StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == (int)System.Net.HttpStatusCode.NotFound)
                    return false;
                throw ex;
            }
        }

        public async Task RemoveAllAsync(IEnumerable<string> keys, CancellationToken token=default)
        {
            foreach (var key in keys)
            {
                await RemoveAsync(key, token);
            }
        }

        public Task<bool> ReplaceAsync<T>(string key, T value, CancellationToken token=default)
        {
            return ReplaceInternalAsync(key, Serialize(value), null, token);
        }

        public Task<bool> ReplaceAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token=default)
        {
            return ReplaceInternalAsync(key, Serialize(value), DateTime.UtcNow.Add(expiresIn), token);
        }

        public Task<bool> ReplaceAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token=default)
        {
            return ReplaceInternalAsync(key, Serialize(value), expiresAt, token);
        }

        private async Task<bool> ReplaceInternalAsync(string key, string value, DateTime? expiresAt = null, CancellationToken token=default)
        {
            if (TryGetValue(key, out var entry))
            {
                entry = CreateTableEntry(key, value, null, expiresAt);
                var op = TableOperation.Replace(entry);
                var result = await table.ExecuteAsync(op, token);
                return result.HttpStatusCode == 200;
            }
            return false;
        }

        public Task<bool> SetAsync<T>(string key, T value, CancellationToken token=default)
        {
            var sVal = Serialize(value);
            var entry = CreateTableEntry(key, sVal);
            return SetInternalAsync(entry, token);
        }

        public Task<bool> SetAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token=default)
        {
            return SetAsync(key, value, DateTime.UtcNow.Add(expiresIn), token);
        }

        public Task<bool> SetAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token=default)
        {
            var sVal = Serialize(value);
            var entry = CreateTableEntry(key, sVal, null, expiresAt);
            return SetInternalAsync(entry, token);
        }

        private async Task<bool> SetInternalAsync(TableCacheEntry entry, CancellationToken token=default)
        {
            var op = TableOperation.InsertOrReplace(entry);
            var result = await table.ExecuteAsync(op, token);
            return result.HttpStatusCode == 200 || result.HttpStatusCode == 204;    // Success or "No content"
        }

        public async Task SetAllAsync<T>(IDictionary<string, T> values, CancellationToken token=default)
        {
            foreach (var key in values.Keys)
            {
                await SetAsync(key, values[key], token);
            }
        }

        public async Task<TimeSpan?> GetTimeToLiveAsync(string key, CancellationToken token=default)
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
        public async IAsyncEnumerable<string> GetKeysByPatternAsync(string pattern, CancellationToken token=default)
        {
            // Very inefficient - query all keys and do client-side filter
            var query = new TableQuery<TableCacheEntry>();

            var keys = (await table.ExecuteQueryAsync(query, token))
                .Where(q => q.RowKey.Glob(pattern))
                .Select(q => q.RowKey);

            foreach (var key in keys)
            {
                yield return key;
            }
        }
#pragma warning restore CS8425

        public async Task RemoveExpiredEntriesAsync(CancellationToken token=default)
        {
            await foreach (var key in GetKeysByPatternAsync("*", token))
            {
                await GetEntryAsync(key, token); // removes if expired
            }
        }

#pragma warning disable CS8425
        public async IAsyncEnumerable<string> GetKeysByRegexAsync(string regex, CancellationToken token=default)
        {
            // Very inefficient - query all keys and do client-side filter
            var query = new TableQuery<TableCacheEntry>();

            var re = new Regex(regex, RegexOptions.Compiled | RegexOptions.Singleline);

            var keys = (await table.ExecuteQueryAsync(query, token))
                .Where(q => re.IsMatch(q.RowKey))
                .Select(q => q.RowKey);
            
            foreach (var key in keys)
            {
                yield return key;
            }
        }
#pragma warning restore CS8425

        public async Task RemoveByPatternAsync(string pattern, CancellationToken token=default)
        {
            await RemoveAllAsync(await GetKeysByPatternAsync(pattern, token).ToListAsync(token), token);
        }

        public async Task RemoveByRegexAsync(string regex, CancellationToken token=default)
        {
            await RemoveAllAsync(await GetKeysByRegexAsync(regex, token).ToListAsync(token), token);
        }
    }
}

