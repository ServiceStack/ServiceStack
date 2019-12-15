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

            InitMappers();
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
            var verifiedCacheItem = driver.WriteTxQuery(tx =>
            {
                var cacheItem = repository.GetCacheEntry<TCacheEntry>(tx, key);
                return Verify(tx, cacheItem);
            });

            return verifiedCacheItem.Deserialize<T>();
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
                driver.WriteTxQuery(tx =>
                {
                    repository.Create(tx, CreateEntry(key, value.Serialize()));
                });
                
                return true;
            }
            catch (ClientException)
            {
                return false;
            }
        }

        public bool Set<T>(string key, T value)
        {
            try
            {
                driver.WriteTxQuery(tx =>
                {
                    if (!repository.Exists(tx, key))
                    {
                        repository.Create(tx, CreateEntry(key, value.Serialize()));
                    }
                    else
                    {
                        repository.Update(tx, key, value.Serialize(), DateTime.UtcNow);
                    }
                });
                
                return true;
            }
            catch (ClientException)
            {
                return false;
            }
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
                driver.WriteTxQuery(tx =>
                {
                    repository.Create(tx, CreateEntry(key, value.Serialize(), DateTime.UtcNow, expiresAt));
                });
                
                return true;
            }
            catch (ClientException)
            {
                return false;
            }
        }

        public bool Set<T>(string key, T value, DateTime expiresAt)
        {
            try
            {
                driver.WriteTxQuery(tx =>
                {
                    if (!repository.Exists(tx, key))
                    {
                        repository.Create(tx, CreateEntry(key, value.Serialize(), DateTime.UtcNow, expiresAt));
                    }
                    else
                    {
                        repository.Update(tx, key, value.Serialize(), DateTime.UtcNow, expiresAt);
                    }
                });
                
                return true;
            }
            catch (ClientException)
            {
                return false;
            }
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
                var utcNow = DateTime.UtcNow;
                driver.WriteTxQuery(tx =>
                {
                    repository.Create(tx, CreateEntry(key, value.Serialize(), utcNow, utcNow.Add(expiresIn)));
                });
                
                return true;
            }
            catch (ClientException)
            {
                return false;
            }
        }

        public bool Set<T>(string key, T value, TimeSpan expiresIn)
        {
            try
            {
                driver.WriteTxQuery(tx =>
                {
                    var utcNow = DateTime.UtcNow;
                    var expiresAt = utcNow.Add(expiresIn);
                    if (!repository.Exists(tx, key))
                    {
                        repository.Create(tx, CreateEntry(key, value.Serialize(), utcNow, expiresAt));
                    }
                    else
                    {
                        repository.Update(tx, key, value.Serialize(), utcNow, expiresAt);
                    }
                });

                return true;
            }
            catch (ClientException)
            {
                return false;
            }
        }

        public bool Replace<T>(string key, T value, TimeSpan expiresIn)
        {
            return driver.WriteTxQuery(tx =>
            {
                if (!repository.Exists(tx, key)) return false;
                
                var utcNow = DateTime.UtcNow;
                repository.Update(tx, key, value.Serialize(), utcNow, utcNow.Add(expiresIn));

                return true;
            });
        }

        public void FlushAll()
        {
            driver.WriteTxQuery(tx => repository.FlushAll(tx));
        }

        public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys)
        {
            var keyList = keys.ToList();

            var verifiedCacheEntries = driver.WriteTxQuery(tx =>
            {
                var cacheEntries = repository.GetCacheEntries<TCacheEntry>(tx, keyList);
                return Verify(tx, cacheEntries);
            });
                
            var map = new Dictionary<string, T>();
            verifiedCacheEntries.Each(c => map[c.Key] = c.Value.Deserialize<T>());

            foreach (var key in keyList.Where(key => !map.ContainsKey(key)))
            {
                map[key] = default;
            }
                
            return map;

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
            var utcNow = created ?? DateTime.UtcNow;
            return new TCacheEntry
            {
                Id = id,
                Data = data,
                ExpiryDate = expires,
                CreatedDate = utcNow,
                ModifiedDate = utcNow,
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
        
        public static void InitMappers()
        {
            AutoMapping.RegisterConverter<ZonedDateTime, DateTime>(zonedDateTime => zonedDateTime.ToDateTimeOffset().DateTime);
            AutoMapping.RegisterConverter<ZonedDateTime, DateTime?>(zonedDateTime => zonedDateTime.ToDateTimeOffset().DateTime);
        }
    }
}
