#nullable enable
#if NET6_0_OR_GREATER

using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceStack;

/// <summary>
/// JsonHttpClient designed to work with 
/// </summary>
public class JsonApiClient : JsonHttpClient
{
    public static string? BasePath = "/api";

    public JsonApiClient(HttpClient httpClient)
    {
        this.HttpClient = httpClient;
        this.SetBaseUri(httpClient.BaseAddress?.ToString() ?? "/");
        if (BasePath != null)
            this.WithBasePath(BasePath);
    }
}

public static class JsonApiClientUtils
{
    public static IHttpClientBuilder AddJsonApiClient(this IServiceCollection services, string baseUrl)
    {
        return services.AddHttpClient<JsonApiClient>(client => client.BaseAddress = new Uri(baseUrl));
    }
}

#endif