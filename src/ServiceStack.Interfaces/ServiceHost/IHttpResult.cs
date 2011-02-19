using System.Collections.Generic;
using System.Net;

namespace ServiceStack.ServiceHost
{
	public interface IHttpResult : IHasOptions
	{
		string ContentType { get; set; }

		Dictionary<string, string> Headers { get; }

		HttpStatusCode StatusCode { get; set; }

		object Response { get; set; }

		/// <summary>
		/// if not provided, get's injected by ServiceStack
		/// </summary>
		IContentTypeWriter ResponseFilter { get; set; }

		/// <summary>
		/// Holds the request call context
		/// </summary>
		IRequestContext RequestContext { get; set; }
	}
}