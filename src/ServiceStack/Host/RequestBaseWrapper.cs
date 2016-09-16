#if !NETSTANDARD1_6

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
        }

        private HttpRequestBase original;
        public HttpRequestBase Original => original ?? (HttpRequestBase) request.OriginalRequest;

        private Uri url;
        public override Uri Url => url ?? new Uri(request.AbsoluteUri);

        public string rawUrl;
        public override string RawUrl => rawUrl ?? request.RawUrl;

        public string contentType;
        public override string ContentType => contentType ?? request.ContentType;

        public string httpMethod;
        public override string HttpMethod => httpMethod ?? request.HttpMethod;

        public string applicationPath;
        public override string ApplicationPath => applicationPath ?? Original.ApplicationPath;

        public string pathInfo;
        public override string PathInfo => pathInfo ?? request.PathInfo;

        public string path;
        public override string Path => path ?? Original.Path;

        public int? contentLength;
        public override int ContentLength => (int) (contentLength ?? request.ContentLength);

        public string requestType;
        public override string RequestType => requestType ?? Original.RequestType;

        public bool? isAuthenticated;
        public override bool IsAuthenticated => isAuthenticated ?? Original.IsAuthenticated;

        public string physicalApplicationPath;
        public override string PhysicalApplicationPath => physicalApplicationPath ?? Original.PhysicalApplicationPath;

        public string physicalPath;
        public override string PhysicalPath => physicalPath ?? Original.PhysicalPath;

        public string filePath;
        public override string FilePath => filePath ?? Original.FilePath;

        public string currentExecutionFilePath;
        public override string CurrentExecutionFilePath => currentExecutionFilePath ?? Original.CurrentExecutionFilePath;

        public bool? isLocal;
        public override bool IsLocal => isLocal ?? request.IsLocal;

        public bool? isSecureConnection;
        public override bool IsSecureConnection => isSecureConnection ?? request.IsSecureConnection;

        public Uri urlReferrer;
        public override Uri UrlReferrer => urlReferrer ?? request.UrlReferrer;

        public string userAgent;
        public override string UserAgent => userAgent ?? request.UserAgent;

        public string userHostAddress;
        public override string UserHostAddress => userHostAddress ?? request.UserHostAddress;

        public string userHostName;
        public override string UserHostName => userHostName ?? Original.UserHostName;

        public System.Text.Encoding contentEncoding;
        public override System.Text.Encoding ContentEncoding
        {
            get
            {
                return contentEncoding ?? Original.ContentEncoding;
            }
            set
            {
                contentEncoding = value;
            }
        }

        public System.Security.Principal.WindowsIdentity logonUserIdentity;
        public override System.Security.Principal.WindowsIdentity LogonUserIdentity => 
            logonUserIdentity ?? Original.LogonUserIdentity;

        public string[] userLanguages;
        public override string[] UserLanguages => userLanguages ?? Original.UserLanguages;

        public HttpCookieCollection cookies;
        public override HttpCookieCollection Cookies => cookies ?? Original.Cookies;

        public NameValueCollection @params;
        public override NameValueCollection Params => @params ?? (@params = Original.Params);

        public NameValueCollection queryString;
        public override NameValueCollection QueryString => queryString ?? request.QueryString.ToNameValueCollection();

        public NameValueCollection formData;
        public override NameValueCollection Form => formData ?? request.FormData.ToNameValueCollection();

        public NameValueCollection headers;
        public override NameValueCollection Headers => headers ?? request.Headers.ToNameValueCollection();

        public NameValueCollection serverVariables;
        public override NameValueCollection ServerVariables => serverVariables ?? Original.ServerVariables;
    }
}

#endif
