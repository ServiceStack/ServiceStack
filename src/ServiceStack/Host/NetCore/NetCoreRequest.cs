#if NETSTANDARD1_6

using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack.Web;
using System.Net;

namespace ServiceStack.Host.NetCore
{
    public class NetCoreRequest : IHttpRequest
    {
        public T TryResolve<T>()
        {
            throw new NotImplementedException();
        }

        public string GetRawBody()
        {
            throw new NotImplementedException();
        }

        public object OriginalRequest { get; }
        public IResponse Response { get; }
        public string OperationName { get; set; }
        public string Verb { get; }
        public RequestAttributes RequestAttributes { get; set; }
        public IRequestPreferences RequestPreferences { get; }
        public object Dto { get; set; }
        public string ContentType { get; }
        public bool IsLocal { get; }
        public string UserAgent { get; }
        public IDictionary<string, Cookie> Cookies { get; }
        public string ResponseContentType { get; set; }
        public bool HasExplicitResponseContentType { get; }
        public Dictionary<string, object> Items { get; }
        public INameValueCollection Headers { get; }
        public INameValueCollection QueryString { get; }
        public INameValueCollection FormData { get; }
        public bool UseBufferedStream { get; set; }
        public string RawUrl { get; }
        public string AbsoluteUri { get; }
        public string UserHostAddress { get; }
        public string RemoteIp { get; }
        public string Authorization { get; }
        public bool IsSecureConnection { get; }
        public string[] AcceptTypes { get; }
        public string PathInfo { get; }
        public Stream InputStream { get; }
        public long ContentLength { get; }
        public IHttpFile[] Files { get; }
        public Uri UrlReferrer { get; }
        public IHttpResponse HttpResponse { get; }
        public string HttpMethod { get; }
        public string XForwardedFor { get; }
        public int? XForwardedPort { get; }
        public string XForwardedProtocol { get; }
        public string XRealIp { get; }
        public string Accept { get; }
    }
}

#endif
