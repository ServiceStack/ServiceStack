using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using Funke;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Extensions
{
    public partial class HttpListenerRequestWrapper
        : IHttpRequest
    {
        private static readonly string physicalFilePath;
        private readonly HttpListenerRequest request;
        public Container Container { get; set; }

        static HttpListenerRequestWrapper()
        {
            physicalFilePath = "~".MapAbsolutePath();
        }

        public HttpListenerRequest Request
        {
            get { return request; }
        }

        public object OriginalRequest
        {
            get { return request; }
        }

        public HttpListenerRequestWrapper(HttpListenerRequest request)
            : this(null, request) { }

        public HttpListenerRequestWrapper(
            string operationName, HttpListenerRequest request)
        {
            this.OperationName = operationName;
            this.request = request;
        }

        public T TryResolve<T>()
        {
            return Container == null
                ? EndpointHost.AppHost.TryResolve<T>()
                : Container.TryResolve<T>();
        }

        public string OperationName { get; set; }

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
            get { return request.IsSecureConnection; }
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
            get { return responseContentType ?? (responseContentType = this.GetResponseContentType()); }
            set { this.responseContentType = value; }
        }

        private string pathInfo;
        public string PathInfo
        {
            get
            {
                if (this.pathInfo == null)
                {
                    var mode = EndpointHost.Config.ServiceStackHandlerFactoryPath;

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

        public NameValueCollection Headers
        {
            get { return request.Headers; }
        }

        private NameValueCollection queryString;
        public NameValueCollection QueryString
        {
            get { return queryString ?? (queryString = HttpUtility.ParseQueryString(request.Url.Query)); }
        }

        public NameValueCollection FormData
        {
            get { return this.Form; }
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

        public string ContentType
        {
            get { return request.ContentType; }
        }

        public Encoding ContentEncoding
        {
            get { return request.ContentEncoding; }
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

        public string ApplicationFilePath
        {
            get { return physicalFilePath; }
        }

        private IFile[] _files;
        public IFile[] Files
        {
            get
            {
                if (_files == null)
                {
                    if (files == null)
                        return _files = new IFile[0];

                    _files = new IFile[files.Count];
                    for (var i = 0; i < files.Count; i++)
                    {
                        var reqFile = files[i];

                        _files[i] = new HttpFile
                        {
                            ContentType = reqFile.ContentType,
                            ContentLength = reqFile.ContentLength,
                            FileName = reqFile.FileName,
                            InputStream = reqFile.InputStream,
                        };
                    }
                }
                return _files;
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
