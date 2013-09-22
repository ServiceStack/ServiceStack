using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Net;
using ServiceStack.Configuration;
using ServiceStack.Messaging;
using ServiceStack.Server;
using ServiceStack.Text;

namespace ServiceStack.ServiceHost
{
    public class MqRequestContext : IRequestContext
    {
        public IResolver Resolver { get; set; }
        public IMessage Message { get; set; }
        public MqRequest Request { get; set; }
        public MqResponse Response { get; set; }

        public MqRequestContext()
            : this(null, new Message()) {}

        public MqRequestContext(IResolver resolver, IMessage message)
        {
            this.Resolver = resolver;
            this.Message = message;
            this.ContentType = this.ResponseContentType = MimeTypes.Json;
            if (message.Body != null)
                this.PathInfo = "/json/oneway/" + OperationName;
            
            this.Request = new MqRequest(this);
            this.Response = new MqResponse(this);
        }

        private string operationName;
        public string OperationName
        {
            get { return operationName ?? (operationName = Message.Body != null ? Message.Body.GetType().Name : null); }
            set { operationName = value; }
        }

        public T Get<T>() where T : class
        {
            if (typeof(T) == typeof(IHttpRequest))
                return (T)(object)Request;

            if (typeof(T) == typeof(IHttpResponse))
                return (T)(object)Response;

            return Resolver.TryResolve<T>();
        }

        public string IpAddress { get; set; }

        public string GetHeader(string headerName)
        {
            string headerValue;
            Headers.TryGetValue(headerName, out headerValue);
            return headerValue;
        }

        private Dictionary<string, string> headers;
        public Dictionary<string, string> Headers
        {
            get
            {
                if (headers != null)
                {
                    headers = Message.ToHeaders();
                }
                return headers;
            }
        }

        public IDictionary<string, Cookie> Cookies
        {
            get { return new Dictionary<string, Cookie>(); }
        }

        public EndpointAttributes EndpointAttributes
        {
            get { return EndpointAttributes.LocalSubnet | EndpointAttributes.MessageQueue; }
        }

        public IRequestAttributes RequestAttributes { get; set; }

        public string ContentType { get; set; }

        public string ResponseContentType { get; set; }

        public string CompressionType { get; set; }

        public string AbsoluteUri { get; set; }

        public string PathInfo { get; set; }

        public IFile[] Files { get; set; }

        public void Dispose()
        {
        }
    }


    public static class MqExtensions
    {
        public static Dictionary<string,string> ToHeaders(this IMessage message)
        {
            var map = new Dictionary<string, string>
            {
                {"CreatedDate",message.CreatedDate.ToLongDateString()},
                {"Priority",message.Priority.ToString(CultureInfo.InvariantCulture)},
                {"RetryAttempts",message.RetryAttempts.ToString(CultureInfo.InvariantCulture)},
                {"ReplyId",message.ReplyId.HasValue ? message.ReplyId.Value.ToString() : null},
                {"ReplyTo",message.ReplyTo},
                {"Options",message.Options.ToString(CultureInfo.InvariantCulture)},
                {"Error",message.Error.Dump()},
            };
            return map;
        }
    }
}