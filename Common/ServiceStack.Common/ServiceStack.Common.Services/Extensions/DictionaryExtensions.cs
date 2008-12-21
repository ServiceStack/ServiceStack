using System.Collections.Generic;

namespace ServiceStack.Common.Services.Extensions
{
    public static class DictionaryExtensions
    {
        public static Value GetValueOrDefault<Value,Key>(this Dictionary<Key,Value> dictionary, Key key)
        {
            return dictionary.ContainsKey(key) ? dictionary[key] : default(Value);
        }
    }
}