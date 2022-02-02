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
}
#endif
