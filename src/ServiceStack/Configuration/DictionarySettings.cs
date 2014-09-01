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

        public virtual string Get(string key)
        {
            string value;
            return map.TryGetValue(key, out value) ? value : null;
        }

        public virtual Dictionary<string, string> GetAll()
        {
            return map;
        }

        public override void Set<T>(string key, T value)
        {
            var textValue = value is string
                ? (string)(object)value
                : value.ToJsv();

            map[key] = textValue;
        }
    }
}