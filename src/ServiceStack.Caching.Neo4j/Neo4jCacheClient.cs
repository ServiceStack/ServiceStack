using System;
using System.Collections.Generic;
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
        private readonly Neo4jCacheRepository repository;

        public Neo4jCacheClient(IDriver driver)
        {
            this.driver = driver;
            repository = new Neo4jCacheRepository();
        }
        
        public bool Remove(string key)
        {
            return driver.WriteTxQuery(tx => repository.Remove(tx, key));
        }

        public void RemoveAll(IEnumerable<string> keys)
        {
            driver.WriteTxQuery(tx => repository.RemoveAll(tx, keys));
        }

        public T Get<T>(string key)
        {
            var cacheItem = driver.ReadTxQuery(tx => repository.GetCacheEntry<TCacheEntry>(tx, key));
            return cacheItem.Deserialize<T>();
        }

        public long Increment(string key, uint amount)
        {
            return driver.WriteTxQuery(tx =>
            {
                long nextVal;

                var cacheEntry = Verify(tx, repository.GetCacheEntry<TCacheEntry>(tx, key));
                if (cacheEntry == null)
                {
                    nextVal = amount;
                    repository.Create(tx, CreateEntry(key, nextVal.ToString()));
                }
                else
                {
                    nextVal = long.Parse(cacheEntry.Data) + amount;
                    cacheEntry.Data = nextVal.ToString();
                    repository.Update(tx, cacheEntry);
                }

                return nextVal;
            });
        }

        public long Decrement(string key, uint amount)
        {
            return driver.WriteTxQuery(tx =>
            {
                long nextVal;

                var cacheEntry = Verify(tx, repository.GetCacheEntry<TCacheEntry>(tx, key));
                if (cacheEntry == null)
                {
                    nextVal = -amount;
                    repository.Create(tx, CreateEntry(key, nextVal.ToString()));
                }
                else
                {
                    nextVal = long.Parse(cacheEntry.Data) - amount;
                    cacheEntry.Data = nextVal.ToString();
                    repository.Update(tx, cacheEntry);
                }

                return nextVal;
            });
        }

        public bool Add<T>(string key, T value)
        {
            try
            {
                return driver.WriteTxQuery(tx =>
                {
                    repository.Create(tx, CreateEntry(key, value.Serialize()));
                    return true;
                });
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool Set<T>(string key, T value)
        {
            return driver.WriteTxQuery(tx =>
            {
                if (!repository.Exists(tx, key))
                {
                    repository.Create(tx, CreateEntry(key, value.Serialize()));
                }
                else
                {
                    repository.Update(tx, key, value.Serialize(), DateTime.UtcNow);
                }

                return true;
            });
        }

        public bool Replace<T>(string key, T value)
        {
            return driver.WriteTxQuery(tx =>
            {
                if (!repository.Exists(tx, key)) return false;
                
                repository.Update(tx, key, value.Serialize(), DateTime.UtcNow);

                return true;
            });
        }

        public bool Add<T>(string key, T value, DateTime expiresAt)
        {
            try
            {
                return driver.WriteTxQuery(tx =>
                {
                    repository.Create(tx, CreateEntry(key, value.Serialize(), expiresAt));
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
            return driver.WriteTxQuery(tx =>
            {
                if (!repository.Exists(tx, key))
                {
                    repository.Create(tx, CreateEntry(key, value.Serialize(), expiresAt));
                }
                else
                {
                    repository.Update(tx, key, value.Serialize(), DateTime.UtcNow);
                }

                return true;
            });
        }

        public bool Replace<T>(string key, T value, DateTime expiresAt)
        {
            return driver.WriteTxQuery(tx =>
            {
                if (!repository.Exists(tx, key)) return false;
                
                repository.Update(tx, key, value.Serialize(), DateTime.UtcNow, expiresAt);

                return true;
            });
        }

        public bool Add<T>(string key, T value, TimeSpan expiresIn)
        {
            try
            {
                return driver.WriteTxQuery(tx =>
                {
                    repository.Create(tx, CreateEntry(key, value.Serialize(), DateTime.UtcNow.Add(expiresIn)));
                    return true;
                });
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool Set<T>(string key, T value, TimeSpan expiresIn)
        {
            return driver.WriteTxQuery(tx =>
            {
                var expiresAt = DateTime.UtcNow.Add(expiresIn);
                if (!repository.Exists(tx, key))
                {
                    repository.Create(tx, CreateEntry(key, value.Serialize(), expiresAt));
                }
                else
                {
                    repository.Update(tx, key, value.Serialize(), DateTime.UtcNow, expiresAt);
                }

                return true;
            });
        }

        public bool Replace<T>(string key, T value, TimeSpan expiresIn)
        {
            return driver.WriteTxQuery(tx =>
            {
                if (!repository.Exists(tx, key)) return false;
                
                repository.Update(tx, key, value.Serialize(), DateTime.UtcNow, DateTime.UtcNow.Add(expiresIn));

                return true;
            });
        }

        public void FlushAll()
        {
            driver.WriteTxQuery(tx => repository.FlushAll(tx));
       }

        public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys)
        {
            return driver.WriteTxQuery(tx =>
            {
                var keyList = keys.ToList();
                var cacheEntries = repository.GetCacheEntries<TCacheEntry>(tx, keyList);
                var verifiedCacheEntries = Verify(tx, cacheEntries);
                
                var map = new Dictionary<string, T>();
                verifiedCacheEntries.Each(c => map[c.Key] = c.Value.Deserialize<T>());

                foreach (var key in keyList.Where(key => !map.ContainsKey(key)))
                {
                    map[key] = default;
                }
                
                return map;
            });
        }

        public void SetAll<T>(IDictionary<string, T> values)
        {
            var cacheEntries = values.Select(entry => 
                CreateEntry(entry.Key, entry.Value.Serialize()));

            driver.WriteTxQuery(tx => repository.Create(tx, cacheEntries));
        }

        public void InitSchema()
        {
            driver.WriteTxQuery(tx => repository.InitSchema(tx));
        }

        public TimeSpan? GetTimeToLive(string key)
        {
            return driver.ReadTxQuery(tx =>
            {
                var cacheEntry = repository.GetCacheEntry<TCacheEntry>(tx, key);
                if (cacheEntry == null)
                    return null;

                if (cacheEntry.ExpiryDate == null)
                    return TimeSpan.MaxValue;

                return cacheEntry.ExpiryDate - DateTime.UtcNow;
            });
        }

        public void RemoveByPattern(string pattern)
        {
            driver.WriteTxQuery(tx => repository.RemoveByPattern(tx, pattern));
        }

        public IEnumerable<string> GetKeysByPattern(string pattern)
        {
            return driver.ReadTxQuery(tx => repository.GetKeysByPattern(tx, pattern));
        }

        public void RemoveByRegex(string regex)
        {
            driver.WriteTxQuery(tx => repository.RemoveByRegex(tx, regex));
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

        private TCacheEntry Verify(ITransaction tx, TCacheEntry entry)
        {
            var utcNow = DateTime.UtcNow;
            if (entry != null 
                && entry.ExpiryDate != null 
                && utcNow > entry.ExpiryDate)
            {
                repository.RemoveExpired(tx, utcNow);
                return default;
            }
            return entry;
        }
        
        private Dictionary<string, TCacheEntry> Verify(ITransaction tx, Dictionary<string, TCacheEntry> entries)
        {
            var utcNow = DateTime.UtcNow;
            if (entries.TryRemoveAll(c => c.ExpiryDate != null && utcNow > c.ExpiryDate))
            {
                repository.RemoveExpired(tx, utcNow);
            }

            return entries;
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

    internal static class DictionaryExtensions
    {
        public static bool TryRemoveAll<TKey, TValue>(this IDictionary<TKey, TValue> dict, 
            Func<TValue, bool> predicate)
        {
            var keys = dict.Keys.Where(k => predicate(dict[k])).ToList();
            foreach (var key in keys)
            {
                dict.Remove(key);
            }

            return keys.Any();
        }
    }

    internal static class DriverExtensions
    {
        public static T ReadTxQuery<T>(this IDriver driver, Func<ITransaction, T> work)
        {
            using (var session = driver.Session())
            {
                return session.ReadTransaction(work);
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
    internal class Neo4jCacheRepository
    {
        private static class Label
        {
            public const string CacheEntry = nameof(CacheEntry);    
        }

        private static class Query
        {
            public static string Constraint => $@"
                CREATE CONSTRAINT ON (item:{Label.CacheEntry}) ASSERT item.Id IS UNIQUE";

            public static string Index => $@"
                CREATE INDEX ON :{Label.CacheEntry}(ExpiryDate)";

            public static string Exists => $@"
                MATCH (item:{Label.CacheEntry} {{Id: $key}})
                RETURN item IS NOT NULL";

            public static string Create => $@"
                CREATE (item:{Label.CacheEntry} {{ $item }})";

            public static string CreateAll => $@"
                UNWIND $items AS item
                CREATE (:{Label.CacheEntry} {{ item }})";

            public static string GetByKey => $@"
                MATCH (item:{Label.CacheEntry} {{Id: $key}})
                RETURN item";
            
            public static string GetByKeys => $@"
                MATCH (item:{Label.CacheEntry})
                WHERE item.Id IN $keys
                RETURN item";

            public static string GetKeysByPattern => $@"
                MATCH (item:{Label.CacheEntry})
                WHERE item.Id LIKE $pattern
                RETURN item.Id";
            
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
                WHERE item.Id IN $keys
                DELETE item";

            public static string DeleteByPattern => $@"
                MATCH (item:{Label.CacheEntry})
                WHERE item.Id LIKE $pattern
                DELETE item";

            public static string DeleteByRegex => $@"
                MATCH (item:{Label.CacheEntry})
                WHERE item.Id =~ $regex
                DELETE item";

            public static string DeleteExpired => $@"
                MATCH (item:{Label.CacheEntry})
                WHERE $now > item.ExpiryDate
                DELETE item";
            
            public static string DeleteAll => $@"
                MATCH (item:{Label.CacheEntry})
                DELETE item";
        }

        public bool Exists(ITransaction tx, string key)
        {
            var parameters = new { key };

            var result = tx.Run(Query.Exists, parameters);
            return result.Truthy();
        }
        
        public void Create<TCacheEntry>(ITransaction tx, TCacheEntry entry)
            where TCacheEntry : ICacheEntry, new()
        {
            var parameters = new
            {
                item = entry.ConvertTo<Dictionary<string, object>>()
            };

            tx.Run(Query.Create, parameters);
        }

        public void Create<TCacheEntry>(ITransaction tx, IEnumerable<TCacheEntry> entries)
            where TCacheEntry : ICacheEntry, new()
        {
            var parameters = new
            {
                items = entries.Select(p => p.ConvertTo<Dictionary<string, object>>())
            };

            tx.Run(Query.CreateAll, parameters);
        }

        public void Update<TCacheEntry>(ITransaction tx, TCacheEntry entry)
            where TCacheEntry : ICacheEntry, new()
        {
            var parameters = new
            {
                item = entry.ConvertTo<Dictionary<string, object>>()
            };

            tx.Run(Query.Update, parameters);
        }

        public void Update(ITransaction tx, string key, string data, DateTime modifiedDate)
        {
            var parameters = new
            {
                key,
                data,
                modifiedDate = new ZonedDateTime(modifiedDate),
            };

            tx.Run(Query.UpdateData, parameters);
        }

        public void Update(ITransaction tx, string key, string data, DateTime modifiedDate, DateTime expiresAt)
        {
            var parameters = new
            {
                key,
                data,
                modifiedDate = new ZonedDateTime(modifiedDate),
                expiresAt = new ZonedDateTime(expiresAt)
            };

            tx.Run(Query.UpdateDataWithExpiry, parameters);
        }

        public bool Remove(ITransaction tx, string key)
        {
            var parameters = new { key };

            var result = tx.Run(Query.DeleteByKey, parameters);
            return result.Truthy();
        }

        public void RemoveAll(ITransaction tx, IEnumerable<string> keys)
        {
            var parameters = new { keys };

            tx.Run(Query.DeleteByKeys, parameters);
        }

        public void FlushAll(ITransaction tx)
        {
            tx.Run(Query.DeleteAll);
        }

        public void RemoveExpired(ITransaction tx, DateTime expiredAt)
        {
            var parameters = new {now = new ZonedDateTime(expiredAt)};

            tx.Run(Query.DeleteExpired, parameters);
        }

        public TCacheEntry GetCacheEntry<TCacheEntry>(ITransaction tx, string key)
            where TCacheEntry : ICacheEntry, new()
        {
            var parameters = new { key };

            var result = tx.Run(Query.GetByKey, parameters);
            
            return result.Map<TCacheEntry>().SingleOrDefault();
        }

        public Dictionary<string, TCacheEntry> GetCacheEntries<TCacheEntry>(ITransaction tx, IEnumerable<string> keys)
            where TCacheEntry : ICacheEntry, new()
        {
            var parameters = new { keys };

            var result = tx.Run(Query.GetByKeys, parameters);
            
            return result.MapDictionary<TCacheEntry>();
        }

        public void InitSchema(ITransaction tx)
        {
            tx.Run(Query.Constraint);
            tx.Run(Query.Index);
        }

        public IEnumerable<string> GetKeysByPattern(ITransaction tx, string pattern)
        {
            var parameters = new { pattern };

            var result = tx.Run(Query.GetKeysByPattern, parameters);

            return result.Map<string>();
        }

        public void RemoveByPattern(ITransaction tx, string pattern)
        {
            var parameters = new { pattern };

            tx.Run(Query.DeleteByPattern, parameters);
        }

        public void RemoveByRegex(ITransaction tx, string regex)
        {
            var parameters = new { regex };

            tx.Run(Query.DeleteByRegex, parameters);
        }
    }
}
