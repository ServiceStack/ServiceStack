﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceStack.Configuration
{
    public class MultiAppSettings : AppSettingsBase, ISettings
    {
        public MultiAppSettings(params IAppSettings[] appSettings)
            : base(new MultiSettingsWrapper(appSettings))
        {
            this.instance = (MultiSettingsWrapper)settings;
        }

        private MultiSettingsWrapper instance;

        class MultiSettingsWrapper : ISettingsWriter
        {
            private readonly IAppSettings[] appSettings;

            public MultiSettingsWrapper(IAppSettings[] appSettings)
            {
                if (appSettings.Length == 0)
                    throw new ArgumentNullException("appSettings");

                this.appSettings = appSettings ?? new IAppSettings[0];
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
                var appSetting = appSettings.First();
                appSetting.Set(key, value);
            }
        }
    }
}