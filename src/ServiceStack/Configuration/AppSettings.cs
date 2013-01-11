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

    public class ConfigurationResourceManager : AppSettings {}
}