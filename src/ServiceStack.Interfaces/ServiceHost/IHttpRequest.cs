using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;

namespace ServiceStack.ServiceHost
{
	public interface IHttpRequest
	{
		string OperationName { get; }

		string ContentType { get; }

		string HttpMethod { get; }

		IDictionary<string, Cookie> Cookies { get; }

		string ResponseContentType { get; set; }

		/// <summary>
		/// Attach any data to this request that all filters and services can access.
		/// </summary>
		Dictionary<string, object> Items { get; }

		NameValueCollection Headers { get; }

		NameValueCollection QueryString { get; }

		NameValueCollection FormData { get; }

		string GetRawBody();

		string RawUrl { get; }

		string AbsoluteUri { get; }

		string UserHostAddress { get; }

		bool IsSecureConnection { get; }

		string[] AcceptTypes { get; }

		string PathInfo { get; }

		Stream InputStream { get; }

		long ContentLength { get; }

		IFile[] Files { get; }

		string ApplicationFilePath { get; }
	}
}