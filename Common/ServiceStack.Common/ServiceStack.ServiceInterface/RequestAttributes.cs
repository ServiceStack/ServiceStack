using System;
using System.Web;
using ServiceStack.Service;

namespace ServiceStack.ServiceInterface
{
	public class RequestAttributes : IRequestAttributes
	{
		private readonly HttpContext httpContext;

		public RequestAttributes(HttpContext httpContext)
		{
			this.httpContext = httpContext;
		}

		public static HttpWorkerRequest GetWorker(HttpContext context)
		{
			var provider = (IServiceProvider)context;
			var worker = (HttpWorkerRequest)provider.GetService(typeof(HttpWorkerRequest));
			return worker;
		}

		private HttpWorkerRequest httpWorkerRequest;
		public HttpWorkerRequest HttpWorkerRequest
		{
			get
			{
				if (this.httpWorkerRequest == null)
				{
					this.httpWorkerRequest = GetWorker(this.httpContext);
				}
				return this.httpWorkerRequest;
			}
		}

		private string acceptEncoding;
		public string AcceptEncoding
		{
			get
			{
				if (acceptEncoding == null)
				{
					acceptEncoding = HttpWorkerRequest.GetKnownRequestHeader(HttpWorkerRequest.HeaderAcceptEncoding);
					if (acceptEncoding != null) acceptEncoding = acceptEncoding.ToLower();
				}
				return acceptEncoding;
			}
		}

		public bool AcceptsGzip
		{
			get
			{
				return AcceptEncoding.Contains("gzip");
			}
		}

		public bool AcceptsDeflate
		{
			get
			{
				return AcceptEncoding.Contains("deflate");
			}
		}

		//This adds a filter
		//if (encoding.Contains("gzip"))
		//{
		//    //accepts GZip
		//    application.Response.Filter = new GZipStream(stream, CompressionMode.Compress);
		//    application.Response.AppendHeader("Content-Encoding", "gzip");
		//}
		//else if (encoding.Contains("deflate"))
		//{
		//    //accepts deflate
		//    application.Response.Filter = new DeflateStream(stream, CompressionMode.Compress);
		//    application.Response.AppendHeader("Content-Encoding", "deflate");
		//}

	}

}