using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceStack.Caching.Neo4j
{
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
}