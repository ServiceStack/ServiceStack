using System;
using System.Collections.Generic;

namespace ServiceStack.Web
{
    public class SerializationContext : IRequestContext
    {
        public SerializationContext(string contentType)
        {
            this.ResponseContentType = this.ContentType = contentType;
        }

        public T Get<T>() where T : class
        {
            return default(T);
        }

        public string GetHeader(string headerName)
        {
            return null;
        }

        public string IpAddress
        {
            get { throw new NotImplementedException(); }
        }

        public IDictionary<string, System.Net.Cookie> Cookies
        {
            get { return new Dictionary<string, System.Net.Cookie>(); }
        }

        public RequestAttributes RequestAttributes
        {
            get { return RequestAttributes.None; }
        }

        public IRequestPreferences RequestPreferences
        {
            get { throw new NotImplementedException(); }
        }

        public string ContentType { get; set; }

        public string ResponseContentType { get; set; }

        public string CompressionType { get; set; }

        public string AbsoluteUri
        {
            get { throw new NotImplementedException(); }
        }

        public string PathInfo
        {
            get { throw new NotImplementedException(); }
        }

        public IHttpFile[] Files
        {
            get { return new IHttpFile[0]; }
        }

        public void Dispose()
        {
        }
    }
}