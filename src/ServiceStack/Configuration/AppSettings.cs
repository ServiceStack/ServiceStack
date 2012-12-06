using System.Configuration;

namespace ServiceStack.Configuration
{
    /// <summary>
    /// More familiar name for the new crowd.
    /// </summary>
    public class AppSettings : AppSettingsBase
    {
        private class ConfigurationManagerWrapper : ISettings
        {
            public string Get(string key)
            {
                return ConfigurationManager.AppSettings[key];
            }
        }

        public AppSettings() : base(new ConfigurationManagerWrapper()) {}
    }

    public class ConfigurationResourceManager : AppSettings {}
}