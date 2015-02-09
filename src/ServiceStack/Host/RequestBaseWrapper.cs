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

        public HttpRequestBase original;
        public HttpRequestBase Original
        {
            get { return original ?? (HttpRequestBase) request.OriginalRequest; }
        }

        public Uri url;
        public override Uri Url
        {
            get { return url ?? new Uri(request.AbsoluteUri); }
        }

        public string rawUrl;
        public override string RawUrl
        {
            get { return rawUrl ?? request.RawUrl; }
        }

        public string contentType;
        public override string ContentType
        {
            get { return contentType ?? request.ContentType; }
        }

        public string httpMethod;
        public override string HttpMethod
        {
            get { return httpMethod ?? request.HttpMethod; }
        }

        public string applicationPath;
        public override string ApplicationPath
        {
            get { return applicationPath ?? Original.ApplicationPath; }
        }

        public string pathInfo;
        public override string PathInfo
        {
            get { return pathInfo ?? request.PathInfo; }
        }

        public string path;
        public override string Path
        {
            get { return path ?? Original.Path; }
        }

        public int? contentLength;
        public override int ContentLength
        {
            get { return (int) (contentLength ?? request.ContentLength); }
        }

        public string requestType;
        public override string RequestType
        {
            get { return requestType ?? Original.RequestType; }
        }

        public bool? isAuthenticated;
        public override bool IsAuthenticated
        {
            get { return isAuthenticated ?? Original.IsAuthenticated; }
        }

        public string physicalApplicationPath;
        public override string PhysicalApplicationPath
        {
            get { return physicalApplicationPath ?? Original.PhysicalApplicationPath; }
        }

        public string physicalPath;
        public override string PhysicalPath
        {
            get { return physicalPath ?? Original.PhysicalPath; }
        }

        public string filePath;
        public override string FilePath
        {
            get { return filePath ?? Original.FilePath; }
        }

        public string currentExecutionFilePath;
        public override string CurrentExecutionFilePath
        {
            get { return currentExecutionFilePath ?? Original.CurrentExecutionFilePath; }
        }

        public bool? isLocal;
        public override bool IsLocal
        {
            get { return isLocal ?? request.IsLocal; }
        }

        public bool? isSecureConnection;
        public override bool IsSecureConnection
        {
            get { return isSecureConnection ?? request.IsSecureConnection; }
        }

        public Uri urlReferrer;
        public override Uri UrlReferrer
        {
            get { return urlReferrer ?? request.UrlReferrer; }
        }

        public string userAgent;
        public override string UserAgent
        {
            get { return userAgent ?? request.UserAgent; }
        }

        public string userHostAddress;
        public override string UserHostAddress
        {
            get { return userHostAddress ?? request.UserHostAddress; }
        }

        public string userHostName;
        public override string UserHostName
        {
            get { return userHostName ?? Original.UserHostName; }
        }

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
        public override System.Security.Principal.WindowsIdentity LogonUserIdentity
        {
            get { return logonUserIdentity ?? Original.LogonUserIdentity; }
        }

        public string[] userLanguages;
        public override string[] UserLanguages
        {
            get { return userLanguages ?? Original.UserLanguages; }
        }

        public HttpCookieCollection cookies;
        public override HttpCookieCollection Cookies
        {
            get { return cookies ?? Original.Cookies; }
        }

        public NameValueCollection @params;
        public override NameValueCollection Params
        {
            get { return @params ?? (@params = Original.Params); }
        }

        public NameValueCollection queryString;
        public override NameValueCollection QueryString
        {
            get { return queryString ?? request.QueryString.ToNameValueCollection(); }
        }

        public NameValueCollection formData;
        public override NameValueCollection Form
        {
            get { return formData ?? request.FormData.ToNameValueCollection(); }
        }

        public NameValueCollection headers;
        public override NameValueCollection Headers
        {
            get { return headers ?? request.Headers.ToNameValueCollection(); }
        }

        public NameValueCollection serverVariables;
        public override NameValueCollection ServerVariables
        {
            get { return serverVariables ?? Original.ServerVariables; }
        }
    }
}