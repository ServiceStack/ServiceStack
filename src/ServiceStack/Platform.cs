using System;
using System.Collections.Generic;
using System.Reflection;
using ServiceStack.Configuration;
using ServiceStack.Platforms;
using ServiceStack.Web;

namespace ServiceStack
{
    public class Platform
    {
        public static Platform Instance =
#if NETSTANDARD1_6
            new PlatformNetCore();
#else
            new PlatformNet();
#endif

        public virtual HashSet<string> GetRazorNamespaces()
        {
            return new HashSet<string>();
        }

        public virtual void InitHostConifg(HostConfig config) {}

        public virtual string GetNullableAppSetting(string key)
        {
            return null;
        }

        public virtual string GetAppSetting(string key)
        {
            return null;
        }

        public virtual string GetAppSetting(string key, string defaultValue)
        {
            return defaultValue;
        }

        public virtual T GetAppSetting<T>(string key, T defaultValue)
        {
            return defaultValue;
        }

        public virtual string GetConnectionString(string key)
        {
            return null;
        }

        public virtual string GetAppConfigPath()
        {
            return null;
        }

        public virtual Dictionary<string, string> GetCookiesAsDictionary(IRequest httpReq)
        {
            return new Dictionary<string, string>();
        }

        public virtual Dictionary<string, string> GetCookiesAsDictionary(IResponse httpRes)
        {
            return new Dictionary<string, string>();
        }

        /// <summary>
        /// Get the static Parse(string) method on the type supplied
        /// </summary>
        private static MethodInfo GetParseMethod(Type type)
        {
            const string parseMethod = "Parse";
            if (type == typeof(string))
                return typeof(ConfigUtils).GetMethod(parseMethod, BindingFlags.Public | BindingFlags.Static);

            var parseMethodInfo = type.GetStaticMethod(parseMethod, new[] { typeof(string) });
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
        public static T ParseTextValue<T>(string textValue)
        {
            var parseMethod = GetParseMethod(typeof(T));
            if (parseMethod == null)
            {
                var ci = GetConstructorInfo(typeof(T));
                if (ci == null)
                    throw new TypeLoadException(
                        $"Error creating type {typeof(T).GetOperationName()} from text '{textValue}");

                var newT = ci.Invoke(null, new object[] { textValue });
                return (T)newT;
            }
            var value = parseMethod.Invoke(null, new object[] { textValue });
            return (T)value;
        }
    }
}