using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ProtoBuf.Grpc;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.IO;
using ServiceStack.Web;

namespace ServiceStack
{
    public class GrpcRequest : IHttpRequest
    {
        public object Dto { get; set; }
        public object OriginalRequest { get; }
        public IResponse Response { get; set; }

        private IResolver resolver;

        public IResolver Resolver
        {
            get => resolver ?? Service.GlobalResolver;
            set => resolver = value;
        }

        public CallContext Context { get; }
        
        public GrpcRequest(CallContext context, object requestDto, string httpMethod)
        {
            this.OriginalRequest = this.Context = context;
            this.Dto = requestDto;
            this.RequestAttributes = RequestAttributes.Grpc | RequestAttributes.ProtoBuf | RequestAttributes.Secure
                | ContentFormat.GetRequestAttribute(httpMethod);
            
            this.Response = new GrpcResponse(this);
            this.Verb = httpMethod;
            this.PathInfo = context.ServerCallContext.Method;
            this.RemoteIp = context.ServerCallContext.Peer;
            ContentType = this.ResponseContentType = MimeTypes.ProtoBuf;
            this.InputStream = Stream.Null;
            this.Headers = new NameValueCollection();
            this.Cookies = new Dictionary<string, Cookie>();
            this.Items = new Dictionary<string, object>();
            this.QueryString = new NameValueCollection();
            this.FormData = new NameValueCollection();
            this.Files = TypeConstants<IHttpFile>.EmptyArray;

            foreach (var header in context.RequestHeaders)
            {
                this.Headers[header.Key] = header.Value;
            }

            if (context.ServerCallContext.UserState != null)
            {
                foreach (var entry in context.ServerCallContext.UserState)
                {
                    if (entry.Key is string key)
                        Items[key] = entry.Value;
                    else if (entry.Key.GetType().IsValueType)
                        Items[entry.Key.ToString()] = entry.Value;
                }
            }
        }

        private string operationName;
        public string OperationName
        {
            get => operationName ?? Dto.GetType().Name;
            set => operationName = value;
        }

        public T TryResolve<T>()
        {
            if (typeof(T) == typeof(IRequest))
                return (T)(object)this;
            if (typeof(T) == typeof(IResponse))
                return (T)this.Response;

            return Resolver.TryResolve<T>();
        }

        public string UserHostAddress { get; set; }

        public string GetHeader(string headerName)
        {
            var headerValue = Headers[headerName];
            return headerValue;
        }

        public Dictionary<string, object> Items { get; set; }

        public string UserAgent { get; private set; }

        public IDictionary<string, Cookie> Cookies { get; set; }

        public string Verb { get; set; }

        public RequestAttributes RequestAttributes { get; set; }

        private IRequestPreferences requestPreferences;

        public IRequestPreferences RequestPreferences =>
            requestPreferences ?? (requestPreferences = new RequestPreferences(this));

        public string ContentType { get; set; }

        public bool IsLocal { get; private set; }

        public string ResponseContentType { get; set; }

        public bool HasExplicitResponseContentType { get; set; }

        public string CompressionType { get; set; }

        public string AbsoluteUri { get; set; }

        public string PathInfo { get; set; }

        public string OriginalPathInfo => PathInfo;

        public IHttpFile[] Files { get; set; }

        public Uri UrlReferrer { get; set; }

        public NameValueCollection Headers { get; set; }

        public NameValueCollection QueryString { get; set; }

        public NameValueCollection FormData { get; set; }

        public bool UseBufferedStream { get; set; }

        public string GetRawBody() => null;

        public string RawUrl { get; set; }

        public string RemoteIp { get; set; }

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

        public Stream InputStream { get; set; }

        public long ContentLength => (GetRawBody() ?? "").Length;

        public GrpcRequest PopulateWith(IRequest request)
        {
            this.Headers = request.Headers;
            this.Cookies = request.Cookies;
            this.Items = request.Items;
            this.UserAgent = request.UserAgent;
            this.RemoteIp = request.RemoteIp;
            this.UserHostAddress = request.UserHostAddress;
            this.IsSecureConnection = request.IsSecureConnection;
            this.AcceptTypes = request.AcceptTypes;
            return this;
        }

        public IVirtualFile GetFile() => HostContext.VirtualFileSources.GetFile(PathInfo);

        public IVirtualDirectory GetDirectory() => HostContext.VirtualFileSources.GetDirectory(PathInfo);

        public bool IsFile { get; set; }

        public bool IsDirectory { get; set; }
        
        public IHttpResponse HttpResponse { get; set; }
        public string HttpMethod { get; set; }
        public string XForwardedFor { get; set; }
        public int? XForwardedPort { get; set; }
        public string XForwardedProtocol { get; set; }
        public string XRealIp { get; set; }
        public string Accept { get; set; }
    }

    public class GrpcResponse : IHttpResponse, IHasHeaders
    {
        private readonly GrpcRequest request;
        public Dictionary<string, string> Headers { get; }

        public GrpcResponse(GrpcRequest request)
        {
            this.request = request;
            this.OriginalResponse = request.Context;
            this.Headers = new Dictionary<string, string>();
            this.Items = new Dictionary<string, object>();
            Cookies = new Cookies(this);
            this.OutputStream = Stream.Null;
        }

        public object OriginalResponse { get; set; }

        public IRequest Request => request;

        public int StatusCode { get; set; } = 200;

        public string StatusDescription { get; set; }

        public string ContentType
        {
            get => request.ResponseContentType;
            set => request.ResponseContentType = value;
        }

        public void AddHeader(string name, string value)
        {
            Headers[name] = value;
        }

        public void RemoveHeader(string name)
        {
            Headers.Remove(name);
        }

        public string GetHeader(string name)
        {
            this.Headers.TryGetValue(name, out var value);
            return value;
        }

        public void Redirect(string url)
        {
        }

        public Stream OutputStream { get; }

        public object Dto { get; set; }

        public void Write(string text) {}

        public bool UseBufferedStream { get; set; }

        public void Close()
        {
            IsClosed = true;
        }

        public Task CloseAsync(CancellationToken token = default(CancellationToken))
        {
            Close();
            return TypeConstants.EmptyTask;
        }

        public void End()
        {
            Close();
        }

        public void Flush()
        {
        }

        public Task FlushAsync(CancellationToken token = default(CancellationToken)) => TypeConstants.EmptyTask;

        public bool IsClosed { get; set; }

        public void SetContentLength(long contentLength)
        {
        }

        public bool KeepAlive { get; set; }

        public bool HasStarted { get; set; }

        public Dictionary<string, object> Items { get; }

        public ICookies Cookies { get; }
        public void SetCookie(Cookie cookie) {}

        public void ClearCookies() {}
    }
}