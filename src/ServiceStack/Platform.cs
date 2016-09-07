using System.Collections.Generic;
using ServiceStack.Platforms;

namespace ServiceStack
{
    public class Platform
    {
        public static Platform Instance =
#if NETSTANDARD1_3
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
    }
}