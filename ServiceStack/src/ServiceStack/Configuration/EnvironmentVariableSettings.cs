using System;
using System.Collections.Generic;

namespace ServiceStack.Configuration;

public class EnvironmentVariableSettings : AppSettingsBase
{
    class EnvironmentSettingsWrapper : ISettings
    {
        public string Get(string key)
        {
            return key != null ? Environment.GetEnvironmentVariable(key) : null;
        }

        public List<string> GetAllKeys()
        {
            var vars = Environment.GetEnvironmentVariables();
            var list = new List<string>();
            foreach (var key in vars.Keys)
            {
                if (key != null)
                    list.Add(key.ToString());
            }
            return list;
        }
    }

    public EnvironmentVariableSettings() : base(new EnvironmentSettingsWrapper()) {}

    public override string GetString(string name)
    {
        return base.GetNullableString(name);
    }
}