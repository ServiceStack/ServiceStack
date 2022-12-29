using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using ServiceStack.IO;
using ServiceStack.Text;

namespace ServiceStack.Configuration
{
    public delegate string ParsingStrategyDelegate(string originalSetting);

    public class AppSettingsBase : IAppSettings, ISettingsWriter
    {
        protected ISettings settings;
        protected ISettingsWriter settingsWriter;

        public string Tier { get; set; }

        public ParsingStrategyDelegate ParsingStrategy { get; set; }

        public AppSettingsBase(ISettings settings=null)
        {
            Init(settings);
        }

        protected void Init(ISettings settings)
        {
            this.settings = settings;
            this.settingsWriter = settings as ISettingsWriter;
        }

        public virtual string GetNullableString(string name)
        {
            var value = Tier != null
                ? Get($"{Tier}.{name}") ?? Get(name)
                : Get(name);

            return ParsingStrategy != null
                ? ParsingStrategy(value)
                : value;
        }

        public string Get(string name)
        {
            var value = settingsWriter?.Get(name);
            return value ?? settings.Get(name);
        }

        public virtual Dictionary<string, string> GetAll()
        {
            var keys = GetAllKeys();
            var to = new Dictionary<string,string>();
            foreach (var key in keys)
            {
                to[key] = GetNullableString(key);
            }
            return to;
        }

        public virtual List<string> GetAllKeys()
        {
            var keys = settings.GetAllKeys().ToSet();
            settingsWriter?.GetAllKeys().Each(x => keys.Add(x));

            return keys.ToList();
        }

        public virtual bool Exists(string key)
        {
            return GetNullableString(key) != null;
        }

        public virtual string GetString(string name)
        {
            return GetNullableString(name);
        }

        public virtual string GetRequiredString(string name)
        {
            var value = GetNullableString(name);
            if (value == null)
                throw new ConfigurationErrorsException(ErrorMessages.AppSettingNotFoundFmt.LocalizeFmt(name));

            return value;
        }

        public virtual IList<string> GetList(string key)
        {
            var value = GetString(key);
            return value == null 
                ? new List<string>() 
                : ConfigUtils.GetListFromAppSettingValue(value);
        }
        
        public virtual IDictionary<string, string> GetDictionary(string key)
        {
            var value = GetString(key);
            try
            {
                return ConfigUtils.GetDictionaryFromAppSettingValue(value);
            }
            catch (Exception ex)
            {
                var message = $"The {key} setting had an invalid Key/Value format: \"Key1:Value1,Key2:Value2\"";
                throw new ConfigurationErrorsException(message, ex);
            }
        }

        public virtual List<KeyValuePair<string, string>> GetKeyValuePairs(string key)
        {
            var value = GetString(key);
            try
            {
                return ConfigUtils.GetKeyValuePairsFromAppSettingValue(value);
            }
            catch (Exception ex)
            {
                var message = $"The {key} setting had an invalid Key/Value format: \"Key1:Value1,Key2:Value2\"";
                throw new ConfigurationErrorsException(message, ex);
            }
        }

        public virtual T Get<T>(string name)
        {
            var stringValue = GetNullableString(name);
            return stringValue != null 
                ? TypeSerializer.DeserializeFromString<T>(stringValue) 
                : default(T);
        }

        public virtual T Get<T>(string name, T defaultValue)
        {
            var stringValue = GetNullableString(name);

            T ret = defaultValue;
            try
            {
                if (stringValue != null)
                {
                    ret = TypeSerializer.DeserializeFromString<T>(stringValue);
                }
            }
            catch (Exception ex)
            {
                var message = $"The {name} setting had an invalid format. " +
                              $"The value \"{stringValue}\" could not be cast to type {typeof(T).FullName}";
                throw new ConfigurationErrorsException(message, ex);
            }

            return ret;
        }

        public virtual void Set<T>(string key, T value)
        {
            settingsWriter ??= new DictionarySettings();
            settingsWriter.Set(key, value);
        }
    }

    public static class AppSettingsStrategy
    {
        public static string CollapseNewLines(string originalSetting)
        {
            if (originalSetting == null) return null;

            var lines = originalSetting.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return lines.Length > 1 
                ? string.Join("", lines.Select(x => x.Trim())) 
                : originalSetting;
        }
    }

    public static class AppSettingsUtils
    {
        public static string GetRequiredString(this IAppSettings settings, string name)
        {
            if (settings is AppSettingsBase appSettings)
                return appSettings.GetRequiredString(name);

            var value = settings.GetString(name);
            if (value == null)
                throw new ConfigurationErrorsException(ErrorMessages.AppSettingNotFoundFmt.LocalizeFmt(name));

            return value;
        }

        public static string GetNullableString(this IAppSettings settings, string name)
        {
            if (settings is AppSettingsBase appSettings)
                return appSettings.GetNullableString(name);

            var value = settings.GetString(name);
            return value;
        }

        public static string GetConnectionString(this IAppSettings appSettings, string name)
        {
#if NETCORE
            return appSettings is NetCoreAppSettings config
                ? config.Configuration?.GetSection("ConnectionStrings")?[name]
                : appSettings.GetString("ConnectionStrings:" + name);
#else
            return System.Configuration.ConfigurationManager.ConnectionStrings[name]?.ConnectionString;
#endif
        }

        
        /// <summary>
        /// User app.settings for Sharp App
        /// </summary>
        public static string GetUserAppSettingsPath(string appName)
        {
            if (appName == null)
                return null;
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var appSettingsPath = Path.Combine(homeDir, ".servicestack", "desktop", appName, "app.settings");
            return appSettingsPath;
        }

        public static void SaveAppSetting(string appSettingsPath, string name, string value)
        {
            if (appSettingsPath == null)
                throw new ArgumentNullException(nameof(appSettingsPath));

            if (!File.Exists(appSettingsPath))
            {
                var dirPath = Path.GetDirectoryName(appSettingsPath);
                FileSystemVirtualFiles.AssertDirectory(dirPath);
                File.WriteAllText(appSettingsPath, $"{name} {value}{Environment.NewLine}");
                return;
            }

            var sb = StringBuilderCache.Allocate();
            var lines = File.ReadAllLines(appSettingsPath);
            var found = false;
            foreach (var line in lines)
            {
                var match = line.StartsWith(name);
                if (match)
                    found = true;
                var useLine = match
                    ? $"{name} {value}"
                    : line;
                sb.AppendLine(useLine);
            }

            if (!found)
            {
                sb.AppendLine($"{name} {value}");
            }

            var contents = StringBuilderCache.ReturnAndFree(sb);
            File.WriteAllText(appSettingsPath, contents);
        }
    }
}