using System;
using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver.V1;

namespace ServiceStack.Caching.Neo4j
{
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
                CREATE (item:{Label.CacheEntry} $item)";

            public static string CreateAll => $@"
                UNWIND $items AS newItem
                CREATE (item:{Label.CacheEntry})
                SET item = newItem";

            public static string GetByKey => $@"
                MATCH (item:{Label.CacheEntry} {{Id: $key}})
                RETURN item";
            
            public static string GetByKeys => $@"
                MATCH (item:{Label.CacheEntry})
                WHERE item.Id IN $keys
                RETURN item";

            public static string GetAllKeys => $@"
                MATCH (item:{Label.CacheEntry})
                RETURN item.Id";

            public static string GetKeysByContainsPattern => $@"
                MATCH (item:{Label.CacheEntry})
                WHERE item.Id CONTAINS $pattern
                RETURN item.Id";
            
            public static string GetKeysByStartsWithPattern => $@"
                MATCH (item:{Label.CacheEntry})
                WHERE item.Id STARTS WITH $pattern
                RETURN item.Id";

            public static string GetKeysByEndsWithPattern => $@"
                MATCH (item:{Label.CacheEntry})
                WHERE item.Id STARTS WITH $pattern
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
                SET item.ExpiryDate = $expiresAt";

            public static string DeleteByKey => $@"
                MATCH (item:{Label.CacheEntry} {{Id: $key}})
                DELETE item
                RETURN item IS NOT NULL";

            public static string DeleteByKeys => $@"
                MATCH (item:{Label.CacheEntry})
                WHERE item.Id IN $keys
                DELETE item";

            public static string DeleteByContainsPattern => $@"
                MATCH (item:{Label.CacheEntry})
                WHERE item.Id CONTAINS $pattern
                DELETE item";

            public static string DeleteByStartsWithPattern => $@"
                MATCH (item:{Label.CacheEntry})
                WHERE item.Id STARTS WITH $pattern
                DELETE item";

            public static string DeleteByEndsWithPattern => $@"
                MATCH (item:{Label.CacheEntry})
                WHERE item.Id ENDS WITH $pattern
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
            string query;
            var wildcards = pattern.Count(p => p == '*');

            if (pattern == "*")
            {
                query = Query.GetAllKeys;
            }
            else if (pattern.StartsWith("*") && wildcards == 1)
            {
                query = Query.GetKeysByEndsWithPattern;
            }
            else if (pattern.EndsWith("*") && wildcards == 1)
            {
                query = Query.GetKeysByStartsWithPattern;
            }
            else if (pattern.StartsWith("*") && pattern.EndsWith("*") && wildcards == 2)
            {
                query = Query.GetKeysByContainsPattern;
            }
            else
            {
                throw new ArgumentException(@"Cannot retrieve keys with given pattern", "pattern");
            }

            var parameters = new
            {
                pattern = pattern.Replace("*", string.Empty)
            };
            var result = tx.Run(query, parameters);

            return result.Select(r => r[0].As<string>());
        }

        public void RemoveByPattern(ITransaction tx, string pattern)
        {
            string query;
            var wildcards = pattern.Count(p => p == '*');

            if (pattern == "*")
            {
                query = Query.DeleteAll;
            }
            else if (pattern.StartsWith("*") && wildcards == 1)
            {
                query = Query.DeleteByEndsWithPattern;
            }
            else if (pattern.EndsWith("*") && wildcards == 1)
            {
                query = Query.DeleteByStartsWithPattern;
            }
            else if (pattern.StartsWith("*") && pattern.EndsWith("*") && wildcards == 2)
            {
                query = Query.DeleteByContainsPattern;
            }
            else
            {
                throw new ArgumentException(@"Cannot retrieve keys with given pattern", "pattern");
            }

            var parameters = new
            {
                pattern = pattern.Replace("*", string.Empty)
            };
            tx.Run(query, parameters);
        }

        public void RemoveByRegex(ITransaction tx, string regex)
        {
            var parameters = new { regex };

            tx.Run(Query.DeleteByRegex, parameters);
        }
    }
}