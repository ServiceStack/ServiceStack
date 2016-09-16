#if NETSTANDARD1_6

using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack.Web;
using System.Net;

namespace ServiceStack.Host.NetCore
{
    public class NetCoreResponse : IHttpResponse
    {
        public void AddHeader(string name, string value)
        {
            throw new NotImplementedException();
        }

        public string GetHeader(string name)
        {
            throw new NotImplementedException();
        }

        public void Redirect(string url)
        {
            throw new NotImplementedException();
        }

        public void Write(string text)
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void End()
        {
            throw new NotImplementedException();
        }

        public void Flush()
        {
            throw new NotImplementedException();
        }

        public void SetContentLength(long contentLength)
        {
            throw new NotImplementedException();
        }

        public object OriginalResponse { get; }
        public IRequest Request { get; }
        public int StatusCode { get; set; }
        public string StatusDescription { get; set; }
        public string ContentType { get; set; }
        public Stream OutputStream { get; }
        public object Dto { get; set; }
        public bool UseBufferedStream { get; set; }
        public bool IsClosed { get; }
        public bool KeepAlive { get; set; }
        public Dictionary<string, object> Items { get; }
        public void SetCookie(Cookie cookie)
        {
            throw new NotImplementedException();
        }

        public void ClearCookies()
        {
            throw new NotImplementedException();
        }

        public ICookies Cookies { get; }
    }
}

#endif