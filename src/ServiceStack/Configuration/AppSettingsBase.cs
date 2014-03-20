using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using ServiceStack.Text;

namespace ServiceStack.Configuration
{
    public delegate string ParsingStrategyDelegate(string originalSetting);

    public class AppSettingsBase : IAppSettings
    {
        protected ISettings settings;
        protected const string ErrorAppsettingNotFound = "Unable to find App Setting: {0}";

        public string Tier { get; set; }

        public ParsingStrategyDelegate ParsingStrategy { get; set; }

        public AppSettingsBase(ISettings settings=null)
        {
            this.settings = settings;
        }

        public virtual string GetNullableString(string name)
        {
            var value = Tier != null
                ? settings.Get("{0}.{1}".Fmt(Tier, name)) ?? settings.Get(name)
                : settings.Get(name);

            return ParsingStrategy != null
                ? ParsingStrategy(value)
                : value;
        }

        public virtual string GetString(string name)
        {
            var value = GetNullableString(name);
            if (value == null)
            {
                throw new ConfigurationErrorsException(String.Format(ErrorAppsettingNotFound, name));
            }

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
                var message =
                    string.Format(
                        "The {0} setting had an invalid dictionary format. The correct format is of type \"Key1:Value1,Key2:Value2\"",
                        key);
                throw new ConfigurationErrorsException(message, ex);
            }
        }

        public virtual T Get<T>(string name, T defaultValue)
        {
            var stringValue = GetNullableString(name);

            T deserializedValue;
            try
            {
                deserializedValue = TypeSerializer.DeserializeFromString<T>(stringValue);
            }
            catch (Exception ex)
            {
                var message =
                   string.Format(
                       "The {0} setting had an invalid format. The value \"{1}\" could not be cast to type {2}",
                       name, stringValue, typeof(T).FullName);
                throw new ConfigurationErrorsException(message, ex);
            }

            return stringValue != null
                       ? deserializedValue
                       : defaultValue;
        }
    }

    public static class AppSettingsStrategy
    {
        public static string CollapseNewLines(string originalSetting)
        {
            if (originalSetting == null) return null;

            var lines = originalSetting.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            return lines.Length > 1 
                ? string.Join("", lines.Select(x => x.Trim())) 
                : originalSetting;
        }
    }
}