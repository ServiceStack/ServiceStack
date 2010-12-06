using System.Collections.Specialized;

namespace ServiceStack.WebHost.Endpoints.Extensions
{
	public interface IHttpRequest
	{
		string OperationName { get; }

		string HttpMethod { get; }

		NameValueCollection QueryString { get; }

		NameValueCollection FormData { get; }

		string RawBody { get; }
	}
}