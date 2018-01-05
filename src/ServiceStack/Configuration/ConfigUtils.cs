using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace ServiceStack.Configuration
{
    public class ConfigUtils
    {
        const int KeyIndex = 0;
        const int ValueIndex = 1;
        public const char ItemSeperator = ',';
        public const char KeyValueSeperator = ':';

        /// <summary>
        /// Gets the nullable app setting.
        /// </summary>
        public static string GetNullableAppSetting(string key)
        {
            return Platform.Instance.GetNullableAppSetting(key);
        }

        /// <summary>
        /// Gets the app setting.
        /// </summary>
        public static string GetAppSetting(string key)
        {
            return Platform.Instance.GetAppSetting(key);
        }

        /// <summary>
        /// Returns AppSetting[key] if exists otherwise defaultValue
        /// </summary>
        public static string GetAppSetting(string key, string defaultValue)
        {
            return Platform.Instance.GetAppSetting(key, defaultValue);
        }

        /// <summary>
        /// Returns AppSetting[key] if exists otherwise defaultValue, for non-string values
        /// </summary>
        public static T GetAppSetting<T>(string key, T defaultValue)
        {
            return Platform.Instance.GetAppSetting(key, defaultValue);
        }

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        public static string GetConnectionString(string key)
        {
            return Platform.Instance.GetConnectionString(key);
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

        private static Dictionary<string, string> appSettings;

        public static Dictionary<string, string> GetAppSettingsMap()
        {
            if (appSettings == null)
            {
                var map = new Dictionary<string, string>();
                var appConfigPath = Platform.Instance.GetAppConfigPath();
                if (appConfigPath != null)
                {
                    var xml = File.ReadAllText(appConfigPath);
                    using (var reader = XmlReader.Create(new StringReader(xml)))
                    {
                        var inAppSettings = false;
                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "appSettings")
                                break;

                            if (reader.NodeType != XmlNodeType.Element)
                                continue;

                            var elName = reader.Name;
                            if (elName == "appSettings")
                            {
                                inAppSettings = true;
                                continue;
                            }

                            if (!inAppSettings)
                                continue;

                            if (elName == "add")
                            {
                                var key = reader.GetAttribute("key");
                                if (key != null)
                                {
                                    var value = reader.GetAttribute("value");
                                    map[key] = value;
                                }
                            }
                        }
                    }
                }
                appSettings = map;
            }
            return appSettings;
        }

    }
}