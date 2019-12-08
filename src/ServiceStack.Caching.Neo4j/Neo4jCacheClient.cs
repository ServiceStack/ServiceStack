using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Neo4j.Driver.V1;

namespace ServiceStack.Caching.Neo4j
{
    // ReSharper disable once InconsistentNaming
    public class Neo4jCacheClient : Neo4jCacheClient<CacheEntry>
    {
        public Neo4jCacheClient(IDriver driver) : base(driver) { }
    }
    
    // ReSharper disable once InconsistentNaming
    public class Neo4jCacheClient<TCacheEntry> : ICacheClient, IRequiresSchema, ICacheClientExtended, IRemoveByPattern
        where TCacheEntry : ICacheEntry, new()
    {
        private readonly IDriver driver;

        public Neo4jCacheClient(IDriver driver)
        {
            this.driver = driver;
        }
        
        private TCacheEntry CreateEntry(
            string id, 
            string data = null,
            DateTime? created = null, 
            DateTime? expires = null)
        {
            var createdDate = created ?? DateTime.UtcNow;
            return new TCacheEntry
            {
                Id = id,
                Data = data,
                ExpiryDate = expires,
                CreatedDate = createdDate,
                ModifiedDate = createdDate,
            };
        }

        public bool Remove(string key)
        {
            return driver.TxWriteCache(c => c.Remove(key));
        }

        public void RemoveAll(IEnumerable<string> keys)
        {
            driver.TxWriteCache(c => c.RemoveAll(keys));
        }

        public T Get<T>(string key)
        {
            var cacheItem = driver.TxReadCache(c => c.GetCacheEntry<TCacheEntry>(key));
            return cacheItem.Deserialize<T>();
        }

        public long Increment(string key, uint amount)
        {
            return driver.TxCacheExec(c =>
            {
                long nextVal;

                var cacheEntry = c.Verify(c.GetCacheEntry<TCacheEntry>(key));
                if (cacheEntry == null)
                {
                    nextVal = amount;
                    c.Create(CreateEntry(key, nextVal.ToString()));
                }
                else
                {
                    nextVal = long.Parse(cacheEntry.Data) + amount;
                    cacheEntry.Data = nextVal.ToString();
                    c.Update(cacheEntry);
                }

                return nextVal;
            });
        }

        public long Decrement(string key, uint amount)
        {
            return driver.TxCacheExec(c =>
            {
                long nextVal;

                var cacheEntry = c.Verify(c.GetCacheEntry<TCacheEntry>(key));
                if (cacheEntry == null)
                {
                    nextVal = -amount;
                    c.Create(CreateEntry(key, nextVal.ToString()));
                }
                else
                {
                    nextVal = long.Parse(cacheEntry.Data) - amount;
                    cacheEntry.Data = nextVal.ToString();
                    c.Update(cacheEntry);
                }

                return nextVal;
            });
        }

        public bool Add<T>(string key, T value)
        {
            try
            {
                return driver.TxCacheExec(c =>
                {
                    c.Create(CreateEntry(key, value.Serialize()));
                    return true;
                });
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool UpdateIfExists<T>(string key, T value)
        {
            return driver.TxCacheExec(c =>
            {
                if (!c.Exists(key)) return false;
                
                c.Update(key, value.Serialize(), DateTime.UtcNow);
                return true;
            });
        }

        private bool UpdateIfExists<T>(string key, T value, DateTime expiresAt)
        {
            return driver.TxCacheExec(c =>
            {
                if (!c.Exists(key)) return false;
                
                c.Update(key, value.Serialize(), DateTime.UtcNow, expiresAt);
                return true;
            });
        }

        public bool Set<T>(string key, T value)
        {
            return driver.TxCacheExec(c =>
            {
                if (!c.Exists(key))
                {
                    c.Create(CreateEntry(key, value.Serialize()));
                }
                else
                {
                    c.Update(key, value.Serialize(), DateTime.UtcNow);
                }

                return true;
            });
        }

        public bool Replace<T>(string key, T value)
        {
            return driver.TxCacheExec(c =>
            {
                if (!c.Exists(key)) return false;
                
                c.Update(key, value.Serialize(), DateTime.UtcNow);

                return true;
            });
        }

        public bool Add<T>(string key, T value, DateTime expiresAt)
        {
            try
            {
                return driver.TxCacheExec(c =>
                {
                    c.Create(CreateEntry(key, value.Serialize(), expiresAt));
                    return true;
                });
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool Set<T>(string key, T value, DateTime expiresAt)
        {
            return Exec(db =>
            {
                var exists = UpdateIfExists(db, key, value, expiresAt);
                if (!exists)
                {
                    try
                    {
                        db.Insert(CreateEntry(key, db.Serialize(value), expires: expiresAt));
                    }
                    catch (Exception)
                    {
                        exists = UpdateIfExists(db, key, value, expiresAt);
                        if (!exists) throw;
                    }
                }

                return true;
            });
        }

        public bool Replace<T>(string key, T value, DateTime expiresAt)
        {
            return Exec(db =>
            {
                var exists = db.UpdateOnly(new TCacheEntry
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
                    db.Insert(CreateEntry(key, db.Serialize(value), expires: expiresAt));
                }

                return true;
            });
        }

        public bool Add<T>(string key, T value, TimeSpan expiresIn)
        {
            try
            {
                Exec(db =>
                {
                    db.Insert(CreateEntry(key, db.Serialize(value),
                        expires: DateTime.UtcNow.Add(expiresIn)));
                });
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool Set<T>(string key, T value, TimeSpan expiresIn)
        {
            return Exec(db =>
            {
                var exists = UpdateIfExists(db, key, value, DateTime.UtcNow.Add(expiresIn));
                if (!exists)
                {
                    try
                    {
                        db.Insert(CreateEntry(key, db.Serialize(value), expires: DateTime.UtcNow.Add(expiresIn)));
                    }
                    catch (Exception)
                    {
                        exists = UpdateIfExists(db, key, value, DateTime.UtcNow.Add(expiresIn));
                        if (!exists) throw;
                    }
                }

                return true;
            });
        }

        public bool Replace<T>(string key, T value, TimeSpan expiresIn)
        {
            return Exec(db =>
            {
                var exists = db.UpdateOnly(new TCacheEntry
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
                    db.Insert(CreateEntry(key, db.Serialize(value), expires: DateTime.UtcNow.Add(expiresIn)));
                }

                return true;
            });
        }

        public void FlushAll()
        {
            Exec(db =>
            {
                db.DeleteAll<TCacheEntry>();
            });
        }

        public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys)
        {
            return Exec(db =>
            {
                var results = Verify(db, db.SelectByIds<TCacheEntry>(keys));
                var map = new Dictionary<string, T>();

                results.Each(x =>
                    map[x.Id] = db.Deserialize<T>(x.Data));

                foreach (var key in keys)
                {
                    if (!map.ContainsKey(key))
                        map[key] = default(T);
                }

                return map;
            });
        }

        public void SetAll<T>(IDictionary<string, T> values)
        {
            Exec(db =>
            {
                var rows = values.Select(entry =>
                        CreateEntry(entry.Key, db.Serialize<T>(entry.Value)))
                    .ToList();

                db.InsertAll(rows);
            });
        }

        public void InitSchema()
        {
            Exec(db => db.CreateTableIfNotExists<TCacheEntry>());
        }

        public List<TCacheEntry> Verify(IDbConnection db, IEnumerable<TCacheEntry> entries)
        {
            var results = entries.ToList();
            var expired = results.RemoveAll(x => x.ExpiryDate != null && DateTime.UtcNow > x.ExpiryDate);
            if (expired > 0)
            {
                db.Delete<TCacheEntry>(q => DateTime.UtcNow > q.ExpiryDate);
            }

            return results;
        }

        public TCacheEntry Verify(ITransaction tx, TCacheEntry entry)
        {
            if (entry != null 
                && entry.ExpiryDate != null 
                && DateTime.UtcNow > entry.ExpiryDate)
            {
                RemoveExpired(tx);
                return default;
            }
            return entry;
        }

        public TimeSpan? GetTimeToLive(string key)
        {
            return Exec(db =>
            {
                var cache = db.SingleById<TCacheEntry>(key);
                if (cache == null)
                    return null;

                if (cache.ExpiryDate == null)
                    return TimeSpan.MaxValue;

                return cache.ExpiryDate - DateTime.UtcNow;
            });
        }

        public void RemoveByPattern(string pattern)
        {
            Exec(db => {
                var dbPattern = pattern.Replace('*', '%');
                var dialect = db.GetDialectProvider();
                db.Delete<TCacheEntry>(dialect.GetQuotedColumnName("Id") + " LIKE " + dialect.GetParam("dbPattern"), new { dbPattern });
            });
        }

        public IEnumerable<string> GetKeysByPattern(string pattern)
        {
            return Exec(db =>
            {
                if (pattern == "*")
                    return db.Column<string>(db.From<TCacheEntry>().Select(x => x.Id));

                var dbPattern = pattern.Replace('*', '%');
                var dialect = db.GetDialectProvider();
                var id = dialect.GetQuotedColumnName("Id");

                return db.Column<string>(db.From<TCacheEntry>()
                    .Where(id + " LIKE {0}", dbPattern));
            });
        }

        public void RemoveByRegex(string regex)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }
    }
    
    public interface ICacheEntry
    {
        string Id { get; set; }
        string Data { get; set; }
        DateTime? ExpiryDate { get; set; }
        DateTime CreatedDate { get; set; }
        DateTime ModifiedDate { get; set; }
    }

    public class CacheEntry : ICacheEntry
    {
        public string Id { get; set; }
        public string Data { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
    }

    internal static class CacheValueExtensions
    {
        public static string Serialize<T>(this T value)
        {
            return value.ToJsv();
        }

        public static T Deserialize<T>(this ICacheEntry cacheEntry)
        {
            return cacheEntry?.Data == null 
                ? default 
                : cacheEntry.Data.FromJsv<T>();
        }
    }
    
    internal static class DriverExtensions
    {
        public static void ReadTxQuery(this IDriver driver, Action<ITransaction> work)
        {
            using (var session = driver.Session())
            {
                session.ReadTransaction(work);
            }
        }
        
        public static T ReadTxQuery<T>(this IDriver driver, Func<ITransaction, T> work)
        {
            using (var session = driver.Session())
            {
                return session.ReadTransaction(work);
            }
        }

        public static IStatementResult ReadQuery(this IDriver driver, string statement, IDictionary<string, object> parameters = null)
        {
            using (var session = driver.Session())
            {
                return session.ReadTransaction(tx => tx.Run(statement, parameters));
            }
        }

        public static IStatementResult ReadQuery(this IDriver driver, string statement, object parameters)
        {
            using (var session = driver.Session())
            {
                return session.ReadTransaction(tx => tx.Run(statement, parameters));
            }
        }

        public static void WriteTxQuery(this IDriver driver, Action<ITransaction> txWorkFn)
        {
            using (var session = driver.Session())
            {
                session.WriteTransaction(txWorkFn);
            }
        }
        
        public static T WriteTxQuery<T>(this IDriver driver, Func<ITransaction, T> txWorkFn)
        {
            using (var session = driver.Session())
            {
                return session.WriteTransaction(txWorkFn);
            }
        }

        public static IStatementResult WriteQuery(this IDriver driver, string statement, IDictionary<string, object> parameters = null)
        {
            using (var session = driver.Session())
            {
                return session.WriteTransaction(tx => tx.Run(statement, parameters));
            }
        }

        public static IStatementResult WriteQuery(this IDriver driver, string statement, object parameters = null)
        {
            using (var session = driver.Session())
            {
                return session.WriteTransaction(tx => tx.Run(statement, parameters));
            }
        }
        
        
        
        public static void TxReadCache(this IDriver driver, Action<Neo4jTransactionalCacheClient> cacheFn)
        {
            using (var session = driver.Session())
            {
                session.ReadTransaction(tx =>
                {
                    var cache = new Neo4jTransactionalCacheClient(tx);
                    cacheFn(cache);
                });
            }
        }

        public static T TxReadCache<T>(this IDriver driver, Func<Neo4jTransactionalCacheClient, T> cacheFn)
        {
            using (var session = driver.Session())
            {
                return session.ReadTransaction(tx =>
                {
                    var cache = new Neo4jTransactionalCacheClient(tx);
                    return cacheFn(cache);
                });
            }
        }

        public static void TxWriteCache(this IDriver driver, Action<Neo4jTransactionalCacheClient> cacheFn)
        {
            using (var session = driver.Session())
            {
                session.WriteTransaction(tx =>
                {
                    var cache = new Neo4jTransactionalCacheClient(tx);
                    cacheFn(cache);
                });
            }
        }

        public static T TxWriteCache<T>(this IDriver driver, Func<Neo4jTransactionalCacheClient, T> cacheFn)
        {
            using (var session = driver.Session())
            {
                return session.WriteTransaction(tx =>
                {
                    var cache = new Neo4jTransactionalCacheClient(tx);
                    return cacheFn(cache);
                });
            }
        }
        
        public static T TxCacheExec<T>(this IDriver driver, Func<Neo4jTransactionalCacheClient, T> cacheFn)
        {
            return driver.TxWriteCache(cacheFn);
        }
        
        public static void TxCacheExec(this IDriver driver, Action<Neo4jTransactionalCacheClient> cacheFn)
        {
            driver.TxWriteCache(cacheFn);
        }
    }
    
    internal static class RecordExtensions
    {
        public static IEnumerable<TReturn> Map<TReturn>(
            this IEnumerable<IRecord> records)
        {
            return records.Select(record => ((IEntity) record[0]).Map<TReturn>());
        }

        public static Dictionary<string, TReturn> MapDictionary<TReturn>(
            this IEnumerable<IRecord> records)
        {
            return records.ToDictionary(
                record => record[0].As<string>(), 
                record => ((IEntity) record[1]).Map<TReturn>());
        }

        public static TReturn Map<TReturn>(this IEntity entity)
        {
            return entity.Properties.FromObjectDictionary<TReturn>();
        }

        public static bool Truthy(this IEnumerable<IRecord> records)
        {
            var record = records.SingleOrDefault();
            return record != null && record[0].As<bool>();
        }
    }
    
        // ReSharper disable once InconsistentNaming
        internal class Neo4jTransactionalCacheClient
        {
            private static class Label
            {
                public const string CacheEntry = nameof(CacheEntry);    
            }

            private static class Query
            {
                public static string Exists => $@"
                    MATCH (item:{Label.CacheEntry} {{Id: $key}})
                    RETURN item IS NOT NULL";

                public static string Create => $@"
                    CREATE (item:{Label.CacheEntry})
                    SET item = $item";

                public static string GetByKey => $@"
                    MATCH (item:{Label.CacheEntry} {{Id: $key}})
                    RETURN item";
                
                public static string GetByKeys => $@"
                    MATCH (item:{Label.CacheEntry})
                    WHERE item.Id IN $keys
                    RETURN item";

                public static string Update => $@"
                    MATCH (item:{Label.CacheEntry} {{Id: $item.Id}})
                    SET item = $item";

                public static string UpdateData => $@"
                    MATCH (item:{Label.CacheEntry} {{Id: $key}})
                    SET item.Data = $data
                    SET item.ModifiedDate = $modifiedDate";

                public static string UpdateDataWithExpiry => $@"
                    MATCH (item:{Label.CacheEntry} {{Id: $key}})
                    SET item.Data = $data
                    SET item.ModifiedDate = $modifiedDate
                    SET item.ModifiedDate = $modifiedDate";

                public static string DeleteByKey => $@"
                    MATCH (item:{Label.CacheEntry} {{Id: $key}})
                    DELETE item
                    RETURN item IS NOT NULL";

                public static string DeleteByKeys => $@"
                    MATCH (item:{Label.CacheEntry})
                    WHERE item.Id IN $keys";

                public static string DeleteExpired => $@"
                    MATCH (item:{Label.CacheEntry})
                    WHERE $now > item.ExpiryDate
                    DELETE item";

            }

            private readonly ITransaction transaction;

            public Neo4jTransactionalCacheClient(ITransaction transaction)
            {
                this.transaction = transaction;
            }

            public bool Exists(string key)
            {
                var parameters = new { key };

                var result = transaction.Run(Query.Exists, parameters);
                return result.Truthy();
            }

            public TCacheEntry Verify<TCacheEntry>(TCacheEntry entry)
                where TCacheEntry : ICacheEntry, new()
            {
                if (entry != null 
                    && entry.ExpiryDate != null 
                    && DateTime.UtcNow > entry.ExpiryDate)
                {
                    RemoveExpired();
                    return default;
                }
                return entry;
            }
            
            public void Create<TCacheEntry>(TCacheEntry entry)
                where TCacheEntry : ICacheEntry, new()
            {
                var parameters = new
                {
                    item = entry.ConvertTo<Dictionary<string, object>>()
                };

                transaction.Run(Query.Create, parameters);
            }

            public void Update<TCacheEntry>(TCacheEntry entry)
                where TCacheEntry : ICacheEntry, new()
            {
                var parameters = new
                {
                    item = entry.ConvertTo<Dictionary<string, object>>()
                };

                transaction.Run(Query.Update, parameters);
            }

            public void Update(string key, string data, DateTime modifiedDate)
            {
                var parameters = new
                {
                    key,
                    data,
                    modifiedDate = new ZonedDateTime(modifiedDate),
                };

                transaction.Run(Query.UpdateData, parameters);
            }

            public void Update(string key, string data, DateTime modifiedDate, DateTime expiresAt)
            {
                var parameters = new
                {
                    key,
                    data,
                    modifiedDate = new ZonedDateTime(modifiedDate),
                    expiresAt = new ZonedDateTime(expiresAt)
                };

                transaction.Run(Query.UpdateDataWithExpiry, parameters);
            }

            public bool Remove(string key)
            {
                var parameters = new { key };

                var result = transaction.Run(Query.DeleteByKey, parameters);
                return result.Truthy();
            }

            public void RemoveAll(IEnumerable<string> keys)
            {
                var parameters = new { keys };

                transaction.Run(Query.DeleteByKeys, parameters);
            }

            public void FlushAll()
            {
                throw new NotImplementedException();
            }

            public void RemoveExpired()
            {
                var parameters = new {now = new ZonedDateTime(DateTime.UtcNow)};

                transaction.Run(Query.DeleteExpired, parameters);
            }

            public TCacheEntry GetCacheEntry<TCacheEntry>(string key)
                where TCacheEntry : ICacheEntry, new()
            {
                var parameters = new { key };

                var result = transaction.Run(Query.GetByKey, parameters);
                
                return result.Map<TCacheEntry>().SingleOrDefault();
            }

            public Dictionary<string, TCacheEntry> GetCacheEntries<TCacheEntry>(IEnumerable<string> keys)
                where TCacheEntry : ICacheEntry, new()
            {
                var parameters = new { keys };

                var result = transaction.Run(Query.GetByKeys, parameters);
                
                return result.MapDictionary<TCacheEntry>();
            }


            public bool Add<T>(string key, T value)
            {
                throw new NotImplementedException();
            }

            public bool Add<T>(string key, T value, DateTime expiresAt)
            {
                throw new NotImplementedException();
            }

            public bool Add<T>(string key, T value, TimeSpan expiresIn)
            {
                throw new NotImplementedException();
            }

            public bool Set<T>(string key, T value)
            {
                throw new NotImplementedException();
            }

            public void SetAll<T>(IDictionary<string, T> values)
            {
                throw new NotImplementedException();
            }

            public bool Set<T>(string key, T value, DateTime expiresAt)
            {
                throw new NotImplementedException();
            }

            public bool Set<T>(string key, T value, TimeSpan expiresIn)
            {
                throw new NotImplementedException();
            }

            public bool Replace<T>(string key, T value)
            {
                throw new NotImplementedException();
            }

            public bool Replace<T>(string key, T value, DateTime expiresAt)
            {
                throw new NotImplementedException();
            }

            public bool Replace<T>(string key, T value, TimeSpan expiresIn)
            {
                throw new NotImplementedException();
            }

            public void InitSchema()
            {
                throw new NotImplementedException();
            }

            public TimeSpan? GetTimeToLive(string key)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<string> GetKeysByPattern(string pattern)
            {
                throw new NotImplementedException();
            }

            public void RemoveByPattern(string pattern)
            {
                throw new NotImplementedException();
            }

            public void RemoveByRegex(string regex)
            {
                throw new NotImplementedException();
            }
        }
}
