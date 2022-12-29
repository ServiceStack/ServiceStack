#nullable enable

#if !NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace ServiceStack;

public delegate object ResultsFilterHttpDelegate(Type responseType, string httpMethod, string requestUri, object request);

public delegate void ResultsFilterHttpResponseDelegate(HttpResponseMessage webResponse, object response, string httpMethod, string requestUri, object request);

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

    public static void AddBasicAuth(this HttpRequestMessage request, string basicAuthKey)
    {
        if (string.IsNullOrEmpty(basicAuthKey))
            return;
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuthKey);
    }

    public static void AddBasicAuth(this HttpRequestMessage request, string userName, string password)
    {
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
            return;

        request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes(userName + ":" + password)));
    }

    public static void AddApiKeyAuth(this HttpRequestMessage request, string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            return;

        request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes(apiKey + ":")));
    }

    public static void AddBearerToken(this HttpRequestMessage request, string bearerToken)
    {
        if (string.IsNullOrEmpty(bearerToken))
            return;

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
    }
}
#endif