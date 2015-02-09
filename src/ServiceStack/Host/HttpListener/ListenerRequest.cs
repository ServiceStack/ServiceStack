//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using Funq;
using ServiceStack.Web;

namespace ServiceStack.Host.HttpListener
{
    public partial class ListenerRequest : IHttpRequest
    {
        public Container Container { get; set; }
        private readonly HttpListenerRequest request;
        private readonly IHttpResponse response;

        public ListenerRequest(HttpListenerContext httpContext, string operationName, RequestAttributes requestAttributes)
        {
            this.OperationName = operationName;
            this.RequestAttributes = requestAttributes;
            this.request = httpContext.Request;
            this.response = new ListenerResponse(httpContext.Response);

            this.RequestPreferences = new RequestPreferences(this);
        }

        public HttpListenerRequest HttpRequest
        {
            get { return request; }
        }

        public object OriginalRequest
        {
            get { return request; }
        }

        public IResponse Response
        {
            get { return response; }
        }

        public IHttpResponse HttpResponse
        {
            get { return response; }
        }

        public RequestAttributes RequestAttributes { get; set; }

        public IRequestPreferences RequestPreferences { get; private set; }

        public T TryResolve<T>()
        {
            if (typeof(T) == typeof(IHttpRequest))
                throw new Exception("You don't need to use IHttpRequest.TryResolve<IHttpRequest> to resolve itself");

            if (typeof(T) == typeof(IHttpResponse))
                throw new Exception("Resolve IHttpResponse with 'Response' property instead of IHttpRequest.TryResolve<IHttpResponse>");

            return Container == null
                ? HostContext.TryResolve<T>()
                : Container.TryResolve<T>();
        }

        public string OperationName { get; set; }

        public object Dto { get; set; }

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
            get { return request.Url.AbsoluteUri.TrimEnd('/'); }
        }

        public string UserHostAddress
        {
            get { return request.UserHostAddress; }
        }

        public string XForwardedFor
        {
            get
            {
                return String.IsNullOrEmpty(request.Headers[HttpHeaders.XForwardedFor]) ? null : request.Headers[HttpHeaders.XForwardedFor];
            }
        }

        public int? XForwardedPort
        {
            get
            {
                return string.IsNullOrEmpty(request.Headers[HttpHeaders.XForwardedPort]) ? (int?)null : int.Parse(request.Headers[HttpHeaders.XForwardedPort]);
            }
        }

        public string XForwardedProtocol
        {
            get
            {
                return string.IsNullOrEmpty(request.Headers[HttpHeaders.XForwardedProtocol]) ? null : request.Headers[HttpHeaders.XForwardedProtocol];
            }
        }

        public string XRealIp
        {
            get
            {
                return String.IsNullOrEmpty(request.Headers[HttpHeaders.XRealIp]) ? null : request.Headers[HttpHeaders.XRealIp];
            }
        }

        private string remoteIp;
        public string RemoteIp
        {
            get
            {
                return remoteIp ??
                    (remoteIp = XForwardedFor ??
                                (XRealIp ??
                                ((request.RemoteEndPoint != null) ? request.RemoteEndPoint.Address.ToString() : null)));
            }
        }

        public bool IsSecureConnection
        {
            get { return request.IsSecureConnection || XForwardedProtocol == "https"; }
        }

        public string[] AcceptTypes
        {
            get { return request.AcceptTypes; }
        }

        private Dictionary<string, object> items;
        public Dictionary<string, object> Items
        {
            get { return items ?? (items = new Dictionary<string, object>()); }
        }

        private string responseContentType;
        public string ResponseContentType
        {
            get { return responseContentType 
                ?? (responseContentType = this.GetResponseContentType()); }
            set
            {
                this.responseContentType = value;
                HasExplicitResponseContentType = true;
            }
        }

        public bool HasExplicitResponseContentType { get; private set; }

        private string pathInfo;
        public string PathInfo
        {
            get
            {
                if (this.pathInfo == null)
                {
                    var mode = HostContext.Config.HandlerFactoryPath;

                    var pos = request.RawUrl.IndexOf("?");
                    if (pos != -1)
                    {
                        var path = request.RawUrl.Substring(0, pos);
                        this.pathInfo = HttpRequestExtensions.GetPathInfo(
                            path,
                            mode,
                            mode ?? "");
                    }
                    else
                    {
                        this.pathInfo = request.RawUrl;
                    }

                    this.pathInfo = this.pathInfo.UrlDecode();
                    this.pathInfo = NormalizePathInfo(pathInfo, mode);
                }
                return this.pathInfo;
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
                        cookies[httpCookie.Name] = httpCookie;
                    }
                }

                return cookies;
            }
        }

        public string UserAgent
        {
            get { return request.UserAgent; }
        }

        private NameValueCollectionWrapper headers;
        public INameValueCollection Headers
        {
            get { return headers ?? (headers = new NameValueCollectionWrapper(request.Headers)); }
        }

        private NameValueCollectionWrapper queryString;
        public INameValueCollection QueryString
        {
            get { return queryString ?? (queryString = new NameValueCollectionWrapper(HttpUtility.ParseQueryString(request.Url.Query))); }
        }

        private NameValueCollectionWrapper formData;
        public INameValueCollection FormData
        {
            get { return formData ?? (formData = new NameValueCollectionWrapper(this.Form)); }
        }

        public bool IsLocal
        {
            get { return request.IsLocal; }
        }

        private string httpMethod;
        public string HttpMethod
        {
            get
            {
                return httpMethod
                    ?? (httpMethod = this.GetParamInRequestHeader(HttpHeaders.XHttpMethodOverride)
                    ?? request.HttpMethod);
            }
        }

        public string Verb
        {
            get { return HttpMethod; }
        }

        public string Param(string name)
        {
            return Headers[name]
                ?? QueryString[name]
                ?? FormData[name];
        }

        public string ContentType
        {
            get { return request.ContentType; }
        }

        public Encoding contentEncoding;
        public Encoding ContentEncoding
        {
            get { return contentEncoding ?? request.ContentEncoding; }
            set { contentEncoding = value; }
        }

        public Uri UrlReferrer
        {
            get { return request.UrlReferrer; }
        }

        public static Encoding GetEncoding(string contentTypeHeader)
        {
            var param = GetParameter(contentTypeHeader, "charset=");
            if (param == null) return null;
            try
            {
                return Encoding.GetEncoding(param);
            }
            catch (ArgumentException)
            {
                return null;
            }
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
            get { return request.ContentLength64; }
        }

        private IHttpFile[] httpFiles;
        public IHttpFile[] Files
        {
            get
            {
                if (httpFiles == null)
                {
                    if (files == null)
                        return httpFiles = new IHttpFile[0];

                    httpFiles = new IHttpFile[files.Count];
                    for (var i = 0; i < files.Count; i++)
                    {
                        var reqFile = files[i];

                        httpFiles[i] = new HttpFile
                        {
                            ContentType = reqFile.ContentType,
                            ContentLength = reqFile.ContentLength,
                            FileName = reqFile.FileName,
                            InputStream = reqFile.InputStream,
                        };
                    }
                }
                return httpFiles;
            }
        }

        static Stream GetSubStream(Stream stream)
        {
            if (stream is MemoryStream)
            {
                var other = (MemoryStream)stream;
                try
                {
                    return new MemoryStream(other.GetBuffer(), 0, (int)other.Length, false, true);
                }
                catch (UnauthorizedAccessException)
                {
                    return new MemoryStream(other.ToArray(), 0, (int)other.Length, false, true);
                }
            }

            return stream;
        }

        static void EndSubStream(Stream stream)
        {
        }

        public static string GetHandlerPathIfAny(string listenerUrl)
        {
            if (listenerUrl == null) return null;
            var pos = listenerUrl.IndexOf("://", StringComparison.InvariantCultureIgnoreCase);
            if (pos == -1) return null;
            var startHostUrl = listenerUrl.Substring(pos + "://".Length);
            var endPos = startHostUrl.IndexOf('/');
            if (endPos == -1) return null;
            var endHostUrl = startHostUrl.Substring(endPos + 1);
            return String.IsNullOrEmpty(endHostUrl) ? null : endHostUrl.TrimEnd('/');
        }

        public static string NormalizePathInfo(string pathInfo, string handlerPath)
        {
            if (handlerPath != null && pathInfo.TrimStart('/').StartsWith(
                handlerPath, StringComparison.InvariantCultureIgnoreCase))
            {
                return pathInfo.TrimStart('/').Substring(handlerPath.Length);
            }

            return pathInfo;
        }
    }

}
