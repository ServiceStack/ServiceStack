using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.Web;

namespace ServiceStack.ServiceHost.Tests
{
    class HttpRequestMock : IHttpRequest
    {
        public object OriginalRequest
        {
            get { throw new NotImplementedException(); }
        }

        public IResponse Response { get; private set; }

        public string OperationName { get; set; }

        public string Verb { get; private set; }
        public RequestAttributes RequestAttributes { get; set; }
        public IRequestPreferences RequestPreferences { get; private set; }
        public object Dto { get; set; }

        public string ContentType
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsLocal
        {
            get { return true; }
        }

        public IHttpResponse HttpResponse { get; private set; }

        public string HttpMethod
        {
            get { throw new NotImplementedException(); }
        }

        public string UserAgent
        {
            get { throw new NotImplementedException(); }
        }

        public IDictionary<string, System.Net.Cookie> Cookies
        {
            get { throw new NotImplementedException(); }
        }

        public string ResponseContentType
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
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

        public string RawUrl
        {
            get { throw new NotImplementedException(); }
        }

        public string AbsoluteUri
        {
            get { throw new NotImplementedException(); }
        }

        public string UserHostAddress
        {
            get { throw new NotImplementedException(); }
        }

        public string RemoteIp
        {
            get { throw new NotImplementedException(); }
        }

        public string Authorization
        {
            get { throw new NotImplementedException(); }
        }

        public string XForwardedFor
        {
            get { throw new NotImplementedException(); }
        }

        public int? XForwardedPort
        {
            get { throw new NotImplementedException(); }
        }

        public string XForwardedProtocol
        {
            get { throw new NotImplementedException(); }
        }

        public string XRealIp
        {
            get { throw new NotImplementedException(); }
        }

        public string Accept
        {
            get { throw new NotImplementedException(); }
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

        public string[] AcceptTypes
        {
            get { throw new NotImplementedException(); }
        }

        public string PathInfo
        {
            get { return "index.html"; }
        }

        public string OriginalPathInfo => PathInfo;

        public System.IO.Stream InputStream
        {
            get { throw new NotImplementedException(); }
        }

        public long ContentLength
        {
            get { throw new NotImplementedException(); }
        }

        public IHttpFile[] Files
        {
            get { throw new NotImplementedException(); }
        }

        public string ApplicationFilePath
        {
            get { return "~".MapAbsolutePath(); }
        }

        public T TryResolve<T>()
        {
            throw new NotImplementedException();
        }

        public Uri UrlReferrer
        {
            get { throw new NotImplementedException(); }
        }
    }
}
