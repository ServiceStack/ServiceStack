using System.Collections.Generic;
using System.Net;

namespace ServiceStack.ServiceHost
{
	public interface IHttpResult : IHasOptions
	{
        /// <summary>
        /// The HTTP Response Status
        /// </summary>
        int Status { get; set; }

		/// <summary>
		/// The HTTP Response Status Code
		/// </summary>
		HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// The HTTP Status Description
        /// </summary>
        string StatusDescription { get; set; }

		/// <summary>
		/// The HTTP Response ContentType
		/// </summary>
		string ContentType { get; set; }

		/// <summary>
		/// Additional HTTP Headers
		/// </summary>
		Dictionary<string, string> Headers { get; }

		/// <summary>
		/// Response DTO
		/// </summary>
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