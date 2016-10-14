#if !NETSTANDARD1_6

//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Web;
using Funq;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Web;

namespace ServiceStack.Host.AspNet
{
    public class AspNetRequest
        : IHttpRequest, IHasResolver
    {
        public static ILog log = LogManager.GetLogger(typeof(AspNetRequest));

        [Obsolete("Use Resolver")]
        public Container Container { get { throw new NotSupportedException("Use Resolver"); } }

        private IResolver resolver;
        public IResolver Resolver
        {
            get { return resolver ?? Service.GlobalResolver; }
            set { resolver = value; }
        }

        private readonly HttpRequestBase request;
        private readonly IHttpResponse response;
        
        public AspNetRequest(HttpContextBase httpContext, string operationName = null)
            : this(httpContext, operationName, RequestAttributes.None)
        {
            this.RequestAttributes = this.GetAttributes();
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

            if (httpContext.Items != null && httpContext.Items.Count > 0)
            {
                foreach (var key in httpContext.Items.Keys)
                {
                    var strKey = key as string;
                    if (strKey == null) continue;
                    Items[strKey] = httpContext.Items[key];
                }
            }
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
                            cookie = new Cookie(httpCookie.Name, httpCookie.Value, httpCookie.Path, httpCookie.Domain)
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

        private NameValueCollectionWrapper headers;
        public INameValueCollection Headers => headers ?? (headers = new NameValueCollectionWrapper(request.Headers));

        private NameValueCollectionWrapper queryString;
        public INameValueCollection QueryString => queryString ?? (queryString = new NameValueCollectionWrapper(request.QueryString));

        private NameValueCollectionWrapper formData;
        public INameValueCollection FormData => formData ?? (formData = new NameValueCollectionWrapper(request.Form));

        public string GetRawBody()
        {
            if (BufferedStream != null)
            {
                return BufferedStream.ToArray().FromUtf8Bytes();
            }

            using (var reader = new StreamReader(InputStream))
            {
                return reader.ReadToEnd();
            }
        }

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
        public string RemoteIp => 
            remoteIp ?? (remoteIp = XForwardedFor ?? (XRealIp ?? request.UserHostAddress));

        public string Authorization => 
            string.IsNullOrEmpty(request.Headers[HttpHeaders.Authorization]) ? null : request.Headers[HttpHeaders.Authorization];

        public bool IsSecureConnection => 
            request.IsSecureConnection || XForwardedProtocol == "https";

        public string[] AcceptTypes => request.AcceptTypes;

        public string PathInfo => request.GetPathInfo();

        public string UrlHostName => request.GetUrlHostName();

        public bool UseBufferedStream
        {
            get { return BufferedStream != null; }
            set
            {
                BufferedStream = value
                    ? BufferedStream ?? new MemoryStream(request.InputStream.ReadFully())
                    : null;
            }
        }

        public MemoryStream BufferedStream { get; set; }
        public Stream InputStream => BufferedStream ?? request.InputStream;

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
    }

}

#endif
