#if !NETSTANDARD1_6
using System;
using System.Configuration;
using System.Reflection;
using ServiceStack.Configuration;

namespace ServiceStack.Platforms
{
    public partial class PlatformNet : Platform
    {
        const string ErrorAppsettingNotFound = "Unable to find App Setting: {0}";
        const string ErrorConnectionStringNotFound = "Unable to find Connection String: {0}";
        const string ErrorCreatingType = "Error creating type {0} from text '{1}";
        public const string ConfigNullValue = "{null}";

        /// <summary>
        /// Determines wheter the Config section identified by the sectionName exists.
        /// </summary>
        public static bool ConfigSectionExists(string sectionName)
        {
            return ConfigurationManager.GetSection(sectionName) != null;
        }

        public override string GetNullableAppSetting(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }

        public override string GetAppSetting(string key)
        {
            string value = ConfigurationManager.AppSettings[key];

            if (value == null)
                throw new ConfigurationErrorsException(string.Format(ErrorAppsettingNotFound, key));

            return value;
        }

        public override string GetAppSetting(string key, string defaultValue)
        {
            return ConfigurationManager.AppSettings[key] ?? defaultValue;
        }

        public override string GetConnectionString(string key)
        {
            return GetConnectionStringSetting(key).ToString();
        }

        public override T GetAppSetting<T>(string key, T defaultValue)
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
        /// Get the static Parse(string) method on the type supplied
        /// </summary>
        private static MethodInfo GetParseMethod(Type type)
        {
            const string parseMethod = "Parse";
            if (type == typeof(string))
                return typeof(ConfigUtils).GetMethod(parseMethod, BindingFlags.Public | BindingFlags.Static);

            var parseMethodInfo = type.GetMethod(parseMethod,
                BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(string) }, null);

            return parseMethodInfo;
        }

        /// <summary>
        /// Gets the constructor info for T(string) if exists.
        /// </summary>
        private static ConstructorInfo GetConstructorInfo(Type type)
        {
            foreach (var ci in type.GetConstructors())
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
#endif
