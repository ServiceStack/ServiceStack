using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using Funq;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack.Testing
{
    public class MockHttpRequest : IHttpRequest, IHasResolver
    {
        [Obsolete("Use Resolver")]
        public Container Container { get { throw new NotSupportedException("Use Resolver"); } }

        private IResolver resolver;
        public IResolver Resolver
        {
            get { return resolver ?? Service.GlobalResolver; }
            set { resolver = value; }
        }

        public MockHttpRequest()
        {
            this.FormData = PclExportClient.Instance.NewNameValueCollection();
            this.Headers = PclExportClient.Instance.NewNameValueCollection();
            this.QueryString = PclExportClient.Instance.NewNameValueCollection();
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
            this.QueryString = queryString.InWrapper();
            this.FormData = new NameValueCollectionWrapper(formData ?? new NameValueCollection());
        }

        public object OriginalRequest => null;

        public IResponse Response { get; }

        public T TryResolve<T>()
        {
            return this.TryResolveInternal<T>();
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
        public IRequestPreferences RequestPreferences => requestPreferences ?? (requestPreferences = new RequestPreferences(this));

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
            get { return responseContentType ?? this.ContentType ?? MimeTypes.Json; }
            set { responseContentType = value; }
        }

        public bool HasExplicitResponseContentType { get; private set; }

        public INameValueCollection Headers { get; set; }

        public INameValueCollection QueryString { get; set; }

        public INameValueCollection FormData { get; set; }

        public bool UseBufferedStream { get; set; }

        public Dictionary<string, object> Items { get; set; }

        private string rawBody;
        public string GetRawBody()
        {
            if (rawBody != null) return rawBody;
            if (InputStream == null) return null;

            //Keep the stream alive in-case it needs to be read twice (i.e. ContentLength)
            rawBody = new StreamReader(InputStream).ReadToEnd();
            InputStream.Position = 0;
            return rawBody;
        }

        public string RawUrl { get; set; }

        public string AbsoluteUri => "http://localhost" + this.PathInfo;

        public string UserHostAddress { get; set; }

        public string RemoteIp { get; set; }
        public string Authorization { get; set; }
        public string XForwardedFor { get; set; }
        public int? XForwardedPort { get; set; }
        public string XForwardedProtocol { get; set; }
        public string XRealIp { get; set; }
        public string Accept { get; set; }

        public bool IsSecureConnection { get; set; }
        public string[] AcceptTypes { get; set; }
        public string PathInfo { get; set; }
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
    }
}