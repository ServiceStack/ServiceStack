using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ServiceStack;

public static class DictionaryExtensions
{
    public static TValue GetValueOrDefault<TValue, TKey>(this Dictionary<TKey, TValue> dictionary, TKey key)
    {
        return dictionary.ContainsKey(key) ? dictionary[key] : default(TValue);
    }

    public static TValue GetValue<TValue, TKey>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TValue> defaultValue)
    {
        return dictionary.ContainsKey(key) ? dictionary[key] : defaultValue();
    }

    public static bool IsNullOrEmpty(this IDictionary dictionary)
    {
        return dictionary == null || dictionary.Count == 0;
    }

    public static void ForEach<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, Action<TKey, TValue> onEachFn)
    {
        foreach (var entry in dictionary)
        {
            onEachFn(entry.Key, entry.Value);
        }
    }

    public static bool UnorderedEquivalentTo<K, V>(this IDictionary<K, V> thisMap, IDictionary<K, V> otherMap)
    {
        if (thisMap == null || otherMap == null) return thisMap == otherMap;
        if (thisMap.Count != otherMap.Count) return false;

        foreach (var entry in thisMap)
        {
            V otherValue;
            if (!otherMap.TryGetValue(entry.Key, out otherValue)) return false;
            if (!Equals(entry.Value, otherValue)) return false;
        }

        return true;
    }

    public static List<T> ConvertAll<T, K, V>(IDictionary<K, V> map, Func<K, V, T> createFn)
    {
        var list = new List<T>();
        map.Each((kvp) => list.Add(createFn(kvp.Key, kvp.Value)));
        return list;
    }

    public static V GetOrAdd<K, V>(this Dictionary<K, V> map, K key, Func<K, V> createFn)
    {
        //simulate ConcurrentDictionary.GetOrAdd
        lock (map)
        {
            V val;
            if (!map.TryGetValue(key, out val))
                map[key] = val = createFn(key);

            return val;
        }
    }

    public static KeyValuePair<TKey, TValue> PairWith<TKey, TValue>(this TKey key, TValue value)
    {
        return new KeyValuePair<TKey, TValue>(key, value);
    }

    public static ConcurrentDictionary<TKey, TValue> ToConcurrentDictionary<TKey, TValue>(this IDictionary<TKey, TValue> from)
    {
        var to = new ConcurrentDictionary<TKey, TValue>();
        foreach (var entry in from)
        {
            to[entry.Key] = entry.Value;
        }
        return to;
    } 
}