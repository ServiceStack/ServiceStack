using System.IO;
using System.Net;

namespace ServiceStack
{
	public interface IRestClient 
	{
        void Get(IReturnVoid request);
        void Get(object request);
        TResponse Get<TResponse>(IReturn<TResponse> requestDto);
        TResponse Get<TResponse>(object requestDto);
        TResponse Get<TResponse>(string relativeOrAbsoluteUrl);

        void Delete(IReturnVoid requestDto);
        void Delete(object requestDto);
        TResponse Delete<TResponse>(IReturn<TResponse> request);
        TResponse Delete<TResponse>(object request);
        TResponse Delete<TResponse>(string relativeOrAbsoluteUrl);

        void Post(IReturnVoid requestDto);
        void Post(object requestDto);
        TResponse Post<TResponse>(IReturn<TResponse> requestDto);
        TResponse Post<TResponse>(object requestDto);
        TResponse Post<TResponse>(string relativeOrAbsoluteUrl, object request);

        void Put(IReturnVoid requestDto);
        void Put(object requestDto);
        TResponse Put<TResponse>(IReturn<TResponse> requestDto);
        TResponse Put<TResponse>(object requestDto);
        TResponse Put<TResponse>(string relativeOrAbsoluteUrl, object requestDto);

        void Patch(IReturnVoid requestDto);
        void Patch(object requestDto);
        TResponse Patch<TResponse>(IReturn<TResponse> requestDto);
        TResponse Patch<TResponse>(object requestDto);
        TResponse Patch<TResponse>(string relativeOrAbsoluteUrl, object requestDto);

        void CustomMethod(string httpVerb, IReturnVoid requestDto);
        void CustomMethod(string httpVerb, object requestDto);
        TResponse CustomMethod<TResponse>(string httpVerb, IReturn<TResponse> requestDto);
        TResponse CustomMethod<TResponse>(string httpVerb, object requestDto);

        HttpWebResponse Head(IReturn requestDto);
        HttpWebResponse Head(object requestDto);
        HttpWebResponse Head(string relativeOrAbsoluteUrl);

#if !(NETFX_CORE || PCL)
        TResponse PostFile<TResponse>(string relativeOrAbsoluteUrl, FileInfo fileToUpload, string mimeType);
#endif
        TResponse PostFile<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, string mimeType);

#if !(NETFX_CORE || PCL)
        TResponse PostFileWithRequest<TResponse>(string relativeOrAbsoluteUrl, FileInfo fileToUpload, object request);
#endif
        TResponse PostFileWithRequest<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, object request);
    }
}