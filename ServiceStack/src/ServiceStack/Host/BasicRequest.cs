using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using ServiceStack.Configuration;
using ServiceStack.IO;
using ServiceStack.Messaging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host
{
    public class BasicRequest : IRequest, IHasResolver, IHasVirtualFiles
#if NETCORE
    , IHasServiceScope
#endif    
    {
        public virtual object Dto { get; set; }
        public IMessage Message { get; set; }
        public object OriginalRequest { get; protected set; }
        public IResponse Response { get; set; }
        
#if NETCORE
        public Microsoft.Extensions.DependencyInjection.IServiceScope ServiceScope { get; set; }
#endif
        
        private IResolver resolver;
        public IResolver Resolver
        {
            get => resolver ?? Service.GlobalResolver;
            set => resolver = value;
        }

        public BasicRequest(object requestDto, 
            RequestAttributes requestAttributes = RequestAttributes.LocalSubnet | RequestAttributes.MessageQueue)
            : this(MessageFactory.Create(requestDto), requestAttributes) {}

        public BasicRequest(IMessage message = null,
            RequestAttributes requestAttributes = RequestAttributes.LocalSubnet | RequestAttributes.MessageQueue)
        {
            Message = message ?? new Message();
            Dto = Message.Body;
            ContentType = this.ResponseContentType = MimeTypes.Json;
            this.Headers = new NameValueCollection();

            if (Dto != null)
            {
                PathInfo = "/json/oneway/" + OperationName;
                RawUrl = AbsoluteUri = "mq://" + PathInfo;
                Headers = Message.ToHeaders().ToNameValueCollection();
            }

            this.IsLocal = true;
            Response = new BasicResponse(this);
            this.RequestAttributes = requestAttributes;

            this.Verb = HttpMethods.Post;
            this.Cookies = new Dictionary<string, Cookie>();
            this.Items = new Dictionary<string, object>();
            this.QueryString = new NameValueCollection();
            this.FormData = new NameValueCollection();
            this.Files = TypeConstants<IHttpFile>.EmptyArray;
            this.RemoteIp = IPAddress.IPv6Loopback.ToString();
        }

        private string operationName;
        public string OperationName
        {
            get => operationName ??= Dto?.GetType().GetOperationName();
            set => operationName = value;
        }

        public T TryResolve<T>()
        {
#if NETCORE
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

        public string UserHostAddress { get; set; }

        public string GetHeader(string headerName)
        {
            var headerValue = Headers[headerName];
            return headerValue;
        }

        public Dictionary<string, object> Items { get; set; }

        public string UserAgent { get; protected set; }

        public IDictionary<string, Cookie> Cookies { get; set; }

        public string Verb { get; set; }

        public RequestAttributes RequestAttributes { get; set; }

        private IRequestPreferences requestPreferences;
        public IRequestPreferences RequestPreferences => requestPreferences ??= new RequestPreferences(this);

        public string ContentType { get; set; }

        public bool IsLocal { get; protected set; }

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

        private string body;
        public string GetRawBody() => body ??= (Message.Body ?? "").Dump();

        public Task<string> GetRawBodyAsync() => Task.FromResult(GetRawBody());

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

        public Stream InputStream { get; set; } = Stream.Null;

        public long ContentLength => (GetRawBody() ?? "").Length;

        public BasicRequest PopulateWith(IRequest request)
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
    }
}