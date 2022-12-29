using System;
using System.Collections.Generic;
using System.IO;

namespace ServiceStack
{
    public interface IRestClient : IRestClientSync
    {
        void AddHeader(string name, string value);

        void ClearCookies();
        Dictionary<string, string> GetCookieValues();
        void SetCookie(string name, string value, TimeSpan? expiresIn = null);

        TResponse Get<TResponse>(string relativeOrAbsoluteUrl);
        IEnumerable<TResponse> GetLazy<TResponse>(IReturn<QueryResponse<TResponse>> queryDto);

        TResponse Delete<TResponse>(string relativeOrAbsoluteUrl);

        TResponse Post<TResponse>(string relativeOrAbsoluteUrl, object request);

        TResponse Put<TResponse>(string relativeOrAbsoluteUrl, object requestDto);

        TResponse Patch<TResponse>(string relativeOrAbsoluteUrl, object requestDto);

        TResponse Send<TResponse>(string httpMethod, string relativeOrAbsoluteUrl, object request);

        TResponse PostFile<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, string mimeType, string fieldName="file");
        TResponse PostFileWithRequest<TResponse>(Stream fileToUpload, string fileName, object request, string fieldName="file");
        TResponse PostFileWithRequest<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, object request, string fieldName="file");
        TResponse PostFilesWithRequest<TResponse>(object request, IEnumerable<UploadFile> files);
        TResponse PostFilesWithRequest<TResponse>(string relativeOrAbsoluteUrl, object request, IEnumerable<UploadFile> files);
    }
}