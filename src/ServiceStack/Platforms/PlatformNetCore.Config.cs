#if NETSTANDARD1_6

using System;
using System.IO;
using ServiceStack.Configuration;
using ServiceStack.Logging;

namespace ServiceStack.Platforms
{
    public partial class PlatformNetCore : Platform
    {
        private static ILog log = LogManager.GetLogger(typeof(PlatformNetCore));
        
        public static ServiceStackHost HostInstance { get; set; }

        const string ErrorAppsettingNotFound = "Unable to find App Setting: {0}";
        public const string ConfigNullValue = "{null}";

        public override string GetAppConfigPath()
        {
            var host = ServiceStackHost.Instance
                ?? (ServiceStackHost) HostInstance;

            if (host == null) return null;

            var configPath = host.MapProjectPath("~/web.config");
            if (File.Exists(configPath))
                return configPath;

            configPath = host.MapProjectPath("~/app.config");
            if (File.Exists(configPath))
                return configPath;

            //*nix FS FTW!
            configPath = host.MapProjectPath("~/Web.config"); 
            if (File.Exists(configPath))
                return configPath;

            configPath = host.MapProjectPath("~/App.config"); 
            if (File.Exists(configPath))
                return configPath;

            try
            {
                //dll App.config
                var location = host.GetType().GetAssembly().Location;
                if (string.IsNullOrEmpty(location))
                    return null;

                var appHostDll = new FileInfo(location).Name;
                configPath = $"~/{appHostDll}.config".MapAbsolutePath();
                return File.Exists(configPath) 
                    ? configPath 
                    : null;
            }
            catch (Exception ex)
            {
                log.Error("GetAppConfigPath(): ", ex);
                return null;
            }
        }

        public override string GetNullableAppSetting(string key)
        {
            string value;
            return ConfigUtils.GetAppSettingsMap().TryGetValue(key, out value)
                ? value
                : null;
        }

        public override string GetAppSetting(string key)
        {
            string value = GetNullableAppSetting(key);

            if (value == null)
                throw new System.Configuration.ConfigurationErrorsException(string.Format(ErrorAppsettingNotFound, key));

            return value;
        }

        public override string GetAppSetting(string key, string defaultValue)
        {
            return GetNullableAppSetting(key) ?? defaultValue;
        }

        public override string GetConnectionString(string key)
        {
            return null;
        }

        public override T GetAppSetting<T>(string key, T defaultValue)
        {
            string val = GetNullableAppSetting(key);
            if (val != null)
            {
                if (ConfigNullValue.EndsWith(val))
                    return default(T);

                return ParseTextValue<T>(val);
            }
            return defaultValue;
        }

    }
}

#endif
