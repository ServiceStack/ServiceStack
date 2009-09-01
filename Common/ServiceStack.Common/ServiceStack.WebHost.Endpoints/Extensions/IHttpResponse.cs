using System;
using System.Collections.Specialized;
using System.IO;

namespace ServiceStack.WebHost.Endpoints.Extensions
{
	public interface IHttpResponse
	{
		string ContentType { get; set; }

		NameValueCollection Headers { get; }

		Stream OutputStream { get; }

		void Write(string text);
	}
}