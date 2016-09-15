#if !NETSTANDARD1_6
using System;
using System.Collections.Generic;
using System.Net;
using System.Web;
using ServiceStack.Web;

namespace ServiceStack.Platforms
{
    public partial class PlatformNet : Platform
    {
        public override Dictionary<string, string> GetCookiesAsDictionary(IRequest httpReq)
        {
            var map = new Dictionary<string, string>();
            var aspNet = httpReq.OriginalRequest as HttpRequest;
            if (aspNet != null)
            {
                foreach (var name in aspNet.Cookies.AllKeys)
                {
                    var cookie = aspNet.Cookies[name];
                    if (cookie == null) continue;
                    map[name] = cookie.Value;
                }
            }
            else
            {
                var httpListener = httpReq.OriginalRequest as HttpListenerRequest;
                if (httpListener != null)
                {
                    for (var i = 0; i < httpListener.Cookies.Count; i++)
                    {
                        var cookie = httpListener.Cookies[i];
                        if (cookie?.Name == null) continue;
                        map[cookie.Name] = cookie.Value;
                    }
                }
            }
            return map;
        }

        public override Dictionary<string, string> GetCookiesAsDictionary(IResponse httpRes)
        {
            var map = new Dictionary<string, string>();
            var aspNet = httpRes.OriginalResponse as System.Web.HttpResponse;
            if (aspNet != null)
            {
                foreach (var name in aspNet.Cookies.AllKeys)
                {
                    var cookie = aspNet.Cookies[name];
                    if (cookie == null) continue;
                    map[name] = cookie.Value;
                }
            }
            else
            {
                var httpListener = httpRes.OriginalResponse as HttpListenerResponse;
                if (httpListener != null)
                {
                    for (var i = 0; i < httpListener.Cookies.Count; i++)
                    {
                        var cookie = httpListener.Cookies[i];
                        if (cookie?.Name == null) continue;
                        map[cookie.Name] = cookie.Value;
                    }
                }
            }
            return map;
        }
    }
}

#endif
