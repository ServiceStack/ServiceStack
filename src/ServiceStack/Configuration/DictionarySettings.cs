using System.Collections.Generic;
using System.Linq;

namespace ServiceStack.Configuration
{
    public class DictionarySettings : AppSettingsBase, ISettings
    {
        private readonly DictionaryWrapper instance;

        class DictionaryWrapper : ISettingsWriter
        {
            internal readonly Dictionary<string, string> Map;

            public DictionaryWrapper(Dictionary<string, string> map = null)
            {
                Map = map ?? new Dictionary<string, string>();
            }

            public string Get(string key)
            {
                string value;
                return Map.TryGetValue(key, out value) ? value : null;
            }

            public List<string> GetAllKeys()
            {
                return Map.Keys.ToList();
            }

            public void Set<T>(string key, T value)
            {
                var textValue = value is string
                    ? (string)(object)value
                    : value.ToJsv();

                Map[key] = textValue;
            }
        }

        public DictionarySettings(Dictionary<string, string> map=null)
            : base(new DictionaryWrapper(map))
        {
            instance = (DictionaryWrapper)settings;
        }

        public override Dictionary<string, string> GetAll()
        {
            return instance.Map;
        }
    }
}