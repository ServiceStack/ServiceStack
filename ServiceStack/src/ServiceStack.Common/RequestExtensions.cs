using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public static class RequestExtensions
    {
        /// <summary>
        /// Duplicate Params are given a unique key by appending a #1 suffix
        /// </summary>
        public static Dictionary<string, string> GetRequestParams(this IRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            
            var map = new Dictionary<string, string>();

            AddToMap(request.QueryString, map);

            if ((request.Verb == HttpMethods.Post || request.Verb == HttpMethods.Put) && request.FormData != null)
            {
                AddToMap(request.FormData, map);
            }

            return map;
        }

        private static void AddToMap(NameValueCollection nvc, Dictionary<string, string> map)
        {
            for (int index = 0; index < nvc.Count; index++)
            {
                var name = nvc.GetKey(index);
                var values = nvc.GetValues(name); // Only use string name instead of index which returns multiple values 

                if (name == null) //thank you .NET Framework!
                {
                    if (values?.Length > 0)
                        map[values[0]] = null;
                    continue;
                }
                
                if (values == null || values.Length == 0)
                {
                    map[name] = null;
                }
                else if (values.Length == 1)
                {
                    map[name] = values[0];
                }
                else
                {
                    for (var i = 0; i < values.Length; i++)
                    {
                        map[name + (i == 0 ? "" : "#" + i)] = values[i];
                    }
                }
            }
        }
    }
}