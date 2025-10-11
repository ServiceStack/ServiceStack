using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ServiceStack;

public static class DictionaryExtensions
{
    public static TValue GetValueOrDefault<TValue, TKey>(this Dictionary<TKey, TValue> dictionary, TKey key)
    {
        return dictionary.TryGetValue(key, out var value) ? value : default;
    }
    
    public static TValue GetValue<TValue, TKey>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TValue> defaultValue)
    {
        return dictionary.TryGetValue(key, out var value) ? value : defaultValue();
    }
    
    public static bool TryGetValue<T>(this Dictionary<string, object> dictionary, string key, out T value)
    {
        if (dictionary.TryGetValue(key, out var objValue))
        {
            if (objValue is T theValue)
            {
                value = theValue;
                return true;
            }
            if (typeof(T).IsNumericType())
            {
                try
                {
                    value = objValue.ConvertTo<T>();
                    return true;
                }
                catch (Exception)
                {
                    // ignore
                }
            }
        }
        value = default;
        return false;
    }
    
    public static bool TryGetObject(this Dictionary<string, object> dictionary, string key, out Dictionary<string, object> value)
    {
        return TryGetValue(dictionary, key, out value);
    }
    
    public static bool TryGetList(this Dictionary<string, object> dictionary, string key, out List<object> value)
    {
        return TryGetValue(dictionary, key, out value);
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
            if (!otherMap.TryGetValue(entry.Key, out var otherValue)) return false;
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

    public static Dictionary<TKey, TValue>ToDictionary<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> map) => new(map);

    public static ConcurrentDictionary<TKey, TValue> ToConcurrentDictionary<TKey, TValue>(this IDictionary<TKey, TValue> from) => new(from);
    
    public static bool TryRemove<TKey, TValue>(this Dictionary<TKey, TValue> map, TKey key, out TValue value)
    {
        lock (map)
        {
            if (!map.TryGetValue(key, out value)) return false;
            map.Remove(key);
            return true;
        }
    }
    
    public static Dictionary<TKey, TValue> RemoveKey<TKey, TValue>(this Dictionary<TKey, TValue> map, TKey key)
    {
        map?.Remove(key);
        return map;
    }
    
    public static Dictionary<TKey, TValue> MoveKey<TKey, TValue>(this Dictionary<TKey, TValue> map, TKey oldKey, TKey newKey, Func<TValue, TValue> valueFilter=null)
    {
        if (map == null)
            return null;
            
        if (map.TryGetValue(oldKey, out var value))
            map[newKey] = valueFilter != null ? valueFilter(value) : value;
            
        map.Remove(oldKey);
        return map;
    }
    
    public static Dictionary<TKey, TValue> Merge<TKey, TValue>(this IDictionary<TKey, TValue> initial,
        params IEnumerable<KeyValuePair<TKey,TValue>>[] withSources)
    {
        var to = new Dictionary<TKey, TValue>(initial);
        foreach (var kvps in withSources)
        {
            foreach (var kvp in kvps)
            {
                to[kvp.Key] = kvp.Value;
            }
        }
        return to;
    }

    public static IEnumerable<TElement> ValuesWithoutLock<TKey, TElement>(this ConcurrentDictionary<TKey, TElement> source)
    {
        foreach (var item in source)
        {
            if (item.Value != null)
                yield return item.Value;
        }
    }

    public static IEnumerable<TKey> KeysWithoutLock<TKey, TElement>(this ConcurrentDictionary<TKey, TElement> source)
    {
        foreach (var item in source)
        {
            yield return item.Key;
        }
    }

}