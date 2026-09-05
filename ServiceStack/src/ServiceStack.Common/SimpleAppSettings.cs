using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Configuration;

namespace ServiceStack;

public class SimpleAppSettings : IAppSettings
{
    private readonly Dictionary<string, string> settings;

    public SimpleAppSettings(Dictionary<string, string> settings = null) =>
        this.settings = settings ?? new Dictionary<string, string>();

    public Dictionary<string, string> GetAll()
    {
        lock (settings)
        {
            return new Dictionary<string, string>(settings);
        }
    }

    public List<string> GetAllKeys()
    {
        lock (settings)
        {
            return settings.Keys.ToList();
        }
    }

    public bool Exists(string key)
    {
        if (key == null) return false;
        lock (settings)
        {
            return settings.ContainsKey(key);
        }
    }

    public void Set<T>(string key, T value)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        var textValue = value is string s
            ? s
            : value.ToJsv();

        lock (settings)
        {
            settings[key] = textValue;
        }
    }

    public string GetString(string key)
    {
        if (key == null) return null;
        lock (settings)
        {
            return settings.TryGetValue(key, out string value) ? value : null;
        }
    }

    public IList<string> GetList(string key)
    {
        var val = GetString(key);
        return val != null ? val.FromJsv<List<string>>() ?? new List<string>() : new List<string>();
    }

    public IDictionary<string, string> GetDictionary(string key)
    {
        var val = GetString(key);
        return val != null ? val.FromJsv<Dictionary<string, string>>() ?? new Dictionary<string, string>() : new Dictionary<string, string>();
    }

    public List<KeyValuePair<string, string>> GetKeyValuePairs(string key)
    {
        var val = GetString(key);
        return val != null ? val.FromJsv<List<KeyValuePair<string, string>>>() ?? new List<KeyValuePair<string, string>>() : new List<KeyValuePair<string, string>>();
    }

    public T Get<T>(string key)
    {
        var val = GetString(key);
        return val != null ? val.FromJsv<T>() : default;
    }

    public T Get<T>(string key, T defaultValue)
    {
        var value = GetString(key);
        return value != null ? value.FromJsv<T>() : defaultValue;
    }
}