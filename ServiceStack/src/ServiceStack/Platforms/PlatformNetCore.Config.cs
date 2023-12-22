#if NETCORE

using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack.Configuration;
using ServiceStack.Logging;

namespace ServiceStack.Platforms;

public partial class PlatformNetCore : Platform
{
    public static ServiceStackHost HostInstance { get; set; }

    public const string ConfigNullValue = "{null}";
    
    public static readonly List<string> AppConfigPaths = new()
    {
        "~/web.config",
        "~/app.config",
        "~/Web.config",
        "~/App.config",
    };

    public override string GetAppConfigPath()
    {
        var host = ServiceStackHost.Instance
            ?? HostInstance;

        var appConfigPaths = new List<string>(AppConfigPaths);

        try
        {
            //dll App.config
            var location = host?.GetType().Assembly.Location;
            if (!string.IsNullOrEmpty(location))
            {
                var appHostDll = new FileInfo(location).Name;
                appConfigPaths.Add($"~/{appHostDll}.config");
            }
        }
        catch (Exception ex)
        {
            LogManager.GetLogger(typeof(PlatformNetCore)).Warn("GetAppConfigPath() GetAssembly().Location: ", ex);
        }

        var log = LogManager.GetLogger(typeof(PlatformNetCore));
        foreach (var configPath in appConfigPaths)
        {
            try
            {
                string resolvedPath;

                if (host != null)
                {
                    resolvedPath = host.MapProjectPath(configPath);
                    if (File.Exists(resolvedPath))
                        return resolvedPath;
                }

                resolvedPath = configPath.MapAbsolutePath();
                if (File.Exists(resolvedPath))
                    return resolvedPath;
            }
            catch (Exception ex)
            {
                log.Error("GetAppConfigPath(): ", ex);
            }
        }

        return null;
    }

    public override string GetNullableAppSetting(string key)
    {
        return ConfigUtils.GetAppSettingsMap().TryGetValue(key, out var value)
            ? value
            : null;
    }

    public override string GetAppSetting(string key)
    {
        string value = GetNullableAppSetting(key);

        if (value == null)
            throw new ConfigurationErrorsException(ErrorMessages.AppSettingNotFoundFmt.LocalizeFmt(key));

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

#endif
