using System;
using System.Collections.Generic;
using System.Text;
using ServiceStack.Text;

namespace ServiceStack.Common.Extensions
{
	public static class DictionaryExtensions
	{
		public static TValue GetValueOrDefault<TValue, TKey>(this Dictionary<TKey, TValue> dictionary, TKey key)
		{
			return dictionary.ContainsKey(key) ? dictionary[key] : default(TValue);
		}

		public static void ForEach<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, Action<TKey, TValue> onEachFn)
		{
			foreach (var entry in dictionary)
			{
				onEachFn(entry.Key, entry.Value);
			}
		}

		public static bool EquivalentTo<K, V>(this IDictionary<K, V> thisMap, IDictionary<K, V> otherMap)
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
	}
}