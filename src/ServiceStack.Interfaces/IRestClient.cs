using System.IO;
using System.Net;

namespace ServiceStack
{
	public interface IRestClient 
	{
        HttpWebResponse Get(IReturnVoid request);
        HttpWebResponse Get(object request);
        TResponse Get<TResponse>(IReturn<TResponse> requestDto);
        TResponse Get<TResponse>(object requestDto);
        TResponse Get<TResponse>(string relativeOrAbsoluteUrl);

        HttpWebResponse Delete(IReturnVoid requestDto);
        HttpWebResponse Delete(object requestDto);
        TResponse Delete<TResponse>(IReturn<TResponse> request);
        TResponse Delete<TResponse>(object request);
        TResponse Delete<TResponse>(string relativeOrAbsoluteUrl);

        HttpWebResponse Post(IReturnVoid requestDto);
        HttpWebResponse Post(object requestDto);
        TResponse Post<TResponse>(IReturn<TResponse> requestDto);
        TResponse Post<TResponse>(object requestDto);
        TResponse Post<TResponse>(string relativeOrAbsoluteUrl, object request);

        HttpWebResponse Put(IReturnVoid requestDto);
        HttpWebResponse Put(object requestDto);
        TResponse Put<TResponse>(IReturn<TResponse> requestDto);
        TResponse Put<TResponse>(object requestDto);
        TResponse Put<TResponse>(string relativeOrAbsoluteUrl, object requestDto);

        HttpWebResponse Patch(IReturnVoid requestDto);
        HttpWebResponse Patch(object requestDto);
        TResponse Patch<TResponse>(IReturn<TResponse> requestDto);
        TResponse Patch<TResponse>(object requestDto);
        TResponse Patch<TResponse>(string relativeOrAbsoluteUrl, object requestDto);

        HttpWebResponse CustomMethod(string httpVerb, IReturnVoid requestDto);
        HttpWebResponse CustomMethod(string httpVerb, object requestDto);
        TResponse CustomMethod<TResponse>(string httpVerb, IReturn<TResponse> requestDto);
        TResponse CustomMethod<TResponse>(string httpVerb, object requestDto);

        HttpWebResponse Head(IReturn requestDto);
        HttpWebResponse Head(object requestDto);
        HttpWebResponse Head(string relativeOrAbsoluteUrl);

        TResponse PostFile<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, string mimeType);
        TResponse PostFileWithRequest<TResponse>(Stream fileToUpload, string fileName, object request, string fieldName = "upload");
        TResponse PostFileWithRequest<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, object request, string fieldName = "upload");
    }
}