using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using ServiceStack.Common.Web;
using ServiceStack.ServiceModel;
using ServiceStack.Text;

namespace ServiceStack.ServiceHost
{
    public class MqRequest : IHttpRequest
    {
        private readonly MqRequestContext requestContext;

        public MqRequest(MqRequestContext requestContext)
        {
            this.requestContext = requestContext;
        }

        public T TryResolve<T>()
        {
            return requestContext.Resolver.TryResolve<T>();
        }

        public object OriginalRequest
        {
            get { return requestContext.Message; }
        }

        public string OperationName
        {
            get { return requestContext.OperationName; }
        }

        public string ContentType
        {
            get { return requestContext.ContentType; }
        }

        public string HttpMethod
        {
            get { return HttpMethods.Post; }
        }

        public string UserAgent
        {
            get { return "MQ"; }
        }

        public bool IsLocal
        {
            get { return true; }
        }

        public IDictionary<string, Cookie> Cookies
        {
            get { return requestContext.Cookies; }
        }

        public string ResponseContentType
        {
            get { return requestContext.ResponseContentType; }
            set { requestContext.ResponseContentType = value; }
        }

        private Dictionary<string, object> items;
        public Dictionary<string, object> Items
        {
            get { return items ?? (items = new Dictionary<string, object>()); }
        }

        private NameValueCollection headers;
        public NameValueCollection Headers
        {
            get { return headers ?? (headers = requestContext.Headers.ToNameValueCollection()); }
        }

        public NameValueCollection QueryString
        {
            get { return new NameValueCollection(); }
        }

        public NameValueCollection FormData
        {
            get { return new NameValueCollection(); }
        }

        public bool UseBufferedStream { get; set; }

        private string body;
        public string GetRawBody()
        {
            return body ?? (body = requestContext.Message.Body.Dump());
        }

        public string RawUrl
        {
            get { return "mq://" + requestContext.PathInfo; }
        }

        public string AbsoluteUri
        {
            get { return "mq://" + requestContext.PathInfo; }
        }

        public string UserHostAddress
        {
            get { return null; }
        }

        public string RemoteIp
        {
            get { return null; }
        }

        public string XForwardedFor
        {
            get { return null; }
        }

        public string XRealIp 
        {
            get { return null; }
        }


        public bool IsSecureConnection
        {
            get { return false; }
        }

        public string[] AcceptTypes
        {
            get { return new string[0]; }
        }

        public string PathInfo
        {
            get { return requestContext.PathInfo; }
        }

        public Stream InputStream
        {
            get { return null; }
        }

        public long ContentLength
        {
            get { return GetRawBody().Length; }
        }

        public IFile[] Files
        {
            get { return null; }
        }

        public string ApplicationFilePath
        {
            get { return null; }
        }
    }
}