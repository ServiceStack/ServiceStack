using System;
using System.Collections.Generic;
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

            foreach (var name in request.QueryString.AllKeys)
            {
                if (name == null) continue; //thank you ASP.NET

                var values = request.QueryString.GetValues(name);
                if (values.Length == 1)
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

            if ((request.Verb == HttpMethods.Post || request.Verb == HttpMethods.Put)
                && request.FormData != null)
            {
                foreach (var name in request.FormData.AllKeys)
                {
                    if (name == null) continue; //thank you ASP.NET

                    var values = request.FormData.GetValues(name);
                    if (values.Length == 1)
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

            return map;
        }

    }
}