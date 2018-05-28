#if !NETSTANDARD2_0 

//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using ServiceStack.Configuration;
using ServiceStack.IO;
using ServiceStack.Web;

namespace ServiceStack.Host.HttpListener
{
    public partial class ListenerRequest : IHttpRequest, IHasResolver, IHasVirtualFiles
    {
        private IResolver resolver;
        public IResolver Resolver
        {
            get => resolver ?? Service.GlobalResolver;
            set => resolver = value;
        }

        private readonly HttpListenerRequest request;
        private readonly IHttpResponse response;

        public ListenerRequest(HttpListenerContext httpContext, string operationName, RequestAttributes requestAttributes)
        {
            this.OperationName = operationName;
            this.RequestAttributes = requestAttributes;
            this.request = httpContext.Request;
            this.response = new ListenerResponse(httpContext.Response, this);

            this.RequestPreferences = new RequestPreferences(this);
            this.PathInfo = this.OriginalPathInfo = GetPathInfo();
            this.PathInfo = HostContext.AppHost.ResolvePathInfo(this, OriginalPathInfo);
        }

        private string GetPathInfo()
        {
            var mode = HostContext.Config.HandlerFactoryPath;

            string pathInfo;
            var pos = RawUrl.IndexOf("?", StringComparison.Ordinal);
            if (pos != -1)
            {
                var path = RawUrl.Substring(0, pos);
                pathInfo = HttpRequestExtensions.GetPathInfo(
                    path,
                    mode,
                    mode ?? "");
            }
            else
            {
                pathInfo = RawUrl;
            }

            pathInfo = pathInfo.UrlDecode();
            return pathInfo;
        }
        
        public HttpListenerRequest HttpRequest => request;

        public object OriginalRequest => request;

        public IResponse Response => response;

        public IHttpResponse HttpResponse => response;

        public RequestAttributes RequestAttributes { get; set; }

        public IRequestPreferences RequestPreferences { get; private set; }

        public T TryResolve<T>()
        {
            return this.TryResolveInternal<T>();
        }

        public string OperationName { get; set; }

        public object Dto { get; set; }

        public string GetRawBody()
        {
            if (BufferedStream != null)
            {
                return BufferedStream.ToArray().FromUtf8Bytes();
            }

            var reader = new StreamReader(InputStream);
            return reader.ReadToEnd();
        }

        private string rawUrl;
        public string RawUrl => rawUrl ?? (rawUrl = request.RawUrl.Replace("//", "/"));

        public string AbsoluteUri => request.Url.AbsoluteUri.TrimEnd('/');

        public string UserHostAddress => request.RemoteEndPoint?.Address.ToString() ?? request.UserHostAddress;

        public string XForwardedFor => string.IsNullOrEmpty(request.Headers[HttpHeaders.XForwardedFor]) ? null : request.Headers[HttpHeaders.XForwardedFor];

        public int? XForwardedPort => string.IsNullOrEmpty(request.Headers[HttpHeaders.XForwardedPort]) ? (int?)null : int.Parse(request.Headers[HttpHeaders.XForwardedPort]);

        public string XForwardedProtocol => string.IsNullOrEmpty(request.Headers[HttpHeaders.XForwardedProtocol]) ? null : request.Headers[HttpHeaders.XForwardedProtocol];

        public string XRealIp => string.IsNullOrEmpty(request.Headers[HttpHeaders.XRealIp]) ? null : request.Headers[HttpHeaders.XRealIp];

        public string Accept => string.IsNullOrEmpty(request.Headers[HttpHeaders.Accept]) ? null : request.Headers[HttpHeaders.Accept];

        private string remoteIp;
        public string RemoteIp => remoteIp ??
                                  (remoteIp = XForwardedFor ??
                                              (XRealIp ?? request.RemoteEndPoint?.Address.ToString()));

        public string Authorization => string.IsNullOrEmpty(request.Headers[HttpHeaders.Authorization]) ? null : request.Headers[HttpHeaders.Authorization];

        public bool IsSecureConnection => request.IsSecureConnection || XForwardedProtocol == "https";

        public string[] AcceptTypes => request.AcceptTypes;

        private Dictionary<string, object> items;
        public Dictionary<string, object> Items => items ?? (items = new Dictionary<string, object>());

        private string responseContentType;
        public string ResponseContentType
        {
            get => responseContentType ?? (responseContentType = this.GetResponseContentType());
            set
            {
                this.responseContentType = value;
                HasExplicitResponseContentType = true;
            }
        }

        public bool HasExplicitResponseContentType { get; private set; }

        public string PathInfo { get; }

        public string OriginalPathInfo { get; }

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

        public string UserAgent => request.UserAgent;

        private NameValueCollection headers;
        public NameValueCollection Headers => headers ?? (headers = request.Headers);

        private NameValueCollection queryString;
        public NameValueCollection QueryString => queryString ?? (queryString = HttpUtility.ParseQueryString(request.Url.Query));

        private NameValueCollection formData;
        public NameValueCollection FormData => formData ?? (formData = this.Form);

        public bool IsLocal => request.IsLocal;

        private string httpMethod;
        public string HttpMethod => httpMethod
            ?? (httpMethod = this.GetParamInRequestHeader(HttpHeaders.XHttpMethodOverride)
            ?? request.HttpMethod);

        public string Verb => HttpMethod;

        public string Param(string name)
        {
            return Headers[name]
                ?? QueryString[name]
                ?? FormData[name];
        }

        public string ContentType => request.ContentType;

        public Encoding contentEncoding;
        public Encoding ContentEncoding
        {
            get => contentEncoding ?? request.ContentEncoding;
            set => contentEncoding = value;
        }

        public Uri UrlReferrer => request.UrlReferrer;

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
            get => BufferedStream != null;
            set => BufferedStream = value
                ? BufferedStream ?? new MemoryStream(request.InputStream.ReadFully())
                : null;
        }

        public MemoryStream BufferedStream { get; set; }
        public Stream InputStream => this.GetInputStream(BufferedStream ?? request.InputStream);

        public long ContentLength => request.ContentLength64;

        private IHttpFile[] httpFiles;
        public IHttpFile[] Files
        {
            get
            {
                if (httpFiles == null)
                {
                    if (files == null)
                        return httpFiles = TypeConstants<IHttpFile>.EmptyArray;

                    httpFiles = new IHttpFile[files.Count];
                    for (int i = 0; i < files.Count; i++)
                    {
                        var reqFile = files[i];
                        httpFiles[i] = new HttpFile
                        {
                            Name = files.AllKeys[i],
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
            if (stream is MemoryStream other)
            {
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
            var pos = listenerUrl.IndexOf("://", StringComparison.OrdinalIgnoreCase);
            if (pos == -1) return null;
            var startHostUrl = listenerUrl.Substring(pos + "://".Length);
            var endPos = startHostUrl.IndexOf('/');
            if (endPos == -1) return null;
            var endHostUrl = startHostUrl.Substring(endPos + 1);
            return string.IsNullOrEmpty(endHostUrl) ? null : endHostUrl.TrimEnd('/');
        }

        public static string NormalizePathInfo(string pathInfo, string handlerPath)
        {
            if (handlerPath != null && pathInfo.TrimStart('/').StartsWith(
                handlerPath, StringComparison.OrdinalIgnoreCase))
            {
                return pathInfo.TrimStart('/').Substring(handlerPath.Length);
            }

            return pathInfo;
        }

        private IVirtualFile file;
        public IVirtualFile GetFile() => file ?? (file = HostContext.VirtualFileSources.GetFile(PathInfo));

        private IVirtualDirectory dir;
        public IVirtualDirectory GetDirectory() => dir ?? (dir = HostContext.VirtualFileSources.GetDirectory(PathInfo));

        private bool? isDirectory;
        public bool IsDirectory
        {
            get
            {
                if (isDirectory == null)
                {
                    isDirectory = dir != null || HostContext.VirtualFileSources.DirectoryExists(PathInfo);
                    if (isDirectory == true)
                        isFile = false;
                }
                return isDirectory.Value;
            }
        }

        private bool? isFile;
        public bool IsFile
        {
            get
            {
                if (isFile == null)
                {
                    isFile = GetFile() != null;
                    if (isFile == true)
                        isDirectory = false;                    
                }
                return isFile.Value;
            }
        }
    }

}

#endif