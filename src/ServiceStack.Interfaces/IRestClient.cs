using System;
using System.Collections.Generic;
using System.IO;

namespace ServiceStack
{
    public interface IRestClient
    {
        void AddHeader(string name, string value);

        void ClearCookies();
        Dictionary<string, string> GetCookieValues();
        void SetCookie(string name, string value, TimeSpan? expiresIn = null);

        void Get(IReturnVoid request);
        TResponse Get<TResponse>(IReturn<TResponse> requestDto);
        TResponse Get<TResponse>(object requestDto);
        TResponse Get<TResponse>(string relativeOrAbsoluteUrl);
        IEnumerable<TResponse> GetLazy<TResponse>(IReturn<QueryResponse<TResponse>> queryDto);

        void Delete(IReturnVoid requestDto);
        TResponse Delete<TResponse>(IReturn<TResponse> request);
        TResponse Delete<TResponse>(object request);
        TResponse Delete<TResponse>(string relativeOrAbsoluteUrl);

        void Post(IReturnVoid requestDto);
        TResponse Post<TResponse>(IReturn<TResponse> requestDto);
        TResponse Post<TResponse>(object requestDto);
        TResponse Post<TResponse>(string relativeOrAbsoluteUrl, object request);

        void Put(IReturnVoid requestDto);
        TResponse Put<TResponse>(IReturn<TResponse> requestDto);
        TResponse Put<TResponse>(object requestDto);
        TResponse Put<TResponse>(string relativeOrAbsoluteUrl, object requestDto);

        void Patch(IReturnVoid requestDto);
        TResponse Patch<TResponse>(IReturn<TResponse> requestDto);
        TResponse Patch<TResponse>(object requestDto);
        TResponse Patch<TResponse>(string relativeOrAbsoluteUrl, object requestDto);

        void CustomMethod(string httpVerb, IReturnVoid requestDto);
        TResponse CustomMethod<TResponse>(string httpVerb, IReturn<TResponse> requestDto);
        TResponse CustomMethod<TResponse>(string httpVerb, object requestDto);

        TResponse PostFile<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, string mimeType);
        TResponse PostFileWithRequest<TResponse>(Stream fileToUpload, string fileName, object request, string fieldName = "upload");
        TResponse PostFileWithRequest<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, object request, string fieldName = "upload");
        TResponse PostFilesWithRequest<TResponse>(object request, IEnumerable<UploadFile> files);
        TResponse PostFilesWithRequest<TResponse>(string relativeOrAbsoluteUrl, object request, IEnumerable<UploadFile> files);
    }
}