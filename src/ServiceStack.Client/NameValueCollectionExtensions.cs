// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if !(SL5 || IOS || XBOX || PCL)

using System.Collections.Generic;
using System.Collections.Specialized;
using ServiceStack.Web;

namespace ServiceStack
{
    public static class NameValueCollectionExtensions
    {

        public static Dictionary<string, string> ToDictionary(this INameValueCollection nameValues)
        {
            return ToDictionary((NameValueCollection)nameValues.Original);
        }

        public static Dictionary<string, string> ToDictionary(this System.Collections.Specialized.NameValueCollection nameValues)
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

        public static System.Collections.Specialized.NameValueCollection ToNameValueCollection(this Dictionary<string, string> map)
        {
            if (map == null) return new System.Collections.Specialized.NameValueCollection();

            var nameValues = new System.Collections.Specialized.NameValueCollection();
            foreach (var item in map)
            {
                nameValues.Add(item.Key, item.Value);
            }
            return nameValues;
        }

    }
}

#endif
