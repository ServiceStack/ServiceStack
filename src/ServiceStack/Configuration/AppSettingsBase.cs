using System;
using System.Collections.Generic;
using System.Configuration;
using ServiceStack.Text;

namespace ServiceStack.Configuration
{
    public class AppSettingsBase : IResourceManager
    {
        protected ISettings settings;
        const string ErrorAppsettingNotFound = "Unable to find App Setting: {0}";

        public AppSettingsBase(ISettings settings=null)
        {
            this.settings = settings;
        }

        public virtual string GetNullableString(string name)
        {
            return settings.Get(name);
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
            return ConfigUtils.GetListFromAppSettingValue(value);
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
}