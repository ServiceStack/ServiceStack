using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ServiceStack.Auth;
using ServiceStack.OrmLite;

namespace ServiceStack.Caching
{
    public class OrmLiteCacheClient : RepositoryBase, ICacheClient, IRequiresSchema
    {
        CacheEntry CreateEntry(string id, string data=null, 
            DateTime? created=null, DateTime? expires=null)
        {
            var createdDate = created ?? DateTime.UtcNow;
            return new CacheEntry {
                Id = id,
                Data = data,
                ExpiryDate = expires,
                CreatedDate = createdDate,
                ModifiedDate = createdDate,
            };
        }

        public bool Remove(string key)
        {
            return Db.DeleteById<CacheEntry>(key) > 0;
        }

        public void RemoveAll(IEnumerable<string> keys)
        {
            Db.DeleteByIds<CacheEntry>(keys);
        }

        public T Get<T>(string key)
        {
            var cache = Verify(Db.SingleById<CacheEntry>(key));
            return cache == null 
                ? default(T) 
                : Db.Deserialize<T>(cache.Data);
        }

        public long Increment(string key, uint amount)
        {
            long nextVal;
            using (var dbTrans = Db.OpenTransaction(IsolationLevel.ReadCommitted))
            {
                var cache = Verify(Db.SingleById<CacheEntry>(key));

                if (cache == null)
                {
                    nextVal = amount;
                    Db.Insert(CreateEntry(key, nextVal.ToString()));
                }
                else
                {
                    nextVal = long.Parse(cache.Data) + amount;
                    cache.Data = nextVal.ToString();

                    Db.Update(cache);
                }

                dbTrans.Commit();
            }

            return nextVal;
        }

        public long Decrement(string key, uint amount)
        {
            long nextVal;
            using (var dbTrans = Db.OpenTransaction(IsolationLevel.ReadCommitted))
            {
                var cache = Verify(Db.SingleById<CacheEntry>(key));

                if (cache == null)
                {
                    nextVal = -amount;
                    Db.Insert(CreateEntry(key, nextVal.ToString()));
                }
                else
                {
                    nextVal = long.Parse(cache.Data) - amount;
                    cache.Data = nextVal.ToString();

                    Db.Update(cache);
                }

                dbTrans.Commit();
            }

            return nextVal;
        }

        public bool Add<T>(string key, T value)
        {
            try
            {
                Db.Insert(CreateEntry(key, Db.Serialize(value)));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool Set<T>(string key, T value)
        {
            var exists = Db.UpdateOnly(new CacheEntry
                {
                    Id = key,
                    Data = Db.Serialize(value),
                    ModifiedDate = DateTime.UtcNow,
                },
                q => new { q.Data, q.ModifiedDate },
                q => q.Id == key) == 1;

            if (!exists)
            {
                Db.Insert(CreateEntry(key, Db.Serialize(value)));
            }

            return true;
        }

        public bool Replace<T>(string key, T value)
        {
            var exists = Db.UpdateOnly(new CacheEntry
                {
                    Id = key,
                    Data = Db.Serialize(value),
                    ModifiedDate = DateTime.UtcNow,
                },
                q => new { q.Data, q.ModifiedDate },
                q => q.Id == key) == 1;

            if (!exists)
            {
                Db.Insert(CreateEntry(key, Db.Serialize(value)));
            }

            return true;
        }

        public bool Add<T>(string key, T value, DateTime expiresAt)
        {
            try
            {
                Db.Insert(CreateEntry(key, Db.Serialize(value), expires:expiresAt));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool Set<T>(string key, T value, DateTime expiresAt)
        {
            var exists = Db.UpdateOnly(new CacheEntry
                {
                    Id = key,
                    Data = Db.Serialize(value),
                    ExpiryDate = expiresAt,
                    ModifiedDate = DateTime.UtcNow,
                },
                q => new { q.Data, ExpiredDate = q.ExpiryDate, q.ModifiedDate },
                q => q.Id == key) == 1;

            if (!exists)
            {
                Db.Insert(CreateEntry(key, Db.Serialize(value), expires:expiresAt));
            }

            return true;
        }

        public bool Replace<T>(string key, T value, DateTime expiresAt)
        {
            var exists = Db.UpdateOnly(new CacheEntry
                {
                    Id = key,
                    Data = Db.Serialize(value),
                    ExpiryDate = expiresAt,
                    ModifiedDate = DateTime.UtcNow,
                },
                q => new { q.Data, ExpiredDate = q.ExpiryDate, q.ModifiedDate },
                q => q.Id == key) == 1;

            if (!exists)
            {
                Db.Insert(CreateEntry(key, Db.Serialize(value), expires: expiresAt));
            }

            return true;
        }

        public bool Add<T>(string key, T value, TimeSpan expiresIn)
        {
            try
            {
                Db.Insert(CreateEntry(key, Db.Serialize(value), 
                    expires: DateTime.UtcNow.Add(expiresIn)));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool Set<T>(string key, T value, TimeSpan expiresIn)
        {
            var exists = Db.UpdateOnly(new CacheEntry
                {
                    Id = key,
                    Data = Db.Serialize(value),
                    ExpiryDate = DateTime.UtcNow.Add(expiresIn),
                    ModifiedDate = DateTime.UtcNow,
                },
                q => new { q.Data, ExpiredDate = q.ExpiryDate, q.ModifiedDate },
                q => q.Id == key) == 1;

            if (!exists)
            {
                Db.Insert(CreateEntry(key, Db.Serialize(value), expires: DateTime.UtcNow.Add(expiresIn)));
            }

            return true;
        }

        public bool Replace<T>(string key, T value, TimeSpan expiresIn)
        {
            var exists = Db.UpdateOnly(new CacheEntry
            {
                Id = key,
                Data = Db.Serialize(value),
                ExpiryDate = DateTime.UtcNow.Add(expiresIn),
                ModifiedDate = DateTime.UtcNow,
            },
                q => new { q.Data, ExpiredDate = q.ExpiryDate, q.ModifiedDate },
                q => q.Id == key) == 1;

            if (!exists)
            {
                Db.Insert(CreateEntry(key, Db.Serialize(value), expires: DateTime.UtcNow.Add(expiresIn)));
            }

            return true;
        }

        public void FlushAll()
        {
            Db.DeleteAll<CacheEntry>();
        }

        public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys)
        {
            var results = Verify(Db.SelectByIds<CacheEntry>(keys));
            var map = new Dictionary<string, T>();

            results.Each(x => 
                map[x.Id] = Db.Deserialize<T>(x.Data));

            foreach (var key in keys)
            {
                if (!map.ContainsKey(key))
                    map[key] = default(T);
            }

            return map;
        }

        public void SetAll<T>(IDictionary<string, T> values)
        {
            var rows = values.Select(entry => 
                CreateEntry(entry.Key, Db.Serialize(entry.Value)))
                .ToList();

            Db.InsertAll(rows);
        }

        public void InitSchema()
        {
            Db.CreateTableIfNotExists<CacheEntry>();
        }

        public List<CacheEntry> Verify(IEnumerable<CacheEntry> entries)
        {
            var results = entries.ToList();
            var expired = results.RemoveAll(x => x.ExpiryDate != null && DateTime.UtcNow > x.ExpiryDate);
            if (expired > 0)
            {
                Db.Delete<CacheEntry>(q => q.ExpiryDate > DateTime.UtcNow);
            }

            return results;
        }

        public CacheEntry Verify(CacheEntry entry)
        {
            if (entry != null &&
                entry.ExpiryDate != null && DateTime.UtcNow > entry.ExpiryDate)
            {
                Db.Delete<CacheEntry>(q => DateTime.UtcNow > q.ExpiryDate);
                return null;
            }
            return entry;
        }
    }

    public class CacheEntry
    {
        public string Id { get; set; }
        public string Data { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
    }

    public static class CacheExtensions
    {
        public static void InitSchema(this ICacheClient cache)
        {
            var requiresSchema = cache as IRequiresSchema;
            if (requiresSchema != null)
            {
                requiresSchema.InitSchema();
            }
        }

        public static string Serialize<T>(this IDbConnection db, T value)
        {
            return db.GetDialectProvider().StringSerializer.SerializeToString(value);
        }

        public static T Deserialize<T>(this IDbConnection db, string text)
        {
            return text == null 
                ? default(T) 
                : db.GetDialectProvider().StringSerializer.DeserializeFromString<T>(text);
        }
    }
}