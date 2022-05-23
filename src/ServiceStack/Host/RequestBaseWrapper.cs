#if NETFX || NET472

using System;
using System.Collections.Specialized;
using System.Web;
using ServiceStack.Web;

namespace ServiceStack.Host
{
    public class RequestBaseWrapper : HttpRequestBase
    {
        public readonly IHttpRequest request;

        public RequestBaseWrapper(IHttpRequest request)
        {
            this.request = request;
            Original = (HttpRequestBase)this.request.OriginalRequest;
            ContentEncoding = Original.ContentEncoding;
        }

        public HttpRequestBase Original { get; set; }

        public override Uri Url => new Uri(request.AbsoluteUri);

        public override string RawUrl => request.RawUrl;

        public override string ContentType => request.ContentType;

        public override string HttpMethod => request.HttpMethod;

        public override string ApplicationPath => Original.ApplicationPath;

        public override string PathInfo => request.PathInfo;

        public override string Path => Original.Path;

        public override int ContentLength => (int)request.ContentLength;

        public override string RequestType => Original.RequestType;

        public override bool IsAuthenticated => Original.IsAuthenticated;

        public override string PhysicalApplicationPath => Original.PhysicalApplicationPath;

        public override string PhysicalPath => Original.PhysicalPath;

        public override string FilePath => Original.FilePath;

        public override string CurrentExecutionFilePath => Original.CurrentExecutionFilePath;

        public override bool IsLocal => request.IsLocal;

        public override bool IsSecureConnection => request.IsSecureConnection;

        public override Uri UrlReferrer => request.UrlReferrer;

        public override string UserAgent => request.UserAgent;

        public override string UserHostAddress => request.UserHostAddress;

        public override string UserHostName => Original.UserHostName;

        public override System.Text.Encoding ContentEncoding { get; set; }

        public override System.Security.Principal.WindowsIdentity LogonUserIdentity => Original.LogonUserIdentity;

        public override string[] UserLanguages => Original.UserLanguages;

        public override HttpCookieCollection Cookies => Original.Cookies;

        public override NameValueCollection Params => Original.Params;

        public override NameValueCollection QueryString => request.QueryString;

        public override NameValueCollection Form => request.FormData;

        public override NameValueCollection Headers => request.Headers;

        public override NameValueCollection ServerVariables => Original.ServerVariables;
    }
}

#endif
