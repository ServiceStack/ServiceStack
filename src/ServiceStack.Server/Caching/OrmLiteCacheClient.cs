using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack.Caching
{
    public class OrmLiteCacheClient : ICacheClient, IRequiresSchema, ICacheClientExtended
    {
        CacheEntry CreateEntry(string id, string data = null,
            DateTime? created = null, DateTime? expires = null)
        {
            var createdDate = created ?? DateTime.UtcNow;
            return new CacheEntry
            {
                Id = id,
                Data = data,
                ExpiryDate = expires,
                CreatedDate = createdDate,
                ModifiedDate = createdDate,
            };
        }

        public IDbConnectionFactory DbFactory { get; set; }

        public T Exec<T>(Func<IDbConnection, T> action)
        {
            using (JsConfig.With(excludeTypeInfo: false))
            using (var db = DbFactory.Open())
            {
                return action(db);
            }
        }

        public void Exec(Action<IDbConnection> action)
        {
            using (JsConfig.With(excludeTypeInfo: false))
            using (var db = DbFactory.Open())
            {
                action(db);
            }
        }

        public bool Remove(string key)
        {
            return Exec(db => db.DeleteById<CacheEntry>(key) > 0);
        }

        public void RemoveAll(IEnumerable<string> keys)
        {
            Exec(db => db.DeleteByIds<CacheEntry>(keys) > 0);
        }

        public T Get<T>(string key)
        {
            return Exec(db =>
            {
                var cache = Verify(db, db.SingleById<CacheEntry>(key));
                return cache == null
                    ? default(T)
                    : db.Deserialize<T>(cache.Data);
            });
        }

        public long Increment(string key, uint amount)
        {
            return Exec(db =>
            {
                long nextVal;
                using (var dbTrans = db.OpenTransaction(IsolationLevel.ReadCommitted))
                {
                    var cache = Verify(db, db.SingleById<CacheEntry>(key));

                    if (cache == null)
                    {
                        nextVal = amount;
                        db.Insert(CreateEntry(key, nextVal.ToString()));
                    }
                    else
                    {
                        nextVal = long.Parse(cache.Data) + amount;
                        cache.Data = nextVal.ToString();

                        db.Update(cache);
                    }

                    dbTrans.Commit();
                }

                return nextVal;
            });
        }

        public long Decrement(string key, uint amount)
        {
            return Exec(db =>
            {
                long nextVal;
                using (var dbTrans = db.OpenTransaction(IsolationLevel.ReadCommitted))
                {
                    var cache = Verify(db, db.SingleById<CacheEntry>(key));

                    if (cache == null)
                    {
                        nextVal = -amount;
                        db.Insert(CreateEntry(key, nextVal.ToString()));
                    }
                    else
                    {
                        nextVal = long.Parse(cache.Data) - amount;
                        cache.Data = nextVal.ToString();

                        db.Update(cache);
                    }

                    dbTrans.Commit();
                }

                return nextVal;
            });
        }

        public bool Add<T>(string key, T value)
        {
            try
            {
                Exec(db =>
                {
                    db.Insert(CreateEntry(key, db.Serialize(value)));
                });
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool UpdateIfExists<T>(IDbConnection db, string key, T value)
        {
            var exists = db.UpdateOnly(new CacheEntry
                {
                    Id = key,
                    Data = db.Serialize(value),
                    ModifiedDate = DateTime.UtcNow,
                },
                onlyFields: q => new { q.Data, q.ModifiedDate },
                @where: q => q.Id == key) == 1;

            return exists;
        }

        private static bool UpdateIfExists<T>(IDbConnection db, string key, T value, DateTime expiresAt)
        {
            var exists = db.UpdateOnly(new CacheEntry
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

        public bool Set<T>(string key, T value)
        {
            return Exec(db =>
            {
                var exists = UpdateIfExists(db, key, value);

                if (!exists)
                {
                    try
                    {
                        db.Insert(CreateEntry(key, db.Serialize(value)));
                    }
                    catch (Exception)
                    {
                        exists = UpdateIfExists(db, key, value);
                        if (!exists) throw;
                    }
                }

                return true;
            });
        }

        public bool Replace<T>(string key, T value)
        {
            return Exec(db =>
            {
                var exists = db.UpdateOnly(new CacheEntry
                    {
                        Id = key,
                        Data = db.Serialize(value),
                        ModifiedDate = DateTime.UtcNow,
                    },
                    onlyFields: q => new { q.Data, q.ModifiedDate },
                    where: q => q.Id == key) == 1;

                if (!exists)
                {
                    db.Insert(CreateEntry(key, db.Serialize(value)));
                }

                return true;
            });
        }

        public bool Add<T>(string key, T value, DateTime expiresAt)
        {
            try
            {
                Exec(db =>
                {
                    db.Insert(CreateEntry(key, db.Serialize(value), expires: expiresAt));
                });
                return true;
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
                var exists = db.UpdateOnly(new CacheEntry
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
                var exists = db.UpdateOnly(new CacheEntry
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
                db.DeleteAll<CacheEntry>();
            });
        }

        public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys)
        {
            return Exec(db =>
            {
                var results = Verify(db, db.SelectByIds<CacheEntry>(keys));
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
                    CreateEntry(entry.Key, db.Serialize(entry.Value)))
                    .ToList();

                db.InsertAll(rows);
            });
        }

        public void InitSchema()
        {
            Exec(db => db.CreateTableIfNotExists<CacheEntry>());
        }

        public List<CacheEntry> Verify(IDbConnection db, IEnumerable<CacheEntry> entries)
        {
            var results = entries.ToList();
            var expired = results.RemoveAll(x => x.ExpiryDate != null && DateTime.UtcNow > x.ExpiryDate);
            if (expired > 0)
            {
                db.Delete<CacheEntry>(q => DateTime.UtcNow > q.ExpiryDate);
            }

            return results;
        }

        public CacheEntry Verify(IDbConnection db, CacheEntry entry)
        {
            if (entry != null &&
                entry.ExpiryDate != null && DateTime.UtcNow > entry.ExpiryDate)
            {
                db.Delete<CacheEntry>(q => DateTime.UtcNow > q.ExpiryDate);
                return null;
            }
            return entry;
        }

        public TimeSpan? GetTimeToLive(string key)
        {
            return Exec(db =>
            {
                var cache = db.SingleById<CacheEntry>(key);
                if (cache == null)
                    return null;

                if (cache.ExpiryDate == null)
                    return TimeSpan.MaxValue;

                return cache.ExpiryDate - DateTime.UtcNow;
            });
        }

        public void Dispose() { }
    }

    public class CacheEntry
    {
        public string Id { get; set; }
        [StringLength(StringLengthAttribute.MaxText)]
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