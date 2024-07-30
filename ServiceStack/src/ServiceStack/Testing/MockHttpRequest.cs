using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.IO;
using ServiceStack.Web;

namespace ServiceStack.Testing;

public class MockHttpRequest : IHttpRequest, IHasResolver, IHasVirtualFiles
#if !NETFRAMEWORK
    , IHasServiceScope
#endif    
{
    private IResolver resolver;
    public IResolver Resolver
    {
        get => resolver ?? Service.GlobalResolver;
        set => resolver = value;
    }
    
#if !NETFRAMEWORK
    public Microsoft.Extensions.DependencyInjection.IServiceScope ServiceScope { get; set; }
#endif

    public MockHttpRequest()
    {
        this.FormData = new NameValueCollection();
        this.Headers = new NameValueCollection();
        this.QueryString = new NameValueCollection();
        this.Cookies = new Dictionary<string, Cookie>();
        this.Items = new Dictionary<string, object>();
        this.Response = new MockHttpResponse(this);
    }

    public MockHttpRequest(string operationName, string httpMethod,
        string contentType, string pathInfo,
        NameValueCollection queryString, Stream inputStream, NameValueCollection formData)
        : this()
    {
        this.OperationName = operationName;
        this.HttpMethod = httpMethod;
        this.ContentType = contentType;
        this.ResponseContentType = contentType;
        this.PathInfo = pathInfo;
        this.InputStream = inputStream;
        this.QueryString = queryString;
        this.FormData = formData;
    }

    public object OriginalRequest => null;

    public IResponse Response { get; }

    public T TryResolve<T>()
    {
#if !NETFRAMEWORK
        if (ServiceScope != null)
        {
            var instance = ServiceScope.ServiceProvider.GetService(typeof(T));
            if (instance != null)
                return (T)instance;
        }
#endif

        return this.TryResolveInternal<T>();
    }

    public object GetService(Type serviceType)
    {
        var mi = typeof(BasicRequest).GetMethod(nameof(TryResolve));
        var genericMi = mi.MakeGenericMethod(serviceType);
        return genericMi.Invoke(this, TypeConstants.EmptyObjectArray);
    }

    public AuthUserSession RemoveSession()
    {
        this.RemoveSession();
        return this.GetSession() as AuthUserSession;
    }

    public AuthUserSession ReloadSession()
    {
        return this.GetSession() as AuthUserSession;
    }

    public string OperationName { get; set; }
    public RequestAttributes RequestAttributes { get; set; }

    private IRequestPreferences requestPreferences;
    public IRequestPreferences RequestPreferences => requestPreferences ??= new RequestPreferences(this);

    public object Dto { get; set; }
    public string ContentType { get; set; }
    public IHttpResponse HttpResponse { get; private set; }
    public string UserAgent { get; set; }
    public bool IsLocal { get; set; }
    
    public string HttpMethod { get; set; }
    public string Verb => HttpMethod;

    public IDictionary<string, Cookie> Cookies { get; set; }

    private string responseContentType;
    public string ResponseContentType
    {
        get => responseContentType ?? this.ContentType ?? MimeTypes.Json;
        set => responseContentType = value;
    }

    public bool HasExplicitResponseContentType { get; private set; }

    public NameValueCollection Headers { get; set; }

    public NameValueCollection QueryString { get; set; }

    public NameValueCollection FormData { get; set; }

    public bool UseBufferedStream { get; set; }

    public Dictionary<string, object> Items { get; set; }

    private string rawBody;
    public string GetRawBody()
    {
        if (rawBody != null) return rawBody;
        if (InputStream == null) return null;

        //Keep the stream alive in-case it needs to be read twice (i.e. ContentLength)
        rawBody = InputStream.ReadToEnd();
        return rawBody;
    }

    public Task<string> GetRawBodyAsync() => Task.FromResult(GetRawBody());

    public string RawUrl { get; set; }

    public string AbsoluteUri => "http://localhost" + this.PathInfo;

    public string UserHostAddress { get; set; }

    public string XForwardedFor
    {
        get => string.IsNullOrEmpty(Headers[HttpHeaders.XForwardedFor]) ? null : Headers[HttpHeaders.XForwardedFor];
        set => Headers[HttpHeaders.XForwardedFor] = value;
    }

    public int? XForwardedPort
    {
        get => string.IsNullOrEmpty(Headers[HttpHeaders.XForwardedPort])
            ? (int?) null
            : int.Parse(Headers[HttpHeaders.XForwardedPort]);
        set => Headers[HttpHeaders.XForwardedPort] = value?.ToString();
    }

    public string XForwardedProtocol
    {
        get => string.IsNullOrEmpty(Headers[HttpHeaders.XForwardedProtocol])
            ? null
            : Headers[HttpHeaders.XForwardedProtocol];
        set => Headers[HttpHeaders.XForwardedProtocol] = value;
    }

    public string XRealIp
    {
        get => string.IsNullOrEmpty(Headers[HttpHeaders.XRealIp]) ? null : Headers[HttpHeaders.XRealIp];
        set => Headers[HttpHeaders.XRealIp] = value;
    }

    public string Accept
    {
        get => string.IsNullOrEmpty(Headers[HttpHeaders.Accept]) ? null : Headers[HttpHeaders.Accept];
        set => Headers[HttpHeaders.Accept] = value;
    }

    private string remoteIp;
    public string RemoteIp => remoteIp ??= XForwardedFor ?? (XRealIp ?? UserHostAddress);

    public string Authorization
    {
        get => string.IsNullOrEmpty(Headers[HttpHeaders.Authorization])
            ? null
            : Headers[HttpHeaders.Authorization];
        set => Headers[HttpHeaders.Authorization] = value;
    }

    public bool IsSecureConnection
    {
        get => (RequestAttributes & RequestAttributes.Secure) == RequestAttributes.Secure;
        set
        {
            if (value)
                RequestAttributes |= RequestAttributes.Secure;
            else
                RequestAttributes &= ~RequestAttributes.Secure;
        }
    }
    
    public string[] AcceptTypes { get; set; }
    public string PathInfo { get; set; }
    public string OriginalPathInfo { get; }
    public Stream InputStream { get; set; }

    public long ContentLength
    {
        get
        {
            var body = GetRawBody();
            return body?.Length ?? 0;
        }
    }

    public IHttpFile[] Files { get; set; }

    public string ApplicationFilePath { get; set; }

    public void AddSessionCookies()
    {
        var permSessionId = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        this.Cookies[SessionFeature.PermanentSessionId] = new Cookie(SessionFeature.PermanentSessionId, permSessionId);
        var sessionId = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        this.Cookies[SessionFeature.SessionId] = new Cookie(SessionFeature.SessionId, sessionId);
    }

    public Uri UrlReferrer => null;
    
    public IVirtualFile GetFile() => HostContext.VirtualFileSources.GetFile(PathInfo);

    public IVirtualDirectory GetDirectory() => HostContext.VirtualFileSources.GetDirectory(PathInfo);

    private bool? isDirectory;
    public bool IsDirectory => isDirectory ?? (bool)(isDirectory = HostContext.VirtualFiles.DirectoryExists(PathInfo));

    private bool? isFile;
    public bool IsFile => isFile ?? (bool)(isFile = HostContext.VirtualFiles.FileExists(PathInfo));
}
