using System;
using System.Collections.Specialized;
using System.IO;

namespace ServiceStack.ServiceHost
{
	public interface IHttpRequest
	{
		string OperationName { get; }

		string ContentType { get; }

		string HttpMethod { get; }

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
	}
}