using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Web;
using Funq;
using ServiceStack.Text;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Extensions
{
    public class HttpRequestWrapper
        : IHttpRequest
    {
        private static readonly string physicalFilePath;
        public Container Container { get; set; }
        private readonly HttpRequest request;

        static HttpRequestWrapper()
        {
            physicalFilePath = "~".MapHostAbsolutePath();
        }

        public HttpRequest Request
        {
            get { return request; }
        }

        public object OriginalRequest
        {
            get { return request; }
        }

        public HttpRequestWrapper(HttpRequest request)
            : this(null, request)
        {
        }

        public HttpRequestWrapper(string operationName, HttpRequest request)
        {
            this.OperationName = operationName;
            this.request = request;
            this.Container = Container;
        }

        public T TryResolve<T>()
        {
            return Container == null
                ? EndpointHost.AppHost.TryResolve<T>()
                : Container.TryResolve<T>();
        }

        public string OperationName { get; set; }

        public string ContentType
        {
            get { return request.ContentType; }
        }
        
        private string httpMethod;
        public string HttpMethod
        {
            get
            {
                return httpMethod
                    ?? (httpMethod = Param(HttpHeaders.XHttpMethodOverride)
                    ?? request.HttpMethod);
            }
        }

        public string Param(string name)
        {
            return Headers[name]
                ?? QueryString[name]
                ?? FormData[name];
        }

        public bool IsLocal
        {
            get { return request.IsLocal; }
        }

        public string UserAgent
        {
            get { return request.UserAgent; }
        }

        private Dictionary<string, object> items;
        public Dictionary<string, object> Items
        {
            get
            {
                if (items == null)
                {
                    items = new Dictionary<string, object>();
                }
                return items;
            }
        }

        private string responseContentType;
        public string ResponseContentType
        {
            get
            {
                if (responseContentType == null)
                {
                    responseContentType = this.GetResponseContentType();
                }
                return responseContentType;
            }
            set
            {
                this.responseContentType = value;
            }
        }

        private Dictionary<string, Cookie> cookies;
        public IDictionary<string, Cookie> Cookies
        {
            get
            {
                if (cookies == null)
                {
                    cookies = new Dictionary<string, Cookie>();
                    for (var i = 0; i < this.request.Cookies.Count; i++)
                    {
                        var httpCookie = this.request.Cookies[i];
                        var cookie = new Cookie(
                            httpCookie.Name, httpCookie.Value, httpCookie.Path, httpCookie.Domain)
                            {
                                HttpOnly = httpCookie.HttpOnly,
                                Secure = httpCookie.Secure,
                                Expires = httpCookie.Expires,
                            };
                        cookies[httpCookie.Name] = cookie;
                    }
                }
                return cookies;
            }
        }

        public NameValueCollection Headers
        {
            get { return request.Headers; }
        }

        public NameValueCollection QueryString
        {
            get { return request.QueryString; }
        }

        public NameValueCollection FormData
        {
            get { return request.Form; }
        }

        public string GetRawBody()
        {
            if (bufferedStream != null)
            {
                return bufferedStream.ToArray().FromUtf8Bytes();
            }

            using (var reader = new StreamReader(InputStream))
            {
                return reader.ReadToEnd();
            }
        }

        public string RawUrl
        {
            get { return request.RawUrl; }
        }

        public string AbsoluteUri
        {
            get
            {
                try
                {
                    return request.Url.AbsoluteUri.TrimEnd('/');
                }
                catch (Exception)
                {
                    //fastcgi mono, do a 2nd rounds best efforts
                    return "http://" + request.UserHostName + request.RawUrl;
                }
            }
        }

        public string UserHostAddress
        {
            get { return request.UserHostAddress; }
        }

        public string XForwardedFor
        {
            get
            {
                return string.IsNullOrEmpty(request.Headers[HttpHeaders.XForwardedFor]) ? null : request.Headers[HttpHeaders.XForwardedFor];
            }
        }

        public string XRealIp
        {
            get
            {
                return string.IsNullOrEmpty(request.Headers[HttpHeaders.XRealIp]) ? null : request.Headers[HttpHeaders.XRealIp];
            }
        }

        private string remoteIp;
        public string RemoteIp
        {
            get
            {
                return remoteIp ?? (remoteIp = XForwardedFor ?? (XRealIp ?? request.UserHostAddress));
            }
        }

        public bool IsSecureConnection
        {
            get { return request.IsSecureConnection; }
        }

        public string[] AcceptTypes
        {
            get { return request.AcceptTypes; }
        }

        public string PathInfo
        {
            get { return request.GetPathInfo(); }
        }

        public string UrlHostName
        {
            get { return request.GetUrlHostName(); }
        }

        public bool UseBufferedStream
        {
            get { return bufferedStream != null; }
            set
            {
                bufferedStream = value
                    ? bufferedStream ?? new MemoryStream(request.InputStream.ReadFully())
                    : null;
            }
        }

        private MemoryStream bufferedStream;
        public Stream InputStream
        {
            get { return bufferedStream ?? request.InputStream; }
        }

        public long ContentLength
        {
            get { return request.ContentLength; }
        }

        private IFile[] files;
        public IFile[] Files
        {
            get
            {
                if (files == null)
                {
                    files = new IFile[request.Files.Count];
                    for (var i = 0; i < request.Files.Count; i++)
                    {
                        var reqFile = request.Files[i];

                        files[i] = new HttpFile
                        {
                            ContentType = reqFile.ContentType,
                            ContentLength = reqFile.ContentLength,
                            FileName = reqFile.FileName,
                            InputStream = reqFile.InputStream,
                        };
                    }
                }
                return files;
            }
        }

        public string ApplicationFilePath
        {
            get { return physicalFilePath; }
        }
    }

}