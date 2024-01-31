using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.Web;

namespace ServiceStack.ServiceHost.Tests;

class HttpRequestMock : IHttpRequest
{
    public object OriginalRequest => throw new NotImplementedException();

    public IResponse Response { get; private set; }

    public string OperationName { get; set; }

    public string Verb { get; private set; }
    public RequestAttributes RequestAttributes { get; set; }
    public IRequestPreferences RequestPreferences { get; private set; }
    public object Dto { get; set; }

    public string ContentType => throw new NotImplementedException();

    public bool IsLocal => true;

    public IHttpResponse HttpResponse { get; private set; }

    public string HttpMethod => throw new NotImplementedException();

    public string UserAgent => throw new NotImplementedException();

    public IDictionary<string, System.Net.Cookie> Cookies => throw new NotImplementedException();

    public string ResponseContentType
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public bool HasExplicitResponseContentType { get; private set; }
    public Dictionary<string, object> Items => throw new NotImplementedException();
    public NameValueCollection Headers => throw new NotImplementedException();
    public NameValueCollection QueryString => throw new NotImplementedException();
    public NameValueCollection FormData => throw new NotImplementedException();

    public bool UseBufferedStream { get; set; }

    public string GetRawBody()
    {
        throw new NotImplementedException();
    }

    public Task<string> GetRawBodyAsync() => throw new NotImplementedException(); 

    public string RawUrl => throw new NotImplementedException();
    public string AbsoluteUri => throw new NotImplementedException();
    public string UserHostAddress => throw new NotImplementedException();
    public string RemoteIp => throw new NotImplementedException();
    public string Authorization => throw new NotImplementedException();
    public string XForwardedFor => throw new NotImplementedException();
    public int? XForwardedPort => throw new NotImplementedException();
    public string XForwardedProtocol => throw new NotImplementedException();
    public string XRealIp => throw new NotImplementedException();
    public string Accept => throw new NotImplementedException();

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

    public string[] AcceptTypes => throw new NotImplementedException();

    public string PathInfo => "index.html";

    public string OriginalPathInfo => PathInfo;

    public System.IO.Stream InputStream => throw new NotImplementedException();
    public long ContentLength => throw new NotImplementedException();
    public IHttpFile[] Files => throw new NotImplementedException();
    public string ApplicationFilePath => "~".MapAbsolutePath();

    public T TryResolve<T>()
    {
        throw new NotImplementedException();
    }
    
    public object GetService(Type serviceType)
    {
        throw new NotImplementedException();
    }
    
    public Uri UrlReferrer => throw new NotImplementedException();
}