using System.IO;
using ServiceStack.ServiceHost;

namespace ServiceStack.Service
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

#if !NETFX_CORE
		TResponse PostFile<TResponse>(string relativeOrAbsoluteUrl, FileInfo fileToUpload, string mimeType);
#endif

        TResponse PostFile<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, string mimeType);

#if !NETFX_CORE
        TResponse PostFileWithRequest<TResponse>(string relativeOrAbsoluteUrl, FileInfo fileToUpload, object request);
#endif

        TResponse PostFileWithRequest<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, object request);
	}
}