using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using ServiceStack.Configuration;
using ServiceStack.Messaging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host
{
    public class BasicRequest : IRequest
    {
        public object Dto { get; set; }
        public IMessage Message { get; set; }
        public object OriginalRequest { get; private set; }
        public IResponse Response { get; set; }

        private IResolver resolver;
        public IResolver Resolver
        {
            get { return resolver ?? Service.GlobalResolver; }
            set { resolver = value; }
        }

        public BasicRequest(object requestDto, 
            RequestAttributes requestAttributes = RequestAttributes.LocalSubnet | RequestAttributes.MessageQueue)
            : this(MessageFactory.Create(requestDto), requestAttributes) {}

        public BasicRequest(IMessage message = null,
            RequestAttributes requestAttributes = RequestAttributes.LocalSubnet | RequestAttributes.MessageQueue)
        {
            Message = message ?? new Message();
            ContentType = this.ResponseContentType = MimeTypes.Json;
            this.Headers = PclExportClient.Instance.NewNameValueCollection();

            if (Message.Body != null)
            {
                PathInfo = "/json/oneway/" + OperationName;
                RawUrl = AbsoluteUri = "mq://" + PathInfo;
                Headers = new NameValueCollectionWrapper(Message.ToHeaders().ToNameValueCollection());
            }

            this.IsLocal = true;
            Response = new BasicResponse(this);
            this.RequestAttributes = requestAttributes;

            this.Verb = HttpMethods.Post;
            this.Cookies = new Dictionary<string, Cookie>();
            this.Items = new Dictionary<string, object>();
            this.QueryString = PclExportClient.Instance.NewNameValueCollection();
            this.FormData = PclExportClient.Instance.NewNameValueCollection();
            this.Files = new IHttpFile[0];
        }

        private string operationName;
        public string OperationName
        {
            get { return operationName ?? (operationName = Message.Body != null ? Message.Body.GetType().GetOperationName() : null); }
            set { operationName = value; }
        }

        public T TryResolve<T>()
        {
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
        public IRequestPreferences RequestPreferences
        {
            get
            {
                if (requestPreferences == null)
                {
                    requestPreferences = new RequestPreferences(this);
                }
                return requestPreferences;
            }
        }

        public string ContentType { get; set; }

        public bool IsLocal { get; private set; }

        public string ResponseContentType { get; set; }

        public bool HasExplicitResponseContentType { get; set; }
        
        public string CompressionType { get; set; }

        public string AbsoluteUri { get; set; }

        public string PathInfo { get; set; }

        public IHttpFile[] Files { get; set; }
        
        public Uri UrlReferrer { get; set; }

        public INameValueCollection Headers { get; set; }

        public INameValueCollection QueryString { get; set; }

        public INameValueCollection FormData { get; set; }

        public bool UseBufferedStream { get; set; }

        private string body;
        public string GetRawBody()
        {
            return body ?? (body = (Message.Body ?? "").Dump());
        }

        public string RawUrl { get; set; }

        public string RemoteIp { get; set; }

        public bool IsSecureConnection { get; set; }

        public string[] AcceptTypes { get; set; }

        public Stream InputStream { get; set; }

        public long ContentLength
        {
            get { return (GetRawBody() ?? "").Length; }
        }

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
    }
}