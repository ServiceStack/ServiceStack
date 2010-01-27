using System;
using System.Collections.Generic;

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
	}
}