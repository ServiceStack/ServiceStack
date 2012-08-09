using System.IO;

namespace ServiceStack.Service
{
	public interface IRestClient 
	{
		TResponse Get<TResponse>(string relativeOrAbsoluteUrl);
		TResponse Delete<TResponse>(string relativeOrAbsoluteUrl);

		TResponse Post<TResponse>(string relativeOrAbsoluteUrl, object request);
		TResponse Put<TResponse>(string relativeOrAbsoluteUrl, object request);

		TResponse Patch<TResponse>(string relativeOrAbsoluteUrl, object request);

		TResponse PostFile<TResponse>(string relativeOrAbsoluteUrl, FileInfo fileToUpload, string mimeType);
	}
}