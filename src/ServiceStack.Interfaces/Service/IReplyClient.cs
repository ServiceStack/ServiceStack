using System.IO;

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

		TResponse PostFile<TResponse>(string relativeOrAbsoluteUrl, FileInfo fileToUpload, string mimeType);

        TResponse PostFile<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, string mimeType);

        TResponse PostFileWithRequest<TResponse>(string relativeOrAbsoluteUrl, FileInfo fileToUpload, object request);

        TResponse PostFileWithRequest<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, object request);
	}
}