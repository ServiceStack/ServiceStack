using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack.Caching;

public partial class OrmLiteCacheClient<TCacheEntry> : ICacheClientAsync, IRemoveByPatternAsync
{
    static void ConfigureDb(IDbConnection db) => db.WithTag(nameof(OrmLiteCacheClient));
    public async Task<T> ExecAsync<T>(Func<IDbConnection, Task<T>> action, CancellationToken token=default)
    {
        using (JsConfig.With(new Config { ExcludeTypeInfo = false }))
        using (var db = await DbFactory.OpenAsync(ConfigureDb,token).ConfigAwait())
        {
            return await action(db).ConfigAwait();
        }
    }

    public async Task ExecAsync(Func<IDbConnection,Task> action, CancellationToken token=default)
    {
        using (JsConfig.With(new Config { ExcludeTypeInfo = false }))
        using (var db = await DbFactory.OpenAsync(ConfigureDb,token).ConfigAwait())
        {
            await action(db).ConfigAwait();
        }
    }

    public async Task<bool> RemoveAsync(string key, CancellationToken token=default)
    {
        return await ExecAsync(async db => 
            await db.DeleteByIdAsync<TCacheEntry>(key, token: token).ConfigAwait() > 0, token).ConfigAwait();
    }

    public async Task RemoveAllAsync(IEnumerable<string> keys, CancellationToken token=default)
    {
        await ExecAsync(async db => 
            await db.DeleteByIdsAsync<TCacheEntry>(keys, token: token).ConfigAwait() > 0, token).ConfigAwait();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken token=default)
    {
        return await ExecAsync(async db =>
        {
            var cache = Verify(db, await db.SingleByIdAsync<TCacheEntry>(key, token).ConfigAwait());
            return cache == null
                ? default
                : db.Deserialize<T>(cache.Data);
        }, token).ConfigAwait();
    }

    public async Task<long> IncrementAsync(string key, uint amount, CancellationToken token=default)
    {
        return await ExecAsync(async db =>
        {
            long nextVal;
            using var dbTrans = db.OpenTransaction(IsolationLevel.ReadCommitted);
            var cache = Verify(db, await db.SingleByIdAsync<TCacheEntry>(key, token).ConfigAwait());

            if (cache == null)
            {
                nextVal = amount;
                await db.InsertAsync(CreateEntry(key, nextVal.ToString()), token: token).ConfigAwait();
            }
            else
            {
                nextVal = long.Parse(cache.Data ?? "0") + amount;
                cache.Data = nextVal.ToString();

                await db.UpdateAsync(cache, token: token).ConfigAwait();
            }

            dbTrans.Commit();

            return nextVal;
        }, token).ConfigAwait();
    }

    public async Task<long> DecrementAsync(string key, uint amount, CancellationToken token=default)
    {
        return await ExecAsync(async db =>
        {
            long nextVal;
            using var dbTrans = db.OpenTransaction(IsolationLevel.ReadCommitted);
            var cache = Verify(db, await db.SingleByIdAsync<TCacheEntry>(key, token).ConfigAwait());

            if (cache == null)
            {
                nextVal = -amount;
                await db.InsertAsync(CreateEntry(key, nextVal.ToString()), token: token).ConfigAwait();
            }
            else
            {
                nextVal = long.Parse(cache.Data ?? "0") - amount;
                cache.Data = nextVal.ToString();

                await db.UpdateAsync(cache, token: token).ConfigAwait();
            }

            dbTrans.Commit();

            return nextVal;
        }, token).ConfigAwait();
    }

    public async Task<bool> AddAsync<T>(string key, T value, CancellationToken token=default)
    {
        try
        {
            await ExecAsync(async db =>
            {
                await db.InsertAsync(CreateEntry(key, db.Serialize(value)), token: token).ConfigAwait();
            }, token).ConfigAwait();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static async Task<bool> UpdateIfExistsAsync<T>(IDbConnection db, string key, T value, CancellationToken token=default)
    {
        var exists = await db.UpdateOnlyAsync(() => new TCacheEntry
            {
                Id = key,
                Data = db.Serialize(value),
                ModifiedDate = DateTime.UtcNow,
            },
            @where: q => q.Id == key, token: token).ConfigAwait() == 1;

        return exists;
    }

    private static async Task<bool> UpdateIfExistsAsync<T>(IDbConnection db, string key, T value, DateTime expiresAt, CancellationToken token=default)
    {
        var exists = await db.UpdateOnlyAsync(() => new TCacheEntry
            {
                Id = key,
                Data = db.Serialize(value),
                ExpiryDate = expiresAt,
                ModifiedDate = DateTime.UtcNow,
            },
            @where: q => q.Id == key, token: token).ConfigAwait() == 1;

        return exists;
    }

    public async Task<bool> SetAsync<T>(string key, T value, CancellationToken token=default)
    {
        return await ExecAsync(async db =>
        {
            var exists = await UpdateIfExistsAsync(db, key, value, token).ConfigAwait();

            if (!exists)
            {
                try
                {
                    await db.InsertAsync(CreateEntry(key, db.Serialize(value)), token: token).ConfigAwait();
                }
                catch (Exception)
                {
                    exists = await UpdateIfExistsAsync(db, key, value, token).ConfigAwait();
                    if (!exists) throw;
                }
            }

            return true;
        }, token).ConfigAwait();
    }

    public async Task<bool> ReplaceAsync<T>(string key, T value, CancellationToken token=default)
    {
        return await ExecAsync(async db =>
        {
            var exists = await db.UpdateOnlyAsync(() => new TCacheEntry
                {
                    Id = key,
                    Data = db.Serialize(value),
                    ModifiedDate = DateTime.UtcNow,
                },
                where: q => q.Id == key, token: token).ConfigAwait() == 1;

            if (!exists)
            {
                await db.InsertAsync(CreateEntry(key, db.Serialize(value)), token: token).ConfigAwait();
            }

            return true;
        }, token).ConfigAwait();
    }

    public async Task<bool> AddAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token=default)
    {
        try
        {
            await ExecAsync(async db =>
            {
                await db.InsertAsync(CreateEntry(key, db.Serialize(value), expires: expiresAt), token: token).ConfigAwait();
            }, token).ConfigAwait();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> SetAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token=default)
    {
        return await ExecAsync(async db =>
        {
            var exists = await UpdateIfExistsAsync(db, key, value, expiresAt, token).ConfigAwait();
            if (!exists)
            {
                try
                {
                    await db.InsertAsync(CreateEntry(key, db.Serialize(value), expires: expiresAt), token: token).ConfigAwait();
                }
                catch (Exception)
                {
                    exists = await UpdateIfExistsAsync(db, key, value, expiresAt, token).ConfigAwait();
                    if (!exists) throw;
                }
            }

            return true;
        }, token).ConfigAwait();
    }

    public async Task<bool> ReplaceAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token=default)
    {
        return await ExecAsync(async db =>
        {
            var exists = await db.UpdateOnlyAsync(() => new TCacheEntry
                {
                    Id = key,
                    Data = db.Serialize(value),
                    ExpiryDate = expiresAt,
                    ModifiedDate = DateTime.UtcNow,
                },
                where: q => q.Id == key, token: token).ConfigAwait() == 1;

            if (!exists)
            {
                await db.InsertAsync(CreateEntry(key, db.Serialize(value), expires: expiresAt), token: token).ConfigAwait();
            }

            return true;
        }, token).ConfigAwait();
    }

    public async Task<bool> AddAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token=default)
    {
        try
        {
            await ExecAsync(async db =>
            {
                await db.InsertAsync(CreateEntry(key, db.Serialize(value),
                    expires: DateTime.UtcNow.Add(expiresIn)), token: token).ConfigAwait();
            }, token).ConfigAwait();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token=default)
    {
        return await ExecAsync(async db =>
        {
            var exists = await UpdateIfExistsAsync(db, key, value, DateTime.UtcNow.Add(expiresIn), token).ConfigAwait();
            if (!exists)
            {
                try
                {
                    await db.InsertAsync(CreateEntry(key, db.Serialize(value), expires: DateTime.UtcNow.Add(expiresIn)), token: token).ConfigAwait();
                }
                catch (Exception)
                {
                    exists = await UpdateIfExistsAsync(db, key, value, DateTime.UtcNow.Add(expiresIn), token).ConfigAwait();
                    if (!exists) throw;
                }
            }

            return true;
        }, token).ConfigAwait();
    }

    public async Task<bool> ReplaceAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token=default)
    {
        return await ExecAsync(async db =>
        {
            var exists = await db.UpdateOnlyAsync(() => new TCacheEntry
                {
                    Id = key,
                    Data = db.Serialize(value),
                    ExpiryDate = DateTime.UtcNow.Add(expiresIn),
                    ModifiedDate = DateTime.UtcNow,
                },
                where: q => q.Id == key, token: token).ConfigAwait() == 1;

            if (!exists)
            {
                await db.InsertAsync(CreateEntry(key, db.Serialize(value), expires: DateTime.UtcNow.Add(expiresIn)), token: token).ConfigAwait();
            }

            return true;
        }, token).ConfigAwait();
    }

    public async Task FlushAllAsync(CancellationToken token=default)
    {
        await ExecAsync(async db =>
        {
            await db.DeleteAllAsync<TCacheEntry>(token).ConfigAwait();
        }, token).ConfigAwait();
    }

    public async Task<IDictionary<string, T>> GetAllAsync<T>(IEnumerable<string> keys, CancellationToken token=default)
    {
        return await ExecAsync(async db =>
        {
            var results = Verify(db, await db.SelectByIdsAsync<TCacheEntry>(keys, token).ConfigAwait());
            var map = new Dictionary<string, T?>();

            results.Each(x =>
                map[x.Id] = db.Deserialize<T>(x.Data));

            foreach (var key in keys)
            {
                if (!map.ContainsKey(key))
                    map[key] = default;
            }

            return map;
        }, token).ConfigAwait();
    }

    public async Task SetAllAsync<T>(IDictionary<string, T> values, CancellationToken token=default)
    {
        await ExecAsync(async db =>
        {
            var rows = values.Select(entry =>
                    CreateEntry(entry.Key, db.Serialize(entry.Value)))
                .ToList();

            await db.InsertAllAsync(rows, token).ConfigAwait();
        }, token).ConfigAwait();
    }

    public async Task<TimeSpan?> GetTimeToLiveAsync(string key, CancellationToken token=default)
    {
        return await ExecAsync(async db =>
        {
            var cache = await db.SingleByIdAsync<TCacheEntry>(key, token).ConfigAwait();
            if (cache == null)
                return null;

            if (cache.ExpiryDate == null)
                return TimeSpan.MaxValue;

            return cache.ExpiryDate - DateTime.UtcNow;
        }, token).ConfigAwait();
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken token=default)
    {
        await ExecAsync(async db => {
            var dbPattern = pattern.Replace('*', '%');
            var dialect = db.GetDialectProvider();
            await db.DeleteAsync<TCacheEntry>(dialect.GetQuotedColumnName("Id") + " LIKE " + dialect.GetParam("dbPattern"), 
                new { dbPattern }, token).ConfigAwait();
        }, token).ConfigAwait();
    }

    public async IAsyncEnumerable<string> GetKeysByPatternAsync(string pattern, [EnumeratorCancellation] CancellationToken token = default)
    {
        var results = await ExecAsync(async db =>
        {
            if (pattern == "*")
                return await db.ColumnAsync<string>(db.From<TCacheEntry>().Select(x => x.Id), token).ConfigAwait();

            var dbPattern = pattern.Replace('*', '%');
            var dialect = db.Dialect();
            var id = dialect.GetQuotedColumnName("Id");

            return await db.ColumnAsync<string>(db.From<TCacheEntry>()
                .Where(id + " LIKE {0}", dbPattern), token).ConfigAwait();
        }, token).ConfigAwait();
            
        foreach (var key in results)
        {
            token.ThrowIfCancellationRequested();
                
            yield return key;
        }
    }

    public ValueTask DisposeAsync() => default;

    public async Task RemoveExpiredEntriesAsync(CancellationToken token=default)
    {
        await ExecAsync(async db => 
            await db.DeleteAsync<TCacheEntry>(q => DateTime.UtcNow > q.ExpiryDate, token: token).ConfigAwait(), token).ConfigAwait();
    }

    public Task RemoveByRegexAsync(string regex, CancellationToken token=default) => 
        throw new NotImplementedException();
}