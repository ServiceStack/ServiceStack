using System.IO;
using System.Net;
using ServiceStack.ServiceHost;

namespace ServiceStack.Service
{
	public interface IRestClient 
	{
	    TResponse Get<TResponse>(IReturn<TResponse> request);
        void Get(IReturnVoid request);
        TResponse Get<TResponse>(string relativeOrAbsoluteUrl);

	    TResponse Delete<TResponse>(IReturn<TResponse> request);
        void Delete(IReturnVoid request);
        TResponse Delete<TResponse>(string relativeOrAbsoluteUrl);

	    TResponse Post<TResponse>(IReturn<TResponse> request);
        void Post(IReturnVoid request);
        TResponse Post<TResponse>(string relativeOrAbsoluteUrl, object request);

	    TResponse Put<TResponse>(IReturn<TResponse> request);
	    void Put(IReturnVoid request);
        TResponse Put<TResponse>(string relativeOrAbsoluteUrl, object request);

	    TResponse Patch<TResponse>(IReturn<TResponse> request);
	    void Patch(IReturnVoid request);
		TResponse Patch<TResponse>(string relativeOrAbsoluteUrl, object request);

#if !NETFX_CORE
		TResponse PostFile<TResponse>(string relativeOrAbsoluteUrl, FileInfo fileToUpload, string mimeType);
#endif

	    void CustomMethod(string httpVerb, IReturnVoid request);
	    TResponse CustomMethod<TResponse>(string httpVerb, IReturn<TResponse> request);

        HttpWebResponse Head(IReturn request);
        HttpWebResponse Head(string relativeOrAbsoluteUrl);
	}
}