using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver.V1;

namespace ServiceStack.Caching.Neo4j
{
    internal static class RecordExtensions
    {
        public static IEnumerable<TCacheEntry> Map<TCacheEntry>(
            this IEnumerable<IRecord> records)
            where TCacheEntry : ICacheEntry, new()
        {
            return records.Select(record => ((IEntity) record[0]).Map<TCacheEntry>());
        }

        public static Dictionary<string, TCacheEntry> MapDictionary<TCacheEntry>(
            this IEnumerable<IRecord> records)
            where TCacheEntry : ICacheEntry, new()
        {
            var dict = new Dictionary<string, TCacheEntry>();
            foreach (var record in records)
            {
                var value = ((IEntity) record[0]).Map<TCacheEntry>();
                var key = value.Id;
                dict.Add(key, value);
            }

            return dict;
        }

        public static TCacheEntry Map<TCacheEntry>(this IEntity entity)
            where TCacheEntry : ICacheEntry, new()
        {
            return entity.Properties.FromObjectDictionary<TCacheEntry>();
        }

        public static bool Truthy(this IEnumerable<IRecord> records)
        {
            var record = records.SingleOrDefault();
            return record != null && record[0].As<bool>();
        }
    }
}