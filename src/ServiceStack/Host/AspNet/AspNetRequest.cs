

using System.Threading.Tasks;
#if !NETCORE

//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Web;
using ServiceStack.Configuration;
using ServiceStack.IO;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host.AspNet
{
    public class AspNetRequest
        : IHttpRequest, IHasResolver, IHasVirtualFiles, IHasBufferedStream
    {
        public static ILog log = LogManager.GetLogger(typeof(AspNetRequest));

        private IResolver resolver;
        public IResolver Resolver
        {
            get => resolver ?? Service.GlobalResolver;
            set => resolver = value;
        }

        private readonly HttpRequestBase request;
        private readonly IHttpResponse response;
        
        public AspNetRequest(HttpContextBase httpContext, string operationName = null)
            : this(httpContext, operationName, RequestAttributes.None)
        {
            this.RequestAttributes = this.GetAttributes() | RequestAttributes.Http;
        }

        public AspNetRequest(HttpContextBase httpContext, string operationName, RequestAttributes requestAttributes)
        {
            this.OperationName = operationName;
            this.RequestAttributes = requestAttributes;
            this.request = httpContext.Request;
            try
            {
                this.response = new AspNetResponse(httpContext.Response, this);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message, ex);
            }

            this.RequestPreferences = new RequestPreferences(httpContext);

            if (httpContext.Items.Count > 0)
            {
                foreach (var key in httpContext.Items.Keys)
                {
                    var strKey = key as string;
                    if (strKey == null) continue;
                    Items[strKey] = httpContext.Items[key];
                }
            }

            this.PathInfo = this.OriginalPathInfo = GetPathInfo();
            this.PathInfo = HostContext.AppHost.ResolvePathInfo(this, OriginalPathInfo);
        }

        public HttpRequestBase HttpRequest => request;

        public object OriginalRequest => request;

        public IResponse Response => response;

        public IHttpResponse HttpResponse => response;

        public RequestAttributes RequestAttributes { get; set; }

        public IRequestPreferences RequestPreferences { get; }

        public T TryResolve<T>()
        {
            return this.TryResolveInternal<T>();
        }

        public string OperationName { get; set; }

        public object Dto { get; set; }

        public string ContentType => request.ContentType;

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

        public bool IsLocal => request.IsLocal;

        public string UserAgent => request.UserAgent;

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
                HasExplicitResponseContentType = true;
            }
        }

        public bool HasExplicitResponseContentType { get; private set; }

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
                        if (httpCookie == null)
                            continue;

                        Cookie cookie = null;

                        // try-catch needed as malformed cookie names (e.g. '$Version') can be returned
                        // from Cookie.Name, but the Cookie constructor will throw for these names.
                        try
                        {
                            var name = httpCookie.Name.StartsWith("$")
                                ? httpCookie.Name.Substring(1)
                                : httpCookie.Name;

                            cookie = new Cookie(name, httpCookie.Value, httpCookie.Path, httpCookie.Domain)
                            {
                                HttpOnly = httpCookie.HttpOnly,
                                Secure = httpCookie.Secure,
                                Expires = httpCookie.Expires,
                            };
                        }
                        catch(Exception ex)
                        {
                            log.Warn("Error trying to create System.Net.Cookie: " + httpCookie.Name, ex);
                        }

                        if (cookie != null)
                            cookies[httpCookie.Name] = cookie;
                    }
                }
                return cookies;
            }
        }

        private NameValueCollection headers;
        public NameValueCollection Headers => headers ?? (headers = request.Headers);

        private NameValueCollection queryString;
        public NameValueCollection QueryString => queryString ?? (queryString = request.QueryString);

        private NameValueCollection formData;
        public NameValueCollection FormData => formData ?? (formData = request.Form);

        public Task<string> GetRawBodyAsync() => Task.FromResult(GetRawBody());

        public string RawUrl => request.RawUrl;

        public string AbsoluteUri
        {
            get
            {
                try
                {
                    return HostContext.Config.StripApplicationVirtualPath
                        ? request.Url.GetLeftAuthority()
                            .CombineWith(HostContext.Config.HandlerFactoryPath)
                            .CombineWith(PathInfo)
                            .TrimEnd('/')
                        : request.Url.AbsoluteUri.TrimEnd('/');
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
            get
            {
                try
                {
                    return request.UserHostAddress;
                }
                catch (Exception)
                {
                    return null; //Can throw in Mono FastCGI Host
                }
            }
        }

        public string XForwardedFor => 
            string.IsNullOrEmpty(request.Headers[HttpHeaders.XForwardedFor]) ? null : request.Headers[HttpHeaders.XForwardedFor];

        public int? XForwardedPort => 
            string.IsNullOrEmpty(request.Headers[HttpHeaders.XForwardedPort]) ? (int?) null : int.Parse(request.Headers[HttpHeaders.XForwardedPort]);

        public string XForwardedProtocol => 
            string.IsNullOrEmpty(request.Headers[HttpHeaders.XForwardedProtocol]) ? null : request.Headers[HttpHeaders.XForwardedProtocol];

        public string XRealIp => 
            string.IsNullOrEmpty(request.Headers[HttpHeaders.XRealIp]) ? null : request.Headers[HttpHeaders.XRealIp];

        public string Accept => 
            string.IsNullOrEmpty(request.Headers[HttpHeaders.Accept]) ? null : request.Headers[HttpHeaders.Accept];

        private string remoteIp;
        public string RemoteIp => remoteIp ??= XForwardedFor ?? (XRealIp ?? request.UserHostAddress);

        public string Authorization
        {
            get
            {
                var auth = request.Headers[HttpHeaders.Authorization];
                return string.IsNullOrEmpty(auth) ? null : auth;
            }
        }

        public bool IsSecureConnection => request.IsSecureConnection 
            || XForwardedProtocol == "https" 
            || (RequestAttributes & RequestAttributes.Secure) == RequestAttributes.Secure;

        public string[] AcceptTypes => request.AcceptTypes;

        public string PathInfo { get; }

        public string OriginalPathInfo { get; } 

        public string GetPathInfo()
        {
            if (!string.IsNullOrEmpty(request.PathInfo)) 
                return request.PathInfo;

            var mode = HostContext.Config.HandlerFactoryPath;
            var appPath = string.IsNullOrEmpty(request.ApplicationPath)
                ? HttpRequestExtensions.WebHostDirectoryName
                : request.ApplicationPath.TrimStart('/');

            //mod_mono: /CustomPath35/api//default.htm
            var path = Env.IsMono ? request.Path.Replace("//", "/") : request.Path;
            return HttpRequestExtensions.GetPathInfo(path, mode, appPath);
        }
        
        public string UrlHostName => request.GetUrlHostName();

        public MemoryStream BufferedStream { get; set; }
        public Stream InputStream => this.GetInputStream(BufferedStream ?? request.InputStream);

        public bool UseBufferedStream
        {
            get => BufferedStream != null;
            set => BufferedStream = value
                ? BufferedStream ?? request.InputStream.CreateBufferedStream()
                : null;
        }

        public string GetRawBody()
        {
            if (BufferedStream != null)
                return BufferedStream.ReadBufferedStreamToEnd(this);

            return InputStream.ReadToEnd();
        }

        public long ContentLength => request.ContentLength;

        private IHttpFile[] httpFiles;
        public IHttpFile[] Files
        {
            get
            {
                if (httpFiles == null)
                {
                    httpFiles = new IHttpFile[request.Files.Count];
                    for (int i = 0; i < request.Files.Count; i++)
                    {
                        var reqFile = request.Files[i];
                        httpFiles[i] = new HttpFile
                        {
                            Name = request.Files.AllKeys[i],
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

        public Uri UrlReferrer => request.UrlReferrer;
        
        
        private IVirtualFile file;
        public IVirtualFile GetFile() => file ??= VirtualPathUtils.IsValidFilePath(PathInfo) ? HostContext.VirtualFileSources.GetFile(PathInfo) : null;

        private IVirtualDirectory dir;
        public IVirtualDirectory GetDirectory() => dir ??= VirtualPathUtils.IsValidFilePath(PathInfo) ? HostContext.VirtualFileSources.GetDirectory(PathInfo) : null;

        private bool? isDirectory;
        public bool IsDirectory
        {
            get
            {
                if (isDirectory == null)
                {
                    isDirectory = dir != null || (VirtualPathUtils.IsValidFilePath(PathInfo) && HostContext.VirtualFileSources.DirectoryExists(PathInfo));
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
