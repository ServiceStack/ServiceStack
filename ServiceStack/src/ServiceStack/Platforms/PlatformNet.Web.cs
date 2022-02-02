#if !NETCORE
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
            if (httpReq.OriginalRequest is HttpRequest aspNet)
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
                if (httpReq.OriginalRequest is HttpListenerRequest httpListener)
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
            if (httpRes.OriginalResponse is HttpResponse aspNet)
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
                if (httpRes.OriginalResponse is HttpListenerResponse httpListener)
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
