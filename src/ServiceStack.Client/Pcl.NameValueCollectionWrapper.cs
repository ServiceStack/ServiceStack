// 
// System.Web.HttpUtility
//
// Authors:
//   Patrik Torstensson (Patrik.Torstensson@labs2.com)
//   Wictor Wilén (decode/encode functions) (wictor@ibizkit.se)
//   Tim Coleman (tim@timcoleman.com)
//   Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// Copyright (C) 2005-2010 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.Collections.Generic;
using ServiceStack.Text;
using ServiceStack.Web;
using System.Text;

using System.Collections.Specialized;

#if NETSTANDARD2_0
//namespace System.Collections.Specialized
namespace ServiceStack.Pcl
{
    using System;
    using System.Net;
    using System.Text;
    using ServiceStack.Text;

    public class HttpUtility
    {
        private sealed class HttpQSCollection : NameValueCollection
        {
            public override string ToString()
            {
                int count = Count;
                if (count == 0)
                    return "";
                var sb = StringBuilderCache.Allocate();
                string[] keys = AllKeys;
                for (int i = 0; i < count; i++)
                {
                    sb.AppendFormat("{0}={1}&", keys[i], this[keys[i]]);
                }
                if (sb.Length > 0)
                    sb.Length--;
                return StringBuilderCache.ReturnAndFree(sb);
            }
        }

        public static NameValueCollection ParseQueryString(string query) => ParseQueryString(query, Encoding.UTF8);

        public static NameValueCollection ParseQueryString(string query, Encoding encoding)
        {
            if (query == null)
                throw new ArgumentNullException("query");
            if (encoding == null)
                throw new ArgumentNullException("encoding");
            if (query.Length == 0 || (query.Length == 1 && query[0] == '?'))
                return new HttpQSCollection();
            if (query[0] == '?')
                query = query.Substring(1);

            NameValueCollection result = new HttpQSCollection();
            ParseQueryString(query, encoding, result);
            return result;
        }

        internal static void ParseQueryString(string query, Encoding encoding, NameValueCollection result)
        {
            if (query.Length == 0)
                return;

            string decoded = PclExportClient.Instance.HtmlDecode(query);
            int decodedLength = decoded.Length;
            int namePos = 0;
            bool first = true;
            while (namePos <= decodedLength)
            {
                int valuePos = -1, valueEnd = -1;
                for (int q = namePos; q < decodedLength; q++)
                {
                    if (valuePos == -1 && decoded[q] == '=')
                    {
                        valuePos = q + 1;
                    }
                    else if (decoded[q] == '&')
                    {
                        valueEnd = q;
                        break;
                    }
                }

                if (first)
                {
                    first = false;
                    if (decoded[namePos] == '?')
                        namePos++;
                }

                string name, value;
                if (valuePos == -1)
                {
                    name = null;
                    valuePos = namePos;
                }
                else
                {
                    name = PclExportClient.Instance.UrlDecode(decoded.Substring(namePos, valuePos - namePos - 1));
                }
                if (valueEnd < 0)
                {
                    namePos = -1;
                    valueEnd = decoded.Length;
                }
                else
                {
                    namePos = valueEnd + 1;
                }
                value = PclExportClient.Instance.UrlDecode(decoded.Substring(valuePos, valueEnd - valuePos));

                result.Add(name, value);
                if (namePos == -1)
                    break;
            }
        }
    }
}
#endif

namespace ServiceStack
{
    public static class NameValueCollectionWrapperExtensions
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
                    map[key] = string.Join(",", values);
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

        public static string ToFormUrlEncoded(this NameValueCollection queryParams)
        {
            var sb = StringBuilderCache.Allocate();
            foreach (string key in queryParams.AllKeys)
            {
                var values = queryParams.GetValues(key);
                AppendKeyValue(sb, key, values);
            }

            return StringBuilderCache.ReturnAndFree(sb);
        }

        private static void AppendKeyValue(StringBuilder sb, string key, string[] values)
        {
            if (values == null) return;

            foreach (var value in values)
            {
                if (sb.Length > 0)
                    sb.Append('&');

                sb.Append($"{key.UrlEncode()}={value.UrlEncode()}");
            }
        }

    }
}
