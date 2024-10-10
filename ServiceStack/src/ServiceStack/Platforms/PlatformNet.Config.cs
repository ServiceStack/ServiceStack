#if NETFRAMEWORK
using System.Configuration;

namespace ServiceStack.Platforms;

public partial class PlatformNet : Platform
{
    public const string ConfigNullValue = "{null}";

    /// <summary>
    /// Determines whether the Config section identified by the sectionName exists.
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
            throw new ConfigurationErrorsException(ErrorMessages.AppSettingNotFoundFmt.LocalizeFmt(key));

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

            return ParseTextValue<T>(val);
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
            throw new ConfigurationErrorsException(ErrorMessages.ConnectionStringNotFoundFmt.LocalizeFmt(key));

        return value;
    }
}

#endif
