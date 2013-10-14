using System;
using System.Configuration;
using System.Linq;

namespace ServiceStack.Configuration
{
    /// <summary>
    /// More familiar name for the new crowd.
    /// </summary>
    public class AppSettings : AppSettingsBase
    {
        private class ConfigurationManagerWrapper : ISettings
        {
            private bool allowLineFeedsInConfigFile;
            public ConfigurationManagerWrapper(bool allowLineFeedsInConfigFile)
            {
                this.allowLineFeedsInConfigFile = allowLineFeedsInConfigFile;
            }

            public string Get(string key)
            {
                string config = ConfigurationManager.AppSettings[key];
                if (allowLineFeedsInConfigFile && !string.IsNullOrEmpty(config))
                {
                    var lines = config.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length > 1)
                        config = string.Join("", lines.Select(x => x.Trim()));
                }
                return config;
            }
        }

        public AppSettings(bool allowLineFeedsInConfigFile = false) : base(new ConfigurationManagerWrapper(allowLineFeedsInConfigFile)) { }

        /// <summary>
        /// Returns string if exists, otherwise null
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override string GetString(string name) //Keeping backwards compatible
        {
            return base.GetNullableString(name);
        }
    }

    public class ConfigurationResourceManager : AppSettings { }
}