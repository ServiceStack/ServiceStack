using System.Collections.Generic;
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
            public string Get(string key)
            {
#if !NETSTANDARD1_3
                return ConfigurationManager.AppSettings[key];
#else
                return null;
#endif
            }

            public List<string> GetAllKeys()
            {
#if !NETSTANDARD1_3
                return new List<string>(ConfigurationManager.AppSettings.AllKeys);
#else
                return TypeConstants.EmptyStringList;
#endif
            }
        }

        /// <summary>
        /// The tier lets you specify a retrieving a setting with the tier prefix first before falling back to the original key. 
        /// E.g a tier of 'Live' looks for 'Live.{Key}' or if not found falls back to '{Key}'.
        /// </summary>
        public AppSettings(string tier = null) : base(new ConfigurationManagerWrapper())
        {
            Tier = tier;
        }

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
}