using System.Collections.Generic;
using System.IO;

namespace ServiceStack
{
	public interface IReplyClient
	{
		/// <summary>
		/// Sends the specified request.
		/// </summary>
		/// <param name="request">The request.</param>
		/// <returns></returns>
		TResponse Send<TResponse>(object request);
        TResponse Send<TResponse>(IReturn<TResponse> request);
	    void Send(IReturnVoid request);
    
        List<TResponse> SendAll<TResponse>(IEnumerable<IReturn<TResponse>> requests);
    }
}