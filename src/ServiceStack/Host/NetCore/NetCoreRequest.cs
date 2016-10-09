#if NETSTANDARD1_6

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Collections.Specialized;

using ServiceStack.Web;
using ServiceStack.Logging;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Configuration;

namespace ServiceStack.Host.NetCore
{
    public class NetCoreRequest : IHttpRequest, IHasResolver
    {
        public static ILog log = LogManager.GetLogger(typeof(NetCoreRequest));

        private IResolver resolver;
        public IResolver Resolver
        {
            get { return resolver ?? Service.GlobalResolver; }
            set { resolver = value; }
        }

        private readonly HttpContext context;
        private readonly HttpRequest request;

        public NetCoreRequest(HttpContext context, string operationName, RequestAttributes attrs = RequestAttributes.None)
        {
            this.context = context;
            this.OperationName = operationName;
            this.request = context.Request;
            this.Items = new Dictionary<string, object>();
            this.Response = new NetCoreResponse(this, context.Response);
            this.RequestPreferences = new RequestPreferences(this);
            this.RequestAttributes = attrs;
        }

        public T TryResolve<T>()
        {
            var instance = context.RequestServices.GetService<T>();
            if (instance != null)
                return instance;

            return this.TryResolveInternal<T>();
        }

        public string GetRawBody()
        {
            request.EnableRewind();
            return request.Body.ReadFully().FromUtf8Bytes();
        }

        public object OriginalRequest => request;
        public IResponse Response { get; }
        public string OperationName { get; set; }
        public string Verb => request.Method;
        public RequestAttributes RequestAttributes { get; set; }
        public IRequestPreferences RequestPreferences { get; }
        public object Dto { get; set; }
        public string ContentType => request.ContentType;

        public bool IsLocal
        {
            get
            {
                var conn = request.HttpContext.Connection;
                if (conn.RemoteIpAddress != null)
                {
                    return conn.LocalIpAddress != null
                        ? conn.RemoteIpAddress.Equals(conn.LocalIpAddress)
                        : IPAddress.IsLoopback(conn.RemoteIpAddress);
                }
                return conn.RemoteIpAddress == null && conn.LocalIpAddress == null;
            }
        }

        public string UserAgent => request.Headers["User-Agent"];

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
                if (cookies != null)
                    return cookies;

                cookies = new Dictionary<string, Cookie>();
                foreach (var httpCookie in request.Cookies)
                {
                    Cookie cookie = null;

                    // try-catch needed as malformed cookie names (e.g. '$Version') can be returned
                    // from Cookie.Name, but the Cookie constructor will throw for these names.
                    try
                    {
                        cookie = new Cookie(httpCookie.Key, httpCookie.Value);
                    }
                    catch (Exception ex)
                    {
                        log.Warn("Error trying to create System.Net.Cookie: " + httpCookie.Key, ex);
                    }

                    if (cookie != null)
                        cookies[httpCookie.Key] = cookie;
                }
                return cookies;
            }
        }

        public Dictionary<string, object> Items { get; }

        private INameValueCollection headers;
        public INameValueCollection Headers
        {
            get
            {
                if (headers != null)
                    return headers;

                var nvc = new NameValueCollection();
                foreach (var header in request.Headers)
                {
                    nvc.Add(header.Key, header.Value);
                }
                return headers = new NameValueCollectionWrapper(nvc);
            }
        }

        private INameValueCollection queryString;
        public INameValueCollection QueryString
        {
            get
            {
                if (queryString != null)
                    return queryString;

                var nvc = new NameValueCollection();
                foreach (var query in request.Query)
                {
                    nvc.Add(query.Key, query.Value);
                }
                return queryString = new NameValueCollectionWrapper(nvc);
            }
        }

        private INameValueCollection formData;
        public INameValueCollection FormData
        {
            get
            {
                if (formData != null)
                    return formData;

                var nvc = new NameValueCollection();
                if (request.HasFormContentType)                    
                {
                    foreach (var form in request.Form)
                    {
                        nvc.Add(form.Key, form.Value);
                    }
                }
                return formData = new NameValueCollectionWrapper(nvc);
            }
        }

        public bool UseBufferedStream { get; set; }

        public string RawUrl => UriHelper.GetDisplayUrl(request);

        public string AbsoluteUri => UriHelper.GetDisplayUrl(request);

        public string UserHostAddress => request.HttpContext.Connection.RemoteIpAddress.ToString();

        public string RemoteIp => UserHostAddress;

        public string Authorization => request.Headers[HttpHeaders.Authorization];

        public bool IsSecureConnection => request.IsHttps;

        public string[] AcceptTypes => request.Headers[HttpHeaders.Accept].ToArray();

        public string PathInfo => WebUtility.UrlDecode(request.Path);

        public Stream InputStream => request.Body;

        public long ContentLength => request.ContentLength.GetValueOrDefault();

        private IHttpFile[] files;
        public IHttpFile[] Files
        {
            get
            {
                if (files != null)
                    return files;

                if (!request.HasFormContentType)                    
                    return new IHttpFile[0];

                files = new IHttpFile[request.Form.Files.Count];
                for (var i=0; i< request.Form.Files.Count; i++)
                {
                    var file = request.Form.Files[i];
                    var fileStream = file.OpenReadStream();
                    files[i] = new HttpFile
                    {
                        ContentLength = file.Length,
                        ContentType = file.ContentType,
                        FileName = file.FileName,
                        Name = file.Name,
                        InputStream = fileStream,
                    };
                    context.Response.RegisterForDispose(fileStream);
                }
                return files;
            }
        }

        public Uri UrlReferrer
        {
            get
            {
                var referer = request.Headers[HttpHeaders.Referer];
                return referer.Count > 0 ? new Uri(referer.ToString()) : null;
            }
        }

        public IHttpResponse HttpResponse { get; }

        private string httpMethod;
        public string HttpMethod => httpMethod
            ?? (httpMethod = this.GetParamInRequestHeader(HttpHeaders.XHttpMethodOverride)
            ?? request.Method);

        public string XForwardedFor =>
            string.IsNullOrEmpty(request.Headers[HttpHeaders.XForwardedFor])
                ? null
                : request.Headers[HttpHeaders.XForwardedFor].ToString();

        public int? XForwardedPort =>
            string.IsNullOrEmpty(request.Headers[HttpHeaders.XForwardedPort])
                ? (int?) null
                : int.Parse(request.Headers[HttpHeaders.XForwardedPort]);

        public string XForwardedProtocol =>
            string.IsNullOrEmpty(request.Headers[HttpHeaders.XForwardedProtocol])
                ? null
                : request.Headers[HttpHeaders.XForwardedProtocol].ToString();

        public string XRealIp =>
            string.IsNullOrEmpty(request.Headers[HttpHeaders.XRealIp])
                ? null
                : request.Headers[HttpHeaders.XRealIp].ToString();

        public string Accept =>
            string.IsNullOrEmpty(request.Headers[HttpHeaders.Accept])
                ? null
                : request.Headers[HttpHeaders.Accept].ToString();
    }
}

#endif
