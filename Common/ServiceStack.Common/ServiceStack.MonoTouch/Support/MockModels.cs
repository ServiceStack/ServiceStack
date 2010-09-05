using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;

namespace System.Web
{
	public class HttpContext : IServiceProvider
	{
		public HttpRequest Request { get; set; }
		public HttpResponse Response { get; set; }

		public static HttpContext Current { get; set; }
		
		public object GetService(Type serviceType)
		{
			return null;
		}
	}

	public class HttpRequest
	{
		public HttpRequest()
		{
			this.QueryString = new NameValueCollection();
			this.Form = new NameValueCollection();
		}

		public string HttpMethod { get; set; }
		public string RequestType { get; set; }
		public bool IsSecureConnection { get; set; }
		public string UserHostAddress { get; set; }
		public NameValueCollection QueryString { get; private set; }
		public NameValueCollection Form { get; private set; }
		public Stream InputStream { get; private set; }
	}

	public class HttpResponse
	{
		public void AddHeader(string headerName, string headerValue)
		{
		}
	}	

	public class HttpWorkerRequest
	{
		public string HeaderAcceptEncoding { get; set; }

		public string GetKnownRequestHeader(string headerName)
		{
			return null;
		}
	}

	public interface IHttpHandler
	{
		void ProcessRequest(HttpContext context);
		bool IsReusable { get; }
	}
}

namespace System.ServiceModel
{
	public class OperationContext
	{
		public Dictionary<string, object> IncomingMessageProperties { get; set; }
	}
}

namespace System.ServiceModel.Channels
{
	public class RemoteEndpointMessageProperty
	{
		public static string Name { get; set; }

		public string Address { get; set; }
	}
}