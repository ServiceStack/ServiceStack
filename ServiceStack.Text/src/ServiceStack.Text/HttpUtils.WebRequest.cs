#if !NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack;

public static partial class HttpUtils
{
    [ThreadStatic]
    public static IHttpResultsFilter ResultsFilter;

    public static string GetJsonFromUrl(this string url,
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return url.GetStringFromUrl(MimeTypes.Json, requestFilter, responseFilter);
    }

    public static Task<string> GetJsonFromUrlAsync(this string url,
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        return url.GetStringFromUrlAsync(MimeTypes.Json, requestFilter, responseFilter, token: token);
    }

    public static string GetXmlFromUrl(this string url,
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return url.GetStringFromUrl(MimeTypes.Xml, requestFilter, responseFilter);
    }

    public static Task<string> GetXmlFromUrlAsync(this string url,
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        return url.GetStringFromUrlAsync(MimeTypes.Xml, requestFilter, responseFilter, token: token);
    }

    public static string GetCsvFromUrl(this string url,
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return url.GetStringFromUrl(MimeTypes.Csv, requestFilter, responseFilter);
    }

    public static Task<string> GetCsvFromUrlAsync(this string url,
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        return url.GetStringFromUrlAsync(MimeTypes.Csv, requestFilter, responseFilter, token: token);
    }

    public static string GetStringFromUrl(this string url, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return SendStringToUrl(url, accept: accept, requestFilter: requestFilter, responseFilter: responseFilter);
    }

    public static Task<string> GetStringFromUrlAsync(this string url, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        return SendStringToUrlAsync(url, accept: accept, requestFilter: requestFilter, responseFilter: responseFilter,
            token: token);
    }

    public static string PostStringToUrl(this string url, string requestBody = null,
        string contentType = null, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return SendStringToUrl(url, method: "POST",
            requestBody: requestBody, contentType: contentType,
            accept: accept, requestFilter: requestFilter, responseFilter: responseFilter);
    }

    public static Task<string> PostStringToUrlAsync(this string url, string requestBody = null,
        string contentType = null, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        return SendStringToUrlAsync(url, method: "POST",
            requestBody: requestBody, contentType: contentType,
            accept: accept, requestFilter: requestFilter, responseFilter: responseFilter, token: token);
    }

    public static string PostToUrl(this string url, string formData = null, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return SendStringToUrl(url, method: "POST",
            contentType: MimeTypes.FormUrlEncoded, requestBody: formData,
            accept: accept, requestFilter: requestFilter, responseFilter: responseFilter);
    }

    public static Task<string> PostToUrlAsync(this string url, string formData = null, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        return SendStringToUrlAsync(url, method: "POST",
            contentType: MimeTypes.FormUrlEncoded, requestBody: formData,
            accept: accept, requestFilter: requestFilter, responseFilter: responseFilter, token: token);
    }

    public static string PostToUrl(this string url, object formData = null, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        string postFormData = formData != null ? QueryStringSerializer.SerializeToString(formData) : null;

        return SendStringToUrl(url, method: "POST",
            contentType: MimeTypes.FormUrlEncoded, requestBody: postFormData,
            accept: accept, requestFilter: requestFilter, responseFilter: responseFilter);
    }

    public static Task<string> PostToUrlAsync(this string url, object formData = null, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        string postFormData = formData != null ? QueryStringSerializer.SerializeToString(formData) : null;

        return SendStringToUrlAsync(url, method: "POST",
            contentType: MimeTypes.FormUrlEncoded, requestBody: postFormData,
            accept: accept, requestFilter: requestFilter, responseFilter: responseFilter, token: token);
    }

    public static string PostJsonToUrl(this string url, string json,
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return SendStringToUrl(url, method: "POST", requestBody: json, contentType: MimeTypes.Json,
            accept: MimeTypes.Json,
            requestFilter: requestFilter, responseFilter: responseFilter);
    }

    public static Task<string> PostJsonToUrlAsync(this string url, string json,
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        return SendStringToUrlAsync(url, method: "POST", requestBody: json, contentType: MimeTypes.Json,
            accept: MimeTypes.Json,
            requestFilter: requestFilter, responseFilter: responseFilter, token: token);
    }

    public static string PostJsonToUrl(this string url, object data,
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return SendStringToUrl(url, method: "POST", requestBody: data.ToJson(), contentType: MimeTypes.Json,
            accept: MimeTypes.Json,
            requestFilter: requestFilter, responseFilter: responseFilter);
    }

    public static Task<string> PostJsonToUrlAsync(this string url, object data,
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        return SendStringToUrlAsync(url, method: "POST", requestBody: data.ToJson(), contentType: MimeTypes.Json,
            accept: MimeTypes.Json,
            requestFilter: requestFilter, responseFilter: responseFilter, token: token);
    }

    public static string PostXmlToUrl(this string url, string xml,
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return SendStringToUrl(url, method: "POST", requestBody: xml, contentType: MimeTypes.Xml, accept: MimeTypes.Xml,
            requestFilter: requestFilter, responseFilter: responseFilter);
    }

    public static Task<string> PostXmlToUrlAsync(this string url, string xml,
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        return SendStringToUrlAsync(url, method: "POST", requestBody: xml, contentType: MimeTypes.Xml,
            accept: MimeTypes.Xml,
            requestFilter: requestFilter, responseFilter: responseFilter, token: token);
    }

    public static string PostCsvToUrl(this string url, string csv,
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return SendStringToUrl(url, method: "POST", requestBody: csv, contentType: MimeTypes.Csv, accept: MimeTypes.Csv,
            requestFilter: requestFilter, responseFilter: responseFilter);
    }

    public static Task<string> PostCsvToUrlAsync(this string url, string csv,
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        return SendStringToUrlAsync(url, method: "POST", requestBody: csv, contentType: MimeTypes.Csv,
            accept: MimeTypes.Csv,
            requestFilter: requestFilter, responseFilter: responseFilter, token: token);
    }

    public static string PutStringToUrl(this string url, string requestBody = null,
        string contentType = null, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return SendStringToUrl(url, method: "PUT",
            requestBody: requestBody, contentType: contentType,
            accept: accept, requestFilter: requestFilter, responseFilter: responseFilter);
    }

    public static Task<string> PutStringToUrlAsync(this string url, string requestBody = null,
        string contentType = null, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        return SendStringToUrlAsync(url, method: "PUT",
            requestBody: requestBody, contentType: contentType,
            accept: accept, requestFilter: requestFilter, responseFilter: responseFilter, token: token);
    }

    public static string PutToUrl(this string url, string formData = null, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return SendStringToUrl(url, method: "PUT",
            contentType: MimeTypes.FormUrlEncoded, requestBody: formData,
            accept: accept, requestFilter: requestFilter, responseFilter: responseFilter);
    }

    public static Task<string> PutToUrlAsync(this string url, string formData = null, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        return SendStringToUrlAsync(url, method: "PUT",
            contentType: MimeTypes.FormUrlEncoded, requestBody: formData,
            accept: accept, requestFilter: requestFilter, responseFilter: responseFilter, token: token);
    }

    public static string PutToUrl(this string url, object formData = null, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        string postFormData = formData != null ? QueryStringSerializer.SerializeToString(formData) : null;

        return SendStringToUrl(url, method: "PUT",
            contentType: MimeTypes.FormUrlEncoded, requestBody: postFormData,
            accept: accept, requestFilter: requestFilter, responseFilter: responseFilter);
    }

    public static Task<string> PutToUrlAsync(this string url, object formData = null, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        string postFormData = formData != null ? QueryStringSerializer.SerializeToString(formData) : null;

        return SendStringToUrlAsync(url, method: "PUT",
            contentType: MimeTypes.FormUrlEncoded, requestBody: postFormData,
            accept: accept, requestFilter: requestFilter, responseFilter: responseFilter, token: token);
    }

    public static string PutJsonToUrl(this string url, string json,
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return SendStringToUrl(url, method: "PUT", requestBody: json, contentType: MimeTypes.Json,
            accept: MimeTypes.Json,
            requestFilter: requestFilter, responseFilter: responseFilter);
    }

    public static Task<string> PutJsonToUrlAsync(this string url, string json,
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        return SendStringToUrlAsync(url, method: "PUT", requestBody: json, contentType: MimeTypes.Json,
            accept: MimeTypes.Json,
            requestFilter: requestFilter, responseFilter: responseFilter, token: token);
    }

    public static string PutJsonToUrl(this string url, object data,
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return SendStringToUrl(url, method: "PUT", requestBody: data.ToJson(), contentType: MimeTypes.Json,
            accept: MimeTypes.Json,
            requestFilter: requestFilter, responseFilter: responseFilter);
    }

    public static Task<string> PutJsonToUrlAsync(this string url, object data,
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        return SendStringToUrlAsync(url, method: "PUT", requestBody: data.ToJson(), contentType: MimeTypes.Json,
            accept: MimeTypes.Json,
            requestFilter: requestFilter, responseFilter: responseFilter, token: token);
    }

    public static string PutXmlToUrl(this string url, string xml,
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return SendStringToUrl(url, method: "PUT", requestBody: xml, contentType: MimeTypes.Xml, accept: MimeTypes.Xml,
            requestFilter: requestFilter, responseFilter: responseFilter);
    }

    public static Task<string> PutXmlToUrlAsync(this string url, string xml,
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        return SendStringToUrlAsync(url, method: "PUT", requestBody: xml, contentType: MimeTypes.Xml,
            accept: MimeTypes.Xml,
            requestFilter: requestFilter, responseFilter: responseFilter, token: token);
    }

    public static string PutCsvToUrl(this string url, string csv,
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return SendStringToUrl(url, method: "PUT", requestBody: csv, contentType: MimeTypes.Csv, accept: MimeTypes.Csv,
            requestFilter: requestFilter, responseFilter: responseFilter);
    }

    public static Task<string> PutCsvToUrlAsync(this string url, string csv,
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        return SendStringToUrlAsync(url, method: "PUT", requestBody: csv, contentType: MimeTypes.Csv,
            accept: MimeTypes.Csv,
            requestFilter: requestFilter, responseFilter: responseFilter, token: token);
    }

    public static string PatchStringToUrl(this string url, string requestBody = null,
        string contentType = null, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return SendStringToUrl(url, method: "PATCH",
            requestBody: requestBody, contentType: contentType,
            accept: accept, requestFilter: requestFilter, responseFilter: responseFilter);
    }

    public static Task<string> PatchStringToUrlAsync(this string url, string requestBody = null,
        string contentType = null, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        return SendStringToUrlAsync(url, method: "PATCH",
            requestBody: requestBody, contentType: contentType,
            accept: accept, requestFilter: requestFilter, responseFilter: responseFilter, token: token);
    }

    public static string PatchToUrl(this string url, string formData = null, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return SendStringToUrl(url, method: "PATCH",
            contentType: MimeTypes.FormUrlEncoded, requestBody: formData,
            accept: accept, requestFilter: requestFilter, responseFilter: responseFilter);
    }

    public static Task<string> PatchToUrlAsync(this string url, string formData = null, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        return SendStringToUrlAsync(url, method: "PATCH",
            contentType: MimeTypes.FormUrlEncoded, requestBody: formData,
            accept: accept, requestFilter: requestFilter, responseFilter: responseFilter, token: token);
    }

    public static string PatchToUrl(this string url, object formData = null, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        string postFormData = formData != null ? QueryStringSerializer.SerializeToString(formData) : null;

        return SendStringToUrl(url, method: "PATCH",
            contentType: MimeTypes.FormUrlEncoded, requestBody: postFormData,
            accept: accept, requestFilter: requestFilter, responseFilter: responseFilter);
    }

    public static Task<string> PatchToUrlAsync(this string url, object formData = null, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        string postFormData = formData != null ? QueryStringSerializer.SerializeToString(formData) : null;

        return SendStringToUrlAsync(url, method: "PATCH",
            contentType: MimeTypes.FormUrlEncoded, requestBody: postFormData,
            accept: accept, requestFilter: requestFilter, responseFilter: responseFilter, token: token);
    }

    public static string PatchJsonToUrl(this string url, string json,
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return SendStringToUrl(url, method: "PATCH", requestBody: json, contentType: MimeTypes.Json,
            accept: MimeTypes.Json,
            requestFilter: requestFilter, responseFilter: responseFilter);
    }

    public static Task<string> PatchJsonToUrlAsync(this string url, string json,
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        return SendStringToUrlAsync(url, method: "PATCH", requestBody: json, contentType: MimeTypes.Json,
            accept: MimeTypes.Json,
            requestFilter: requestFilter, responseFilter: responseFilter, token: token);
    }

    public static string PatchJsonToUrl(this string url, object data,
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return SendStringToUrl(url, method: "PATCH", requestBody: data.ToJson(), contentType: MimeTypes.Json,
            accept: MimeTypes.Json,
            requestFilter: requestFilter, responseFilter: responseFilter);
    }

    public static Task<string> PatchJsonToUrlAsync(this string url, object data,
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        return SendStringToUrlAsync(url, method: "PATCH", requestBody: data.ToJson(), contentType: MimeTypes.Json,
            accept: MimeTypes.Json,
            requestFilter: requestFilter, responseFilter: responseFilter, token: token);
    }

    public static string DeleteFromUrl(this string url, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return SendStringToUrl(url, method: "DELETE", accept: accept, requestFilter: requestFilter,
            responseFilter: responseFilter);
    }

    public static Task<string> DeleteFromUrlAsync(this string url, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        return SendStringToUrlAsync(url, method: "DELETE", accept: accept, requestFilter: requestFilter,
            responseFilter: responseFilter, token: token);
    }

    public static string OptionsFromUrl(this string url, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return SendStringToUrl(url, method: "OPTIONS", accept: accept, requestFilter: requestFilter,
            responseFilter: responseFilter);
    }

    public static Task<string> OptionsFromUrlAsync(this string url, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        return SendStringToUrlAsync(url, method: "OPTIONS", accept: accept, requestFilter: requestFilter,
            responseFilter: responseFilter, token: token);
    }

    public static string HeadFromUrl(this string url, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return SendStringToUrl(url, method: "HEAD", accept: accept, requestFilter: requestFilter,
            responseFilter: responseFilter);
    }

    public static Task<string> HeadFromUrlAsync(this string url, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        return SendStringToUrlAsync(url, method: "HEAD", accept: accept, requestFilter: requestFilter,
            responseFilter: responseFilter, token: token);
    }

    public static string SendStringToUrl(this string url, string method = null,
        string requestBody = null, string contentType = null, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        var webReq = WebRequest.CreateHttp(url);
        return SendStringToUrl(webReq, method, requestBody, contentType, accept, requestFilter, responseFilter);
    }

    public static async Task<string> SendStringToUrlAsync(this string url, string method = null,
        string requestBody = null,
        string contentType = null, string accept = "*/*", Action<HttpWebRequest> requestFilter = null,
        Action<HttpWebResponse> responseFilter = null, CancellationToken token = default)
    {
        var webReq = WebRequest.CreateHttp(url);
        return await SendStringToUrlAsync(webReq, method, requestBody, contentType, accept, requestFilter, responseFilter);
    }

    public static string SendStringToUrl(this HttpWebRequest webReq, string method, string requestBody, string contentType,
        string accept, Action<HttpWebRequest> requestFilter, Action<HttpWebResponse> responseFilter)
    {
        if (method != null)
            webReq.Method = method;
        if (contentType != null)
            webReq.ContentType = contentType;

        webReq.Accept = accept;
        PclExport.Instance.AddCompression(webReq);

        requestFilter?.Invoke(webReq);

        if (ResultsFilter != null)
        {
            return ResultsFilter.GetString(webReq, requestBody);
        }

        if (requestBody != null)
        {
            using var reqStream = PclExport.Instance.GetRequestStream(webReq);
            using var writer = new StreamWriter(reqStream, UseEncoding);
            writer.Write(requestBody);
        }
        else if (method != null && HasRequestBody(method))
        {
            webReq.ContentLength = 0;
        }

        using var webRes = webReq.GetResponse();
        using var stream = webRes.GetResponseStream();
        responseFilter?.Invoke((HttpWebResponse)webRes);
        return stream.ReadToEnd(UseEncoding);
    }
    
    public static async Task<string> SendStringToUrlAsync(this HttpWebRequest webReq, 
        string method, string requestBody, string contentType, string accept,
        Action<HttpWebRequest> requestFilter, Action<HttpWebResponse> responseFilter)
    {
        if (method != null)
            webReq.Method = method;
        if (contentType != null)
            webReq.ContentType = contentType;

        webReq.Accept = accept;
        PclExport.Instance.AddCompression(webReq);

        requestFilter?.Invoke(webReq);

        if (ResultsFilter != null)
        {
            var result = ResultsFilter.GetString(webReq, requestBody);
            return result;
        }

        if (requestBody != null)
        {
            using var reqStream = PclExport.Instance.GetRequestStream(webReq);
            using var writer = new StreamWriter(reqStream, UseEncoding);
            await writer.WriteAsync(requestBody).ConfigAwait();
        }
        else if (method != null && HasRequestBody(method))
        {
            webReq.ContentLength = 0;
        }

        using var webRes = await webReq.GetResponseAsync().ConfigAwait();
        responseFilter?.Invoke((HttpWebResponse)webRes);
        using var stream = webRes.GetResponseStream();
        return await stream.ReadToEndAsync().ConfigAwait();
    }

    public static byte[] GetBytesFromUrl(this string url, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return url.SendBytesToUrl(accept: accept, requestFilter: requestFilter, responseFilter: responseFilter);
    }

    public static Task<byte[]> GetBytesFromUrlAsync(this string url, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        return url.SendBytesToUrlAsync(accept: accept, requestFilter: requestFilter, responseFilter: responseFilter,
            token: token);
    }

    public static byte[] PostBytesToUrl(this string url, byte[] requestBody = null, string contentType = null,
        string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return SendBytesToUrl(url, method: "POST",
            contentType: contentType, requestBody: requestBody,
            accept: accept, requestFilter: requestFilter, responseFilter: responseFilter);
    }

    public static Task<byte[]> PostBytesToUrlAsync(this string url, byte[] requestBody = null,
        string contentType = null, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        return SendBytesToUrlAsync(url, method: "POST",
            contentType: contentType, requestBody: requestBody,
            accept: accept, requestFilter: requestFilter, responseFilter: responseFilter, token: token);
    }

    public static byte[] PutBytesToUrl(this string url, byte[] requestBody = null, string contentType = null,
        string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return SendBytesToUrl(url, method: "PUT",
            contentType: contentType, requestBody: requestBody,
            accept: accept, requestFilter: requestFilter, responseFilter: responseFilter);
    }

    public static Task<byte[]> PutBytesToUrlAsync(this string url, byte[] requestBody = null, string contentType = null,
        string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        return SendBytesToUrlAsync(url, method: "PUT",
            contentType: contentType, requestBody: requestBody,
            accept: accept, requestFilter: requestFilter, responseFilter: responseFilter, token: token);
    }

    public static byte[] SendBytesToUrl(this string url, string method = null,
        byte[] requestBody = null, string contentType = null, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        var webReq = WebRequest.CreateHttp(url);
        return SendBytesToUrl(webReq, method, requestBody, contentType, accept, requestFilter, responseFilter);
    }

    public static async Task<byte[]> SendBytesToUrlAsync(this string url, string method = null,
        byte[] requestBody = null, string contentType = null, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        var webReq = WebRequest.CreateHttp(url);
        return await SendBytesToUrlAsync(webReq, method, requestBody, contentType, accept, requestFilter, responseFilter, token);
    }

    public static byte[] SendBytesToUrl(this HttpWebRequest webReq, string method, byte[] requestBody, string contentType,
        string accept, Action<HttpWebRequest> requestFilter, Action<HttpWebResponse> responseFilter)
    {
        if (method != null)
            webReq.Method = method;

        if (contentType != null)
            webReq.ContentType = contentType;

        webReq.Accept = accept;
        PclExport.Instance.AddCompression(webReq);

        requestFilter?.Invoke(webReq);

        if (ResultsFilter != null)
        {
            return ResultsFilter.GetBytes(webReq, requestBody);
        }

        if (requestBody != null)
        {
            using var req = PclExport.Instance.GetRequestStream(webReq);
            req.Write(requestBody, 0, requestBody.Length);
        }

        using var webRes = PclExport.Instance.GetResponse(webReq);
        responseFilter?.Invoke((HttpWebResponse)webRes);

        using var stream = webRes.GetResponseStream();
        return stream.ReadFully();
    }
 
    public static async Task<byte[]> SendBytesToUrlAsync(this HttpWebRequest webReq, string method, byte[] requestBody,
        string contentType, string accept, Action<HttpWebRequest> requestFilter, Action<HttpWebResponse> responseFilter, CancellationToken token)
    {
        if (method != null)
            webReq.Method = method;
        if (contentType != null)
            webReq.ContentType = contentType;

        webReq.Accept = accept;
        PclExport.Instance.AddCompression(webReq);

        requestFilter?.Invoke(webReq);

        if (ResultsFilter != null)
        {
            var result = ResultsFilter.GetBytes(webReq, requestBody);
            return result;
        }

        if (requestBody != null)
        {
            using var req = PclExport.Instance.GetRequestStream(webReq);
            await req.WriteAsync(requestBody, 0, requestBody.Length, token).ConfigAwait();
        }

        var webRes = await webReq.GetResponseAsync().ConfigAwait();
        responseFilter?.Invoke((HttpWebResponse)webRes);

        using var stream = webRes.GetResponseStream();
        return await stream.ReadFullyAsync(token).ConfigAwait();
    }
   
    public static Stream GetStreamFromUrl(this string url, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return url.SendStreamToUrl(accept: accept, requestFilter: requestFilter, responseFilter: responseFilter);
    }

    public static Task<Stream> GetStreamFromUrlAsync(this string url, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        return url.SendStreamToUrlAsync(accept: accept, requestFilter: requestFilter, responseFilter: responseFilter,
            token: token);
    }

    public static Stream PostStreamToUrl(this string url, Stream requestBody = null, string contentType = null,
        string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return SendStreamToUrl(url, method: "POST",
            contentType: contentType, requestBody: requestBody,
            accept: accept, requestFilter: requestFilter, responseFilter: responseFilter);
    }

    public static Task<Stream> PostStreamToUrlAsync(this string url, Stream requestBody = null,
        string contentType = null, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        return SendStreamToUrlAsync(url, method: "POST",
            contentType: contentType, requestBody: requestBody,
            accept: accept, requestFilter: requestFilter, responseFilter: responseFilter, token: token);
    }

    public static Stream PutStreamToUrl(this string url, Stream requestBody = null, string contentType = null,
        string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return SendStreamToUrl(url, method: "PUT",
            contentType: contentType, requestBody: requestBody,
            accept: accept, requestFilter: requestFilter, responseFilter: responseFilter);
    }

    public static Task<Stream> PutStreamToUrlAsync(this string url, Stream requestBody = null,
        string contentType = null, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        return SendStreamToUrlAsync(url, method: "PUT",
            contentType: contentType, requestBody: requestBody,
            accept: accept, requestFilter: requestFilter, responseFilter: responseFilter, token: token);
    }

    /// <summary>
    /// Returns HttpWebResponse Stream which must be disposed
    /// </summary>
    public static Stream SendStreamToUrl(this string url, string method = null,
        Stream requestBody = null, string contentType = null, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        var webReq = WebRequest.CreateHttp(url);
        if (method != null)
            webReq.Method = method;

        if (contentType != null)
            webReq.ContentType = contentType;

        webReq.Accept = accept;
        PclExport.Instance.AddCompression(webReq);

        requestFilter?.Invoke(webReq);

        if (ResultsFilter != null)
        {
            return new MemoryStream(ResultsFilter.GetBytes(webReq, requestBody.ReadFully()));
        }

        if (requestBody != null)
        {
            using var req = PclExport.Instance.GetRequestStream(webReq);
            requestBody.CopyTo(req);
        }

        var webRes = PclExport.Instance.GetResponse(webReq);
        responseFilter?.Invoke((HttpWebResponse)webRes);

        var stream = webRes.GetResponseStream();
        return stream;
    }

    /// <summary>
    /// Returns HttpWebResponse Stream which must be disposed
    /// </summary>
    public static async Task<Stream> SendStreamToUrlAsync(this string url, string method = null,
        Stream requestBody = null, string contentType = null, string accept = "*/*",
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null,
        CancellationToken token = default)
    {
        var webReq = WebRequest.CreateHttp(url);
        if (method != null)
            webReq.Method = method;
        if (contentType != null)
            webReq.ContentType = contentType;

        webReq.Accept = accept;
        PclExport.Instance.AddCompression(webReq);

        requestFilter?.Invoke(webReq);

        if (ResultsFilter != null)
        {
            return new MemoryStream(ResultsFilter.GetBytes(webReq,
                await requestBody.ReadFullyAsync(token).ConfigAwait()));
        }

        if (requestBody != null)
        {
            using var req = PclExport.Instance.GetRequestStream(webReq);
            await requestBody.CopyToAsync(req, token).ConfigAwait();
        }

        var webRes = await webReq.GetResponseAsync().ConfigAwait();
        responseFilter?.Invoke((HttpWebResponse)webRes);

        var stream = webRes.GetResponseStream();
        return stream;
    }

    public static HttpStatusCode? GetResponseStatus(this string url)
    {
        try
        {
            var webReq = WebRequest.CreateHttp(url);
            using var webRes = PclExport.Instance.GetResponse(webReq);
            var httpRes = webRes as HttpWebResponse;
            return httpRes?.StatusCode;
        }
        catch (Exception ex)
        {
            return ex.GetStatus();
        }
    }

    public static HttpWebResponse GetErrorResponse(this string url)
    {
        try
        {
            var webReq = WebRequest.Create(url);
            using var webRes = PclExport.Instance.GetResponse(webReq);
            webRes.ReadToEnd();
            return null;
        }
        catch (WebException webEx)
        {
            return (HttpWebResponse)webEx.Response;
        }
    }

    public static async Task<HttpWebResponse> GetErrorResponseAsync(this string url)
    {
        try
        {
            var webReq = WebRequest.Create(url);
            using var webRes = await webReq.GetResponseAsync().ConfigAwait();
            await webRes.ReadToEndAsync().ConfigAwait();
            return null;
        }
        catch (WebException webEx)
        {
            return (HttpWebResponse)webEx.Response;
        }
    }

    public static void UploadFile(this WebRequest webRequest, Stream fileStream, string fileName, string mimeType,
        string accept = null, Action<HttpWebRequest> requestFilter = null, string method = "POST",
        string field = "file")
    {
        var httpReq = (HttpWebRequest)webRequest;
        httpReq.Method = method;

        if (accept != null)
            httpReq.Accept = accept;

        requestFilter?.Invoke(httpReq);

        var boundary = Guid.NewGuid().ToString("N");

        httpReq.ContentType = "multipart/form-data; boundary=\"" + boundary + "\"";

        var boundaryBytes = ("\r\n--" + boundary + "--\r\n").ToAsciiBytes();

        var headerBytes = GetHeaderBytes(fileName, mimeType, field, boundary);

        var contentLength = fileStream.Length + headerBytes.Length + boundaryBytes.Length;
        PclExport.Instance.InitHttpWebRequest(httpReq,
            contentLength: contentLength, allowAutoRedirect: false, keepAlive: false);

        if (ResultsFilter != null)
        {
            ResultsFilter.UploadStream(httpReq, fileStream, fileName);
            return;
        }

        using var outputStream = PclExport.Instance.GetRequestStream(httpReq);
        outputStream.Write(headerBytes, 0, headerBytes.Length);
        fileStream.CopyTo(outputStream, 4096);
        outputStream.Write(boundaryBytes, 0, boundaryBytes.Length);
        PclExport.Instance.CloseStream(outputStream);
    }

    public static async Task UploadFileAsync(this WebRequest webRequest, Stream fileStream, string fileName,
        string mimeType,
        string accept = null, Action<HttpWebRequest> requestFilter = null, string method = "POST",
        string field = "file",
        CancellationToken token = default)
    {
        var httpReq = (HttpWebRequest)webRequest;
        httpReq.Method = method;

        if (accept != null)
            httpReq.Accept = accept;

        requestFilter?.Invoke(httpReq);

        var boundary = Guid.NewGuid().ToString("N");

        httpReq.ContentType = "multipart/form-data; boundary=\"" + boundary + "\"";

        var boundaryBytes = ("\r\n--" + boundary + "--\r\n").ToAsciiBytes();

        var headerBytes = GetHeaderBytes(fileName, mimeType, field, boundary);

        var contentLength = fileStream.Length + headerBytes.Length + boundaryBytes.Length;
        PclExport.Instance.InitHttpWebRequest(httpReq,
            contentLength: contentLength, allowAutoRedirect: false, keepAlive: false);

        if (ResultsFilter != null)
        {
            ResultsFilter.UploadStream(httpReq, fileStream, fileName);
            return;
        }

        using var outputStream = PclExport.Instance.GetRequestStream(httpReq);
        await outputStream.WriteAsync(headerBytes, 0, headerBytes.Length, token).ConfigAwait();
        await fileStream.CopyToAsync(outputStream, 4096, token).ConfigAwait();
        await outputStream.WriteAsync(boundaryBytes, 0, boundaryBytes.Length, token).ConfigAwait();
        PclExport.Instance.CloseStream(outputStream);
    }

    public static void UploadFile(this WebRequest webRequest, Stream fileStream, string fileName)
    {
        if (fileName == null)
            throw new ArgumentNullException(nameof(fileName));
        var mimeType = MimeTypes.GetMimeType(fileName);
        if (mimeType == null)
            throw new ArgumentException("Mime-type not found for file: " + fileName);

        UploadFile(webRequest, fileStream, fileName, mimeType);
    }

    public static async Task UploadFileAsync(this WebRequest webRequest, Stream fileStream, string fileName,
        CancellationToken token = default)
    {
        if (fileName == null)
            throw new ArgumentNullException(nameof(fileName));
        var mimeType = MimeTypes.GetMimeType(fileName);
        if (mimeType == null)
            throw new ArgumentException("Mime-type not found for file: " + fileName);

        await UploadFileAsync(webRequest, fileStream, fileName, mimeType, token: token).ConfigAwait();
    }

    public static string PostXmlToUrl(this string url, object data,
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return SendStringToUrl(url, method: "POST", requestBody: data.ToXml(), contentType: MimeTypes.Xml,
            accept: MimeTypes.Xml,
            requestFilter: requestFilter, responseFilter: responseFilter);
    }

    public static string PostCsvToUrl(this string url, object data,
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return SendStringToUrl(url, method: "POST", requestBody: data.ToCsv(), contentType: MimeTypes.Csv,
            accept: MimeTypes.Csv,
            requestFilter: requestFilter, responseFilter: responseFilter);
    }

    public static string PutXmlToUrl(this string url, object data,
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return SendStringToUrl(url, method: "PUT", requestBody: data.ToXml(), contentType: MimeTypes.Xml,
            accept: MimeTypes.Xml,
            requestFilter: requestFilter, responseFilter: responseFilter);
    }

    public static string PutCsvToUrl(this string url, object data,
        Action<HttpWebRequest> requestFilter = null, Action<HttpWebResponse> responseFilter = null)
    {
        return SendStringToUrl(url, method: "PUT", requestBody: data.ToCsv(), contentType: MimeTypes.Csv,
            accept: MimeTypes.Csv,
            requestFilter: requestFilter, responseFilter: responseFilter);
    }

    public static WebResponse PostFileToUrl(this string url,
        FileInfo uploadFileInfo, string uploadFileMimeType,
        string accept = null,
        Action<HttpWebRequest> requestFilter = null)
    {
        var webReq = WebRequest.CreateHttp(url);
        using (var fileStream = uploadFileInfo.OpenRead())
        {
            var fileName = uploadFileInfo.Name;

            webReq.UploadFile(fileStream, fileName, uploadFileMimeType, accept: accept, requestFilter: requestFilter,
                method: "POST");
        }

        if (ResultsFilter != null)
            return null;

        return webReq.GetResponse();
    }

    public static async Task<WebResponse> PostFileToUrlAsync(this string url,
        FileInfo uploadFileInfo, string uploadFileMimeType,
        string accept = null,
        Action<HttpWebRequest> requestFilter = null, CancellationToken token = default)
    {
        var webReq = WebRequest.CreateHttp(url);
        using (var fileStream = uploadFileInfo.OpenRead())
        {
            var fileName = uploadFileInfo.Name;

            await webReq.UploadFileAsync(fileStream, fileName, uploadFileMimeType, accept: accept,
                requestFilter: requestFilter, method: "POST", token: token).ConfigAwait();
        }

        if (ResultsFilter != null)
            return null;

        return await webReq.GetResponseAsync().ConfigAwait();
    }

    public static WebResponse PutFileToUrl(this string url,
        FileInfo uploadFileInfo, string uploadFileMimeType,
        string accept = null,
        Action<HttpWebRequest> requestFilter = null)
    {
        var webReq = WebRequest.CreateHttp(url);
        using (var fileStream = uploadFileInfo.OpenRead())
        {
            var fileName = uploadFileInfo.Name;

            webReq.UploadFile(fileStream, fileName, uploadFileMimeType, accept: accept, requestFilter: requestFilter,
                method: "PUT");
        }

        if (ResultsFilter != null)
            return null;

        return webReq.GetResponse();
    }

    public static async Task<WebResponse> PutFileToUrlAsync(this string url,
        FileInfo uploadFileInfo, string uploadFileMimeType,
        string accept = null,
        Action<HttpWebRequest> requestFilter = null, CancellationToken token = default)
    {
        var webReq = WebRequest.CreateHttp(url);
        using (var fileStream = uploadFileInfo.OpenRead())
        {
            var fileName = uploadFileInfo.Name;

            await webReq.UploadFileAsync(fileStream, fileName, uploadFileMimeType, accept: accept,
                requestFilter: requestFilter, method: "PUT", token: token).ConfigAwait();
        }

        if (ResultsFilter != null)
            return null;

        return await webReq.GetResponseAsync().ConfigAwait();
    }

    public static WebResponse UploadFile(this WebRequest webRequest,
        FileInfo uploadFileInfo, string uploadFileMimeType)
    {
        using (var fileStream = uploadFileInfo.OpenRead())
        {
            var fileName = uploadFileInfo.Name;

            webRequest.UploadFile(fileStream, fileName, uploadFileMimeType);
        }

        if (ResultsFilter != null)
            return null;

        return webRequest.GetResponse();
    }

    public static async Task<WebResponse> UploadFileAsync(this WebRequest webRequest,
        FileInfo uploadFileInfo, string uploadFileMimeType)
    {
        using (var fileStream = uploadFileInfo.OpenRead())
        {
            var fileName = uploadFileInfo.Name;

            await webRequest.UploadFileAsync(fileStream, fileName, uploadFileMimeType).ConfigAwait();
        }

        if (ResultsFilter != null)
            return null;

        return await webRequest.GetResponseAsync().ConfigAwait();
    }
    
    private static byte[] GetHeaderBytes(string fileName, string mimeType, string field, string boundary)
    {
        var header = "\r\n--" + boundary +
                     $"\r\nContent-Disposition: form-data; name=\"{field}\"; filename=\"{fileName}\"\r\nContent-Type: {mimeType}\r\n\r\n";

        var headerBytes = header.ToAsciiBytes();
        return headerBytes;
    }

    public static void DownloadFileTo(this string downloadUrl, string fileName, 
        List<NameValue> headers = null)
    {
        var webClient = new WebClient();
        if (headers != null)
        {
            foreach (var header in headers)
            {
                webClient.Headers[header.Name] = header.Value;
            }
        }
        webClient.DownloadFile(downloadUrl, fileName);
    }
    
    public static void SetRange(this HttpWebRequest request, long from, long? to) 
    {
        if (to != null)
            request.AddRange(from, to.Value);
        else
            request.AddRange(from);
    }

    public static void AddHeader(this HttpWebRequest res, string name, string value) =>
        res.Headers[name] = value;
    public static string GetHeader(this HttpWebRequest res, string name) =>
        res.Headers.Get(name);
    public static string GetHeader(this HttpWebResponse res, string name) =>
        res.Headers.Get(name);

    public static HttpWebRequest WithHeader(this HttpWebRequest httpReq, string name, string value)
    {
        httpReq.Headers[name] = value;
        return httpReq;
    }

    /// <summary>
    /// Populate HttpRequestMessage with a simpler, untyped API
    /// Syntax compatible with HttpClient's HttpRequestMessage
    /// </summary>
    public static HttpWebRequest With(this HttpWebRequest httpReq, Action<HttpRequestConfig> configure)
    {
        var config = new HttpRequestConfig();
        configure(config);
        
        if (config.Accept != null)
            httpReq.Accept = config.Accept;

        if (config.UserAgent != null)
            httpReq.UserAgent = config.UserAgent;

        if (config.ContentType != null)
            httpReq.ContentType = config.ContentType;

        if (config.Referer != null)
            httpReq.Referer = config.Referer;

        if (config.Authorization != null)
            httpReq.Headers[HttpHeaders.Authorization] = 
                config.Authorization.Name + " " + config.Authorization.Value;
        
        if (config.Range != null)
            httpReq.SetRange(config.Range.From, config.Range.To);

        if (config.Expect != null)
            httpReq.Expect = config.Expect;

        if (config.TransferEncodingChunked != null)
            httpReq.TransferEncoding = "chunked";
        else if (config.TransferEncoding?.Length > 0)
            httpReq.TransferEncoding = string.Join(", ", config.TransferEncoding);

        foreach (var entry in config.Headers)
        {
            httpReq.Headers[entry.Name] = entry.Value;
        }
        
        return httpReq;
    }
}

public interface IHttpResultsFilter : IDisposable
{
    string GetString(HttpWebRequest webReq, string reqBody);
    byte[] GetBytes(HttpWebRequest webReq, byte[] reqBody);
    void UploadStream(HttpWebRequest webRequest, Stream fileStream, string fileName);
}

public class HttpResultsFilter : IHttpResultsFilter
{
    private readonly IHttpResultsFilter previousFilter;

    public string StringResult { get; set; }
    public byte[] BytesResult { get; set; }

    public Func<HttpWebRequest, string, string> StringResultFn { get; set; }
    public Func<HttpWebRequest, byte[], byte[]> BytesResultFn { get; set; }
    public Action<HttpWebRequest, Stream, string> UploadFileFn { get; set; }

    public HttpResultsFilter(string stringResult = null, byte[] bytesResult = null)
    {
        StringResult = stringResult;
        BytesResult = bytesResult;

        previousFilter = HttpUtils.ResultsFilter;
        HttpUtils.ResultsFilter = this;
    }

    public void Dispose()
    {
        HttpUtils.ResultsFilter = previousFilter;
    }

    public string GetString(HttpWebRequest webReq, string reqBody)
    {
        return StringResultFn != null
            ? StringResultFn(webReq, reqBody)
            : StringResult;
    }

    public byte[] GetBytes(HttpWebRequest webReq, byte[] reqBody)
    {
        return BytesResultFn != null
            ? BytesResultFn(webReq, reqBody)
            : BytesResult;
    }

    public void UploadStream(HttpWebRequest webRequest, Stream fileStream, string fileName)
    {
        UploadFileFn?.Invoke(webRequest, fileStream, fileName);
    }
}

public static class HttpClientExt
{
    /// <summary>
    /// Case-insensitive, trimmed compare of two content types from start to ';', i.e. without charset suffix 
    /// </summary>
    public static bool MatchesContentType(this HttpWebResponse res, string matchesContentType) => 
        MimeTypes.MatchesContentType(res.Headers[HttpHeaders.ContentType], matchesContentType);
    
    /// <summary>
    /// Returns null for unknown Content-length
    /// Syntax + Behavior compatible with HttpClient HttpResponseMessage 
    /// </summary>
    public static long? GetContentLength(this HttpWebResponse res) =>
        res.ContentLength == -1 ? null : res.ContentLength;
}
#endif
