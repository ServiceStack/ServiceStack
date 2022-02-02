using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Configuration;

namespace ServiceStack
{
    public class SimpleAppSettings : IAppSettings
    {
        private readonly Dictionary<string, string> settings;

        public SimpleAppSettings(Dictionary<string, string> settings = null) =>
            this.settings = settings ?? new Dictionary<string, string>();

        public Dictionary<string, string> GetAll() => settings;

        public List<string> GetAllKeys() => settings.Keys.ToList();

        public bool Exists(string key) => settings.ContainsKey(key);

        public void Set<T>(string key, T value)
        {
            var textValue = value is string s
                ? s
                : value.ToJsv();

            settings[key] = textValue;
        }

        public string GetString(string key) => settings.TryGetValue(key, out string value)
            ? value
            : null;

        public IList<string> GetList(string key) => GetString(key).FromJsv<List<string>>();

        public IDictionary<string, string> GetDictionary(string key) => GetString(key).FromJsv<Dictionary<string, string>>();
        public List<KeyValuePair<string, string>> GetKeyValuePairs(string key) => GetString(key).FromJsv<List<KeyValuePair<string, string>>>();

        public T Get<T>(string key) => GetString(key).FromJsv<T>();

        public T Get<T>(string key, T defaultValue)
        {
            var value = GetString(key);
            return value != null ? value.FromJsv<T>() : defaultValue;
        }
    }
}