using System;
using System.Collections.Generic;

using Proxy = ServiceStack.Common.DictionaryExtensions;

namespace ServiceStack.Common.Extensions
{
    [Obsolete("Use ServiceStack.Common.DictionaryExtensions")]
    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TValue, TKey>(this Dictionary<TKey, TValue> dictionary, TKey key)
        {
            return Proxy.GetValueOrDefault(dictionary, key);
        }

        public static void ForEach<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, Action<TKey, TValue> onEachFn)
        {
            Proxy.ForEach(dictionary, onEachFn);
        }

        public static bool EquivalentTo<K, V>(this IDictionary<K, V> thisMap, IDictionary<K, V> otherMap)
        {
            return Proxy.EquivalentTo(thisMap, otherMap);
        }
         
        public static List<T> ConvertAll<T, K, V>(IDictionary<K, V> map, Func<K, V, T> createFn)
        {
            return Proxy.ConvertAll(map, createFn);
        }
    }
}