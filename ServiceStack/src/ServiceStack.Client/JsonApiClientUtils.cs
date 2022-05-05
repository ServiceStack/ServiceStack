#nullable enable

#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.IO;
using ServiceStack.Text;

namespace ServiceStack;

public delegate object ResultsFilterHttpDelegate(Type responseType, string httpMethod, string requestUri, object? request);

public delegate void ResultsFilterHttpResponseDelegate(HttpResponseMessage webResponse, object? response, string httpMethod, string requestUri, object? request);

public delegate object ExceptionFilterHttpDelegate(HttpResponseMessage webResponse, string requestUri, Type responseType);

public static class JsonApiClientUtils
{
    public static Dictionary<string, string> ToDictionary(this HttpResponseHeaders headers)
    {
        var to = new Dictionary<string, string>();
        foreach (var header in headers)
        {
            to[header.Key] = string.Join(", ", header.Value);
        }
        return to;
    }

    public static WebHeaderCollection ToWebHeaderCollection(this HttpResponseHeaders headers)
    {
        var to = new WebHeaderCollection();
        foreach (var header in headers)
        {
            to[header.Key] = string.Join(", ", header.Value);
        }
        return to;
    }

    public static string? GetContentType(this HttpResponseMessage httpRes)
    {
        return httpRes.Headers.TryGetValues(HttpHeaders.ContentType, out var values)
            ? values.FirstOrDefault()
            : null;
    }

    public static void AddBasicAuth(this HttpRequestMessage request, string userName, string password)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes(userName + ":" + password)));
    }

    public static void AddApiKeyAuth(this HttpRequestMessage request, string apiKey)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes(apiKey + ":")));
    }

    public static void AddBearerToken(this HttpRequestMessage request, string bearerToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
    }

    public static IHttpClientBuilder AddJsonApiClient(this IServiceCollection services, string baseUrl)
    {
        return services.AddHttpClient<JsonApiClient>(client => client.BaseAddress = new Uri(baseUrl));
    }
    
    public static async Task<ApiResult<TResponse>> ApiAsync<TResponse>(this IHasJsonApiClient instance, IReturn<TResponse> request) =>
        await instance.Client!.ApiAsync(request);

    public static async Task<ApiResult<EmptyResponse>> ApiAsync(this IHasJsonApiClient instance, IReturnVoid request) =>
        await instance.Client!.ApiAsync(request);

    public static async Task<TResponse> SendAsync<TResponse>(this IHasJsonApiClient instance, IReturn<TResponse> request) =>
        await instance.Client!.SendAsync(request);

    static class ApiResultsCache<T>
    {
        internal static ApiResult<T>? Instance { get; set; }
    }

    public static async Task<ApiResult<T>> ApiCacheAsync<T>(this IHasJsonApiClient instance, IReturn<T> requestDto)
    {
        if (ApiResultsCache<T>.Instance != null)
            return ApiResultsCache<T>.Instance;

        var api = await instance.Client!.ApiAsync(requestDto);
        if (api.Succeeded)
            ApiResultsCache<T>.Instance = api;
        return api;
    }

    public static Task<ApiResult<AppMetadata>> ApiAppMetadataAsync(this IHasJsonApiClient instance, bool reload=false) =>
        !reload ? instance.ApiCacheAsync(new MetadataApp()) : instance.Client!.ApiAsync(new MetadataApp());

    public static string ReadAsString(this HttpContent content)
    {
        using var reader = new StreamReader(content.ReadAsStream());
        return reader.ReadToEnd();        
    }

    public static byte[] ReadAsByteArray(this HttpContent content)
    {
        using var stream = content.ReadAsStream();
        return stream.ReadFully();
    }

    public static ReadOnlyMemory<byte> ReadAsMemoryBytes(this HttpContent content)
    {
        using var stream = content.ReadAsStream();
        return stream.ReadFullyAsMemory();
    }

    public static MultipartFormDataContent AddParam(this MultipartFormDataContent content, string key, string value)
    {
        content.Add(new StringContent(value), $"\"{key}\"");
        return content;
    }

    public static MultipartFormDataContent AddParam(this MultipartFormDataContent content, string key, object value)
    {
        content.Add(new StringContent(value.ToJsv(), encoding:null, mediaType:MimeTypes.Jsv), $"\"{key}\"");
        return content;
    }
    
    public static MultipartFormDataContent AddParams(this MultipartFormDataContent content, System.Collections.IDictionary map)
    {
        foreach (System.Collections.DictionaryEntry entry in map)
        {
            var strVal = entry.Value.ToJsv();
            if (strVal == null) continue;
            content.Add(new StringContent(strVal), $"\"{entry.Key}\"");
        }
        return content;
    }
    
    public static MultipartFormDataContent AddParams(this MultipartFormDataContent content, Dictionary<string, object> map)
    {
        foreach (var entry in map)
        {
            var strVal = entry.Value.ToJsv();
            if (strVal == null) continue;
            content.Add(new StringContent(strVal), $"\"{entry.Key}\"");
        }
        return content;
    }

    public static MultipartFormDataContent AddParams<T>(this MultipartFormDataContent content, T dto) => 
        content.AddParams(dto.ToObjectDictionary());

    public static MultipartFormDataContent AddJsvParam<T>(this MultipartFormDataContent content, string key, T value)
    {
        content.Add(new StringContent(value.ToJsv(), encoding:null, mediaType:MimeTypes.Jsv), $"\"{key}\"");
        return content;
    }

    public static MultipartFormDataContent AddJsonParam<T>(this MultipartFormDataContent content, string key, T value)
    {
        content.Add(new StringContent(value.ToJson(), encoding:null, mediaType:MimeTypes.Json), $"\"{key}\"");
        return content;
    }

    public static MultipartFormDataContent AddCsvParam<T>(this MultipartFormDataContent content, string key, T value)
    {
        content.Add(new StringContent(value.ToCsv(), encoding:null, mediaType:MimeTypes.Csv), $"\"{key}\"");
        return content;
    }

    public static HttpContent AddFileInfo(this HttpContent content, string fieldName, string fileName, string? mimeType=null)
    {
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(mimeType ?? MimeTypes.GetMimeType(fileName));
        content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") {
            Name = fieldName,
            FileName = fileName,
        };
        return content;
    }

    public static MultipartFormDataContent AddFile(this MultipartFormDataContent content, string fieldName, string fileName, Stream fileContents, string? mimeType=null)
    {
        content.Add(new StreamContent(fileContents)
            .AddFileInfo(fieldName: fieldName, fileName: fileName, mimeType: mimeType));
        return content;
    }

    public static MultipartFormDataContent AddFile(this MultipartFormDataContent content, string fieldName, string fileName, ReadOnlyMemory<byte> fileContents, string? mimeType=null)
    {
        content.Add(new ReadOnlyMemoryContent(fileContents)
            .AddFileInfo(fieldName: fieldName, fileName: fileName, mimeType: mimeType));
        return content;
    }

    public static MultipartFormDataContent AddFile(this MultipartFormDataContent content, string fieldName, FileInfo file, string? mimeType=null)
    {
        using var fs = file.OpenRead();
        content.Add(new ReadOnlyMemoryContent(fs.ReadFullyAsMemory())
            .AddFileInfo(fieldName: fieldName, fileName: file.Name, mimeType: mimeType));
        return content;
    }

    public static async Task<MultipartFormDataContent> AddFileAsync(this MultipartFormDataContent content, string fieldName, FileInfo file, string? mimeType=null)
    {
        await using var fs = file.OpenRead();
        content.Add(new ReadOnlyMemoryContent(await fs.ReadFullyAsMemoryAsync().ConfigAwait())
            .AddFileInfo(fieldName: fieldName, fileName: file.Name, mimeType: mimeType));
        return content;
    }

    public static MultipartFormDataContent AddFile(this MultipartFormDataContent content, string fieldName, IVirtualFile file, string? mimeType=null)
    {
        content.Add(file.ToHttpContent()
            .AddFileInfo(fieldName: fieldName, fileName: file.Name, mimeType: mimeType));
        return content;
    }

    public static HttpContent ToHttpContent(this IVirtualFile file)
    {
        var fileContents = file.GetContents();
        HttpContent? httpContent = fileContents is ReadOnlyMemory<byte> romBytes
            ? new ReadOnlyMemoryContent(romBytes)
            : fileContents is string str
                ? new StringContent(str)
                : fileContents is ReadOnlyMemory<char> romChars
                    ? new ReadOnlyMemoryContent(romChars.ToUtf8())
                    : fileContents is byte[] bytes
                        ? new ByteArrayContent(bytes, 0, bytes.Length)
                        : null;

        if (httpContent != null)
            return httpContent;

        using var stream = fileContents as Stream ?? file.OpenRead();
        return new ReadOnlyMemoryContent(stream.ReadFullyAsMemory());
    }
}
#endif
