using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack.Caching
{
    public partial class OrmLiteCacheClient<TCacheEntry> : ICacheClientAsync, IRemoveByPatternAsync
    {
        public async Task<T> ExecAsync<T>(Func<IDbConnection, Task<T>> action)
        {
            using (JsConfig.With(new Config { ExcludeTypeInfo = false }))
            using (var db = await DbFactory.OpenAsync())
            {
                return await action(db);
            }
        }

        public async Task ExecAsync(Func<IDbConnection,Task> action)
        {
            using (JsConfig.With(new Config { ExcludeTypeInfo = false }))
            using (var db = await DbFactory.OpenAsync())
            {
                await action(db);
            }
        }

        public Task<bool> RemoveAsync(string key)
        {
            return ExecAsync(async db => await db.DeleteByIdAsync<TCacheEntry>(key) > 0);
        }

        public Task RemoveAllAsync(IEnumerable<string> keys)
        {
            return ExecAsync(async db => await db.DeleteByIdsAsync<TCacheEntry>(keys) > 0);
        }

        public async Task<T> GetAsync<T>(string key)
        {
            return await ExecAsync(async db =>
            {
                var cache = Verify(db, await db.SingleByIdAsync<TCacheEntry>(key));
                return cache == null
                    ? default
                    : db.Deserialize<T>(cache.Data);
            });
        }

        public async Task<long> IncrementAsync(string key, uint amount)
        {
            return await ExecAsync(async db =>
            {
                long nextVal;
                using var dbTrans = db.OpenTransaction(IsolationLevel.ReadCommitted);
                var cache = Verify(db, await db.SingleByIdAsync<TCacheEntry>(key));

                if (cache == null)
                {
                    nextVal = amount;
                    await db.InsertAsync(CreateEntry(key, nextVal.ToString()));
                }
                else
                {
                    nextVal = long.Parse(cache.Data) + amount;
                    cache.Data = nextVal.ToString();

                    await db.UpdateAsync(cache);
                }

                dbTrans.Commit();

                return nextVal;
            });
        }

        public async Task<long> DecrementAsync(string key, uint amount)
        {
            return await ExecAsync(async db =>
            {
                long nextVal;
                using var dbTrans = db.OpenTransaction(IsolationLevel.ReadCommitted);
                var cache = Verify(db, await db.SingleByIdAsync<TCacheEntry>(key));

                if (cache == null)
                {
                    nextVal = -amount;
                    await db.InsertAsync(CreateEntry(key, nextVal.ToString()));
                }
                else
                {
                    nextVal = long.Parse(cache.Data) - amount;
                    cache.Data = nextVal.ToString();

                    await db.UpdateAsync(cache);
                }

                dbTrans.Commit();

                return nextVal;
            });
        }

        public async Task<bool> AddAsync<T>(string key, T value)
        {
            try
            {
                await ExecAsync(async db =>
                {
                    await db.InsertAsync(CreateEntry(key, db.Serialize(value)));
                });
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static async Task<bool> UpdateIfExistsAsync<T>(IDbConnection db, string key, T value)
        {
            var exists = await db.UpdateOnlyAsync(new TCacheEntry
                {
                    Id = key,
                    Data = db.Serialize(value),
                    ModifiedDate = DateTime.UtcNow,
                },
                onlyFields: q => new { q.Data, q.ModifiedDate },
                @where: q => q.Id == key) == 1;

            return exists;
        }

        private static async Task<bool> UpdateIfExistsAsync<T>(IDbConnection db, string key, T value, DateTime expiresAt)
        {
            var exists = await db.UpdateOnlyAsync(new TCacheEntry
                {
                    Id = key,
                    Data = db.Serialize(value),
                    ExpiryDate = expiresAt,
                    ModifiedDate = DateTime.UtcNow,
                },
                onlyFields: q => new { q.Data, ExpiredDate = q.ExpiryDate, q.ModifiedDate },
                @where: q => q.Id == key) == 1;

            return exists;
        }

        public async Task<bool> SetAsync<T>(string key, T value)
        {
            return await ExecAsync(async db =>
            {
                var exists = await UpdateIfExistsAsync(db, key, value);

                if (!exists)
                {
                    try
                    {
                        await db.InsertAsync(CreateEntry(key, db.Serialize(value)));
                    }
                    catch (Exception)
                    {
                        exists = await UpdateIfExistsAsync(db, key, value);
                        if (!exists) throw;
                    }
                }

                return true;
            });
        }

        public async Task<bool> ReplaceAsync<T>(string key, T value)
        {
            return await ExecAsync(async db =>
            {
                var exists = await db.UpdateOnlyAsync(new TCacheEntry
                    {
                        Id = key,
                        Data = db.Serialize(value),
                        ModifiedDate = DateTime.UtcNow,
                    },
                    onlyFields: q => new { q.Data, q.ModifiedDate },
                    where: q => q.Id == key) == 1;

                if (!exists)
                {
                    await db.InsertAsync(CreateEntry(key, db.Serialize(value)));
                }

                return true;
            });
        }

        public async Task<bool> AddAsync<T>(string key, T value, DateTime expiresAt)
        {
            try
            {
                await ExecAsync(async db =>
                {
                    await db.InsertAsync(CreateEntry(key, db.Serialize(value), expires: expiresAt));
                });
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> SetAsync<T>(string key, T value, DateTime expiresAt)
        {
            return await ExecAsync(async db =>
            {
                var exists = await UpdateIfExistsAsync(db, key, value, expiresAt);
                if (!exists)
                {
                    try
                    {
                        await db.InsertAsync(CreateEntry(key, db.Serialize(value), expires: expiresAt));
                    }
                    catch (Exception)
                    {
                        exists = await UpdateIfExistsAsync(db, key, value, expiresAt);
                        if (!exists) throw;
                    }
                }

                return true;
            });
        }

        public async Task<bool> ReplaceAsync<T>(string key, T value, DateTime expiresAt)
        {
            return await ExecAsync(async db =>
            {
                var exists = await db.UpdateOnlyAsync(new TCacheEntry
                    {
                        Id = key,
                        Data = db.Serialize(value),
                        ExpiryDate = expiresAt,
                        ModifiedDate = DateTime.UtcNow,
                    },
                    onlyFields: q => new { q.Data, ExpiredDate = q.ExpiryDate, q.ModifiedDate },
                    where: q => q.Id == key) == 1;

                if (!exists)
                {
                    await db.InsertAsync(CreateEntry(key, db.Serialize(value), expires: expiresAt));
                }

                return true;
            });
        }

        public async Task<bool> AddAsync<T>(string key, T value, TimeSpan expiresIn)
        {
            try
            {
                await ExecAsync(async db =>
                {
                    await db.InsertAsync(CreateEntry(key, db.Serialize(value),
                        expires: DateTime.UtcNow.Add(expiresIn)));
                });
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> SetAsync<T>(string key, T value, TimeSpan expiresIn)
        {
            return await ExecAsync(async db =>
            {
                var exists = await UpdateIfExistsAsync(db, key, value, DateTime.UtcNow.Add(expiresIn));
                if (!exists)
                {
                    try
                    {
                        await db.InsertAsync(CreateEntry(key, db.Serialize(value), expires: DateTime.UtcNow.Add(expiresIn)));
                    }
                    catch (Exception)
                    {
                        exists = await UpdateIfExistsAsync(db, key, value, DateTime.UtcNow.Add(expiresIn));
                        if (!exists) throw;
                    }
                }

                return true;
            });
        }

        public async Task<bool> ReplaceAsync<T>(string key, T value, TimeSpan expiresIn)
        {
            return await ExecAsync(async db =>
            {
                var exists = await db.UpdateOnlyAsync(new TCacheEntry
                    {
                        Id = key,
                        Data = db.Serialize(value),
                        ExpiryDate = DateTime.UtcNow.Add(expiresIn),
                        ModifiedDate = DateTime.UtcNow,
                    },
                    onlyFields: q => new { q.Data, ExpiredDate = q.ExpiryDate, q.ModifiedDate },
                    where: q => q.Id == key) == 1;

                if (!exists)
                {
                    await db.InsertAsync(CreateEntry(key, db.Serialize(value), expires: DateTime.UtcNow.Add(expiresIn)));
                }

                return true;
            });
        }

        public async Task FlushAllAsync()
        {
            await ExecAsync(async db =>
            {
                await db.DeleteAllAsync<TCacheEntry>();
            });
        }

        public async Task<IDictionary<string, T>> GetAllAsync<T>(IEnumerable<string> keys)
        {
            return await ExecAsync(async db =>
            {
                var results = Verify(db, await db.SelectByIdsAsync<TCacheEntry>(keys));
                var map = new Dictionary<string, T>();

                results.Each(x =>
                    map[x.Id] = db.Deserialize<T>(x.Data));

                foreach (var key in keys)
                {
                    if (!map.ContainsKey(key))
                        map[key] = default;
                }

                return map;
            });
        }

        public async Task SetAllAsync<T>(IDictionary<string, T> values)
        {
            await ExecAsync(async db =>
            {
                var rows = values.Select(entry =>
                    CreateEntry(entry.Key, db.Serialize(entry.Value)))
                    .ToList();

                await db.InsertAllAsync(rows);
            });
        }

        public async Task<TimeSpan?> GetTimeToLiveAsync(string key)
        {
            return await ExecAsync(async db =>
            {
                var cache = await db.SingleByIdAsync<TCacheEntry>(key);
                if (cache == null)
                    return null;

                if (cache.ExpiryDate == null)
                    return TimeSpan.MaxValue;

                return cache.ExpiryDate - DateTime.UtcNow;
            });
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            await ExecAsync(async db => {
                var dbPattern = pattern.Replace('*', '%');
                var dialect = db.GetDialectProvider();
                await db.DeleteAsync<TCacheEntry>(dialect.GetQuotedColumnName("Id") + " LIKE " + dialect.GetParam("dbPattern"), new { dbPattern });
            });
        }

        public async Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern)
        {
            return await ExecAsync(async db =>
            {
                if (pattern == "*")
                    return await db.ColumnAsync<string>(db.From<TCacheEntry>().Select(x => x.Id));

                var dbPattern = pattern.Replace('*', '%');
                var dialect = db.Dialect();
                var id = dialect.GetQuotedColumnName("Id");

                return await db.ColumnAsync<string>(db.From<TCacheEntry>()
                    .Where(id + " LIKE {0}", dbPattern));
            });
        }

        public async Task RemoveExpiredEntriesAsync()
        {
            await ExecAsync(async db => await db.DeleteAsync<TCacheEntry>(q => DateTime.UtcNow > q.ExpiryDate));
        }

        public Task RemoveByRegexAsync(string regex) => throw new NotImplementedException();
    }
}