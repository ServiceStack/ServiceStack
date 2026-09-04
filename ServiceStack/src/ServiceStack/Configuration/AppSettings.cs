using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Configuration;

/// <summary>
/// More familiar name for the new crowd.
/// </summary>
public class AppSettings : AppSettingsBase
{
    private class ConfigurationManagerWrapper : ISettings
    {
        public string Get(string key)
        {
#if !NETCORE
            return ConfigurationManager.AppSettings[key];
#else
            var appSettings = ConfigUtils.GetAppSettingsMap();
            return appSettings.TryGetValue(key, out var value)
                ? value
                : null;
#endif
        }

        public List<string> GetAllKeys()
        {
#if !NETCORE
            var allKeys = ConfigurationManager.AppSettings.AllKeys;
            return allKeys != null ? new List<string>(allKeys) : new List<string>();
#else
            var appSettings = ConfigUtils.GetAppSettingsMap();
            return appSettings != null ? appSettings.Keys.ToList() : new List<string>();
#endif
        }
    }

    /// <summary>
    /// The tier lets you specify a retrieving a setting with the tier prefix first before falling back to the original key. 
    /// E.g a tier of 'Live' looks for 'Live.{Key}' or if not found falls back to '{Key}'.
    /// </summary>
    public AppSettings(string tier = null) : base(new ConfigurationManagerWrapper())
    {
        Tier = tier;
    }

    /// <summary>
    /// Returns string if exists, otherwise null
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public override string GetString(string name) //Keeping backwards compatible
    {
        return base.GetNullableString(name); 
    }
}

public class RuntimeAppSettings : IRuntimeAppSettings
{
    public Dictionary<string, Func<IRequest, object>> Settings { get; set; } = new();

    public T Get<T>(IRequest request, string name, T defaultValue)
    {
        if (name != null && Settings != null && Settings.TryGetValue(name, out var fn))
        {
            try
            {
                var val = fn(request);
                if (val == null)
                    return defaultValue;
                if (val is T t)
                    return t;
                return val.ConvertTo<T>();
            }
            catch
            {
                return defaultValue;
            }
        }

        return defaultValue;
    }
}