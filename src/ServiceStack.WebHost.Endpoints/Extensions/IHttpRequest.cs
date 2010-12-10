using System.Collections.Specialized;
using System.IO;

namespace ServiceStack.WebHost.Endpoints.Extensions
{
	public interface IHttpRequest
	{
		string OperationName { get; }

		string HttpMethod { get; }

		NameValueCollection QueryString { get; }

		NameValueCollection FormData { get; }

		string RawBody { get; }

		string RawUrl { get; }

		string UserHostAddress { get; }

		bool IsSecureConnection { get; }

		string[] AcceptTypes { get; }

		string PathInfo { get; }

		Stream InputStream { get; }
	}

}