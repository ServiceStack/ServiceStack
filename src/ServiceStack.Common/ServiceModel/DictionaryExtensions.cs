#if !SILVERLIGHT && !MONOTOUCH && !XBOX

using System.Collections.Generic;
using System.Collections.Specialized;

namespace ServiceStack.ServiceModel
{
    public static class DictionaryExtensions
    {
        public static Dictionary<string, string> ToDictionary(this NameValueCollection nameValues)
        {
            if (nameValues == null) return new Dictionary<string, string>();

            var map = new Dictionary<string, string>();
            foreach (var key in nameValues.AllKeys)
            {
                if (key == null)
                {
                    //occurs when no value is specified, e.g. 'path/to/page?debug'
                    //throw new ArgumentNullException("key", "nameValues: " + nameValues);
                    continue;
                }

                var values = nameValues.GetValues(key);
                if (values != null && values.Length > 0)
                {
                    map[key] = values[0];
                }
            }
            return map;
        }

        public static NameValueCollection ToNameValueCollection(this Dictionary<string, string> map)
        {
            if (map == null) return new NameValueCollection();

            var nameValues = new NameValueCollection();
            foreach (var item in map)
            {
                nameValues.Add(item.Key, item.Value);
            }
            return nameValues;
        }
    }

}
#endif