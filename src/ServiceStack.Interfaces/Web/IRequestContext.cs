using System;
using System.Collections.Generic;
using System.Net;

namespace ServiceStack.Web
{
	public interface IRequestContext : IDisposable
	{
		T Get<T>() where T : class;
		
		string IpAddress { get; }

		string GetHeader(string headerName);

		IDictionary<string, Cookie> Cookies { get; }

		RequestAttributes RequestAttributes { get; }
		
		IRequestPreferences RequestPreferences { get; }

		string ContentType { get; }

		string ResponseContentType { get; }

		string CompressionType { get; }

		string AbsoluteUri { get; }

		string PathInfo { get; }

		IHttpFile[] Files { get; }
	}
}