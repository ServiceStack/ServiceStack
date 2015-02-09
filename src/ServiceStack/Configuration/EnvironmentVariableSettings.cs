using System;
using System.Collections.Generic;

namespace ServiceStack.Configuration
{
    public class EnvironmentVariableSettings : AppSettingsBase
    {
        class EnvironmentSettingsWrapper : ISettings
        {
            public string Get(string key)
            {
                return Environment.GetEnvironmentVariable(key);
            }

            public List<string> GetAllKeys()
            {
                return Environment.GetEnvironmentVariables().Keys.Map(x => x.ToString());
            }
        }

        public EnvironmentVariableSettings() : base(new EnvironmentSettingsWrapper()) {}

        public override string GetString(string name)
        {
            return base.GetNullableString(name);
        }
    }
}