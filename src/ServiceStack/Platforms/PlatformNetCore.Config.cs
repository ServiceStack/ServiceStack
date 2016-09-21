#if NETSTANDARD1_6

using System.IO;
using ServiceStack.Configuration;

namespace ServiceStack.Platforms
{
    public partial class PlatformNetCore : Platform
    {
        const string ErrorAppsettingNotFound = "Unable to find App Setting: {0}";
        const string ErrorConnectionStringNotFound = "Unable to find Connection String: {0}";
        const string ErrorCreatingType = "Error creating type {0} from text '{1}";
        public const string ConfigNullValue = "{null}";

        public override string GetAppConfigPath()
        {
            var hostInstance = ServiceStackHost.Instance
                ?? (ServiceStackHost) AppHostBase.NetCoreInstance
                ?? AppSelfHostBase.NetCoreInstance;

            if (hostInstance == null) return null;

            var configPath = "~/../web.config".MapProjectPath();
            if (File.Exists(configPath))
                return configPath;

            configPath = "~/../app.config".MapProjectPath();
            if (File.Exists(configPath))
                return configPath;

            //*nix FS FTW!
            configPath = "~/../Web.config".MapProjectPath(); 
            if (File.Exists(configPath))
                return configPath;

            configPath = "~/../App.config".MapProjectPath(); 
            if (File.Exists(configPath))
                return configPath;

            //dll App.config
            var appHostDll = new FileInfo(hostInstance.GetType().GetAssembly().Location).Name;
            configPath = $"~/{appHostDll}.config".MapAbsolutePath();
            return File.Exists(configPath) 
                ? configPath 
                : null;
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
