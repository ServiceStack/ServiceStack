using System.Collections.Generic;

namespace ServiceStack.Configuration
{
    public class DictionarySettings : AppSettingsBase, ISettings
    {
        private readonly Dictionary<string, string> map;

        public DictionarySettings(Dictionary<string, string> map=null)
        {
            this.map = map ?? new Dictionary<string, string>();
            settings = this;
        }

        public string Get(string key)
        {
            string value;
            return map.TryGetValue(key, out value) ? value : null;
        }
    }
}