using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;

namespace ServiceStack.Configuration
{
    public class ConfigUtils
    {
        const int KeyIndex = 0;
        const int ValueIndex = 1;
        const string ErrorAppsettingNotFound = "Unable to find App Setting: {0}";
        const string ErrorConnectionStringNotFound = "Unable to find Connection String: {0}";
        const string ErrorCreatingType = "Error creating type {0} from text '{1}";
        public const char ItemSeperator = ',';
        public const char KeyValueSeperator = ':';
        public const string ConfigNullValue = "{null}";

        /// <summary>
        /// Gets the nullable app setting.
        /// </summary>
        public static string GetNullableAppSetting(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }

        /// <summary>
        /// Gets the app setting.
        /// </summary>
        public static string GetAppSetting(string key)
        {
            string value = ConfigurationManager.AppSettings[key];

            if (value == null)
                throw new ConfigurationErrorsException(string.Format(ErrorAppsettingNotFound, key));

            return value;
        }

        /// <summary>
        /// Determines wheter the Config section identified by the sectionName exists.
        /// </summary>
        public static bool ConfigSectionExists(string sectionName)
        {
            return (ConfigurationManager.GetSection(sectionName) != null);
        }

        /// <summary>
        /// Returns AppSetting[key] if exists otherwise defaultValue
        /// </summary>
        public static string GetAppSetting(string key, string defaultValue)
        {
            return ConfigurationManager.AppSettings[key] ?? defaultValue;
        }

        /// <summary>
        /// Returns AppSetting[key] if exists otherwise defaultValue, for non-string values
        /// </summary>
        public static T GetAppSetting<T>(string key, T defaultValue)
        {
            string val = ConfigurationManager.AppSettings[key];
            if (val != null)
            {
                if (ConfigNullValue.EndsWith(val))
                    return default(T);

                return ParseTextValue<T>(ConfigurationManager.AppSettings[key]);
            }
            return defaultValue;
        }

        /// <summary>
        /// Gets the connection string setting.
        /// </summary>
        public static ConnectionStringSettings GetConnectionStringSetting(string key)
        {
            var value = ConfigurationManager.ConnectionStrings[key];
            if (value == null)
                throw new ConfigurationErrorsException(string.Format(ErrorConnectionStringNotFound, key));

            return value;
        }

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        public static string GetConnectionString(string key)
        {
            return GetConnectionStringSetting(key).ToString();
        }

        /// <summary>
        /// Gets the list from app setting.
        /// </summary>
        public static List<string> GetListFromAppSetting(string key)
        {
            var appSettingValue = GetAppSetting(key);
            return GetListFromAppSettingValue(appSettingValue);
        }

        public static List<string> GetListFromAppSettingValue(string appSettingValue)
        {
            return new List<string>(appSettingValue.Split(ItemSeperator));
        }

        /// <summary>
        /// Gets the dictionary from app setting.
        /// </summary>
        public static Dictionary<string, string> GetDictionaryFromAppSetting(string key)
        {
            var appSettingValue = GetAppSetting(key);
            return GetDictionaryFromAppSettingValue(appSettingValue);
        }

        public static Dictionary<string, string> GetDictionaryFromAppSettingValue(string appSettingValue)
        {
            var dictionary = new Dictionary<string, string>();

            foreach (var item in appSettingValue.Split(ItemSeperator))
            {
                var keyValuePair = item.Split(KeyValueSeperator);
                dictionary.Add(keyValuePair[KeyIndex], keyValuePair[ValueIndex]);
            }
            return dictionary;
        }

        /// <summary>
        /// Get the static Parse(string) method on the type supplied
        /// </summary>
        private static MethodInfo GetParseMethod(Type type)
        {
            const string parseMethod = "Parse";
            if (type == typeof(string))
                return typeof(ConfigUtils).GetMethod(parseMethod, BindingFlags.Public | BindingFlags.Static);

            var parseMethodInfo = type.GetMethod(parseMethod,
                BindingFlags.Public | BindingFlags.Static, null,
                new Type[] { typeof(string) }, null);

            return parseMethodInfo;
        }

        /// <summary>
        /// Gets the constructor info for T(string) if exists.
        /// </summary>
        private static ConstructorInfo GetConstructorInfo(Type type)
        {
            foreach (ConstructorInfo ci in type.GetConstructors())
            {
                var ciTypes = ci.GetGenericArguments();
                var matchFound = (ciTypes.Length == 1 && ciTypes[0] == typeof(string)); //e.g. T(string)
                if (matchFound)
                    return ci;
            }
            return null;
        }

        /// <summary>
        /// Returns the value returned by the 'T.Parse(string)' method if exists otherwise 'new T(string)'. 
        /// e.g. if T was a TimeSpan it will return TimeSpan.Parse(textValue).
        /// If there is no Parse Method it will attempt to create a new instance of the destined type
        /// </summary>
        private static T ParseTextValue<T>(string textValue)
        {
            var parseMethod = GetParseMethod(typeof(T));
            if (parseMethod == null)
            {
                var ci = GetConstructorInfo(typeof(T));
                if (ci == null)
                    throw new TypeLoadException(string.Format(ErrorCreatingType, typeof(T).GetOperationName(), textValue));

                var newT = ci.Invoke(null, new object[] { textValue });
                return (T)newT;
            }
            var value = parseMethod.Invoke(null, new object[] { textValue });
            return (T)value;
        }
    }
}