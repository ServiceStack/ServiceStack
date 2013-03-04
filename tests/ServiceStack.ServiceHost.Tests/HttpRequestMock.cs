using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Common.Utils;

namespace ServiceStack.ServiceHost.Tests
{
	class HttpRequestMock : IHttpRequest
	{
		public object OriginalRequest
		{
			get { throw new NotImplementedException(); }
		}

		public string OperationName
		{
			get { throw new NotImplementedException(); }
		}

		public string ContentType
		{
			get { throw new NotImplementedException(); }
		}

        public bool IsLocal
        {
            get { return true; }
        }

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

		public Dictionary<string, object> Items
		{
			get { throw new NotImplementedException(); }
		}

		public System.Collections.Specialized.NameValueCollection Headers
		{
			get { throw new NotImplementedException(); }
		}

		public System.Collections.Specialized.NameValueCollection QueryString
		{
			get { throw new NotImplementedException(); }
		}

		public System.Collections.Specialized.NameValueCollection FormData
		{
			get { throw new NotImplementedException(); }
		}

	    public bool UseBufferedStream { get; set; }

	    public string GetRawBody()
		{
			throw new NotImplementedException();
		}

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

        public string XForwardedFor
        {
            get { throw new NotImplementedException(); }
        }
        
        public string XRealIp
        {
            get { throw new NotImplementedException(); }
        }

		public bool IsSecureConnection
		{
			get { throw new NotImplementedException(); }
		}

		public string[] AcceptTypes
		{
			get { throw new NotImplementedException(); }
		}

		public string PathInfo
		{
			get { return "index.html"; }
		}

		public System.IO.Stream InputStream
		{
			get { throw new NotImplementedException(); }
		}

		public long ContentLength
		{
			get { throw new NotImplementedException(); }
		}

		public IFile[] Files
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
	}
}
