

using System.Threading.Tasks;
#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Collections.Specialized;

using ServiceStack.Web;
using ServiceStack.Logging;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Configuration;
using ServiceStack.IO;
using ServiceStack.Model;
using ServiceStack.NetCore;

namespace ServiceStack.Host.NetCore;

public class NetCoreRequest : IHttpRequest, IHasResolver, IHasVirtualFiles, IServiceProvider, IHasBufferedStream, IHasStringId, IHasTraceId
{
    public static ILog log = LogManager.GetLogger(typeof(NetCoreRequest));

    private IResolver resolver;
    public IResolver Resolver
    {
        get => resolver ?? Service.GlobalResolver;
        set => resolver = value;
    }

    private readonly HttpContext context;
    private readonly HttpRequest request;

    public NetCoreRequest(HttpContext context, string operationName, RequestAttributes attrs = RequestAttributes.None, string pathInfo = null)
    {
        this.context = context;
        this.OperationName = operationName;
        this.request = context.Request;
        this.Items = new Dictionary<string, object>();
        this.RequestAttributes = attrs;

        //Kestrel does not decode '+' into space
        this.PathInfo = this.OriginalPathInfo = (pathInfo ?? request.Path.Value)?.Replace("+", " ").Replace("%2f", "/");  
        this.PathInfo = HostContext.AppHost.ResolvePathInfo(this, PathInfo);
    }

    public T TryResolve<T>()
    {
        var instance = context.RequestServices.GetService<T>();
        if (instance is IRequiresRequest requiresRequest)
            requiresRequest.Request ??= this;
        return instance ?? this.TryResolveInternal<T>();
    }

    public object GetService(Type serviceType) => context.RequestServices.GetService(serviceType);

    public HttpContext HttpContext => context;
    public HttpRequest HttpRequest => request;

    public object OriginalRequest => request;

    private IResponse response;
    public IResponse Response => response ??= new NetCoreResponse(this, context.Response);

    public string OperationName { get; set; }
    public string Verb => HttpMethod;
    public RequestAttributes RequestAttributes { get; set; }

    private IRequestPreferences requestPreferences;
    public IRequestPreferences RequestPreferences => requestPreferences ??= new RequestPreferences(this);

    public object Dto { get; set; }
    public string ContentType => request.ContentType;

    public string TraceId => request.HttpContext.TraceIdentifier;

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
        get => responseContentType ??= this.GetResponseContentType();
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
                    var name = httpCookie.Key.StartsWith("$")
                        ? httpCookie.Key.Substring(1)
                        : httpCookie.Key;

                    cookie = new Cookie(name, httpCookie.Value);
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

    private NameValueCollection headers;
    public NameValueCollection Headers => headers ??= new NetCoreHeadersCollection(request.Headers);

    private NameValueCollection queryString;
    public NameValueCollection QueryString => queryString ??= new NetCoreQueryStringCollection(request.Query);

    private NameValueCollection formData;
    public NameValueCollection FormData
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
            return formData = nvc;
        }
    }

    public string RawUrl => request.Path.Value + request.QueryString;

    public string AbsoluteUri => request.GetDisplayUrl();

    public string UserHostAddress => request.HttpContext.Connection.RemoteIpAddress?.ToString();

    public string Authorization => request.Headers[HttpHeaders.Authorization];

    public bool IsSecureConnection => request.IsHttps 
        || XForwardedProtocol == "https" 
        || (RequestAttributes & RequestAttributes.Secure) == RequestAttributes.Secure;

    public string[] AcceptTypes => request.Headers[HttpHeaders.Accept].ToArray();

    public string PathInfo { get; }

    public string OriginalPathInfo { get; }

    public MemoryStream BufferedStream { get; set; }
    public Stream InputStream => this.GetInputStream(BufferedStream ?? request.Body);

    public bool UseBufferedStream
    {
        get => BufferedStream != null;
        set => BufferedStream = value
            ? BufferedStream ?? request.AllowSyncIO().Body.CreateBufferedStream()
            : null;
    }

    public string GetRawBody()
    {
        if (BufferedStream != null)
        {
            request.EnableBuffering();
            return BufferedStream.ReadBufferedStreamToEnd(this);
        }
        return request.AllowSyncIO().Body.ReadToEnd();
    }

    public Task<string> GetRawBodyAsync()
    {
        if (BufferedStream != null)
        {
            request.EnableBuffering();
            return Task.FromResult(BufferedStream.ReadBufferedStreamToEnd(this));
        }

        return request.Body.ReadToEndAsync();
    }

    public long ContentLength => request.ContentLength.GetValueOrDefault();

    private IHttpFile[] files;
    public IHttpFile[] Files
    {
        get
        {
            if (files?.Length > 0 && files[0] != null) // During Debugging VS.NET initializes field to T[null]
                return files;

            if (!request.HasFormContentType)                    
                return Array.Empty<IHttpFile>();

            var filesCount = request.Form.Files.Count;
            files = new IHttpFile[filesCount];
            for (var i=0; i<filesCount; i++)
            {
                var uploadedFile = request.Form.Files[i];
                var fileStream = uploadedFile.OpenReadStream();
                files[i] = new HttpFile
                {
                    ContentLength = uploadedFile.Length,
                    ContentType = uploadedFile.ContentType,
                    FileName = uploadedFile.FileName,
                    Name = uploadedFile.Name,
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
    public string HttpMethod => httpMethod ??= this.GetParamInRequestHeader(HttpHeaders.XHttpMethodOverride)
                                               ?? request.Method;

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

    private string remoteIp;
    public string RemoteIp => remoteIp ??= XForwardedFor ?? (XRealIp ?? UserHostAddress);

    
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

    public string Id => System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier;
}

#endif
