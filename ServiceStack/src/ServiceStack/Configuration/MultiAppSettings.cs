using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace ServiceStack.Configuration;

public class MultiAppSettings : AppSettingsBase, ISettings
{
    public MultiAppSettings(params IAppSettings[] appSettings)
        : base(new MultiSettingsWrapper(appSettings))
    {
        this.instance = (MultiSettingsWrapper)settings;
        AppSettings = appSettings;
    }

    private MultiSettingsWrapper instance;
    public IAppSettings[] AppSettings { get; }

    class MultiSettingsWrapper : ISettingsWriter
    {
        private readonly IAppSettings[] appSettings;

        public MultiSettingsWrapper(IAppSettings[] appSettings)
        {
            if (appSettings.Length == 0)
                throw new ArgumentNullException(nameof(appSettings));

            this.appSettings = appSettings;
        }

        public string Get(string key)
        {
            return appSettings
                .Select(appSetting => appSetting.GetString(key))
                .FirstOrDefault(value => value != null);
        }

        public List<string> GetAllKeys()
        {
            var allKeys = new HashSet<string>();
            appSettings.Each(s => s.GetAllKeys().Each(x => allKeys.Add(x)));
            return allKeys.ToList();
        }

        public void Set<T>(string key, T value)
        {
            appSettings.Each(x => x.Set(key, value));
        }
    }

    public override T Get<T>(string name) => Get<T>(name, default);

    public override T Get<T>(string name, T defaultValue)
    {
        try
        {
            foreach (var appSettings in AppSettings)
            {
                if (appSettings.Exists(name))
                    return appSettings.Get<T>(name);
            }
            return defaultValue;
        }
        catch (Exception ex)
        {
            if (ex is ConfigurationErrorsException)
                throw;
            var message = $"The {name} setting had an invalid format and could not be cast to type {typeof(T).FullName}";
            throw new ConfigurationErrorsException(message, ex);
        }
    }
}