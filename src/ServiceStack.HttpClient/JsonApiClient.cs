#nullable enable
#if NET6_0_OR_GREATER

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceStack;

public interface IHasJsonApiClient
{
    public JsonApiClient? Client { get; }
}

/// <summary>
/// JsonHttpClient designed to work with 
/// </summary>
public class JsonApiClient : JsonHttpClient
{
    public static string? DefaultBasePath { get;set; } = "/api";

    public JsonApiClient(HttpClient httpClient)
    {
        this.HttpClient = httpClient;
        this.SetBaseUri(httpClient.BaseAddress?.ToString() ?? "/");
        if (DefaultBasePath != null)
            this.WithBasePath(DefaultBasePath);
    }
}

public static class JsonApiClientUtils
{
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

    class ApiResultsCache<T>
    {
        internal static ApiResult<T>? Instance { get; set; }
    }

    public static async Task<ApiResult<T>> ApiCacheAsync<T>(this IHasJsonApiClient instance, IReturn<T> requestDto)
    {
        if (ApiResultsCache<T>.Instance != null)
            return ApiResultsCache<T>.Instance;

        var apiResult = await instance.Client!.ApiAsync(requestDto);
        if (apiResult.IsSuccess)
            ApiResultsCache<T>.Instance = apiResult;
        return apiResult;
    }

    public static Task<ApiResult<AppMetadata>> ApiAppMetadataAsync(this IHasJsonApiClient instance) =>
        instance.ApiCacheAsync(new MetadataApp());
}

#endif