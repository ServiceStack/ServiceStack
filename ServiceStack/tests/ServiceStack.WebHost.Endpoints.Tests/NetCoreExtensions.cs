using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace ServiceStack.WebHost.Endpoints.Tests;

public static class NetCoreExtensions
{
#if !NETFRAMEWORK
    public static HttpWebResponse GetResponse(this HttpWebRequest request)
    {
        return (HttpWebResponse)PclExport.Instance.GetResponse(request);
    }

    public static void AddRange(this HttpWebRequest request, int from, int? to) 
    {
        var rangeSpecifier = "bytes";
        var curRange = request.Headers[HttpRequestHeader.Range];

        if (string.IsNullOrEmpty(curRange)) 
        {
            curRange = rangeSpecifier + "=";
        }
        else
        {
            if (string.Compare(curRange.Substring(0, curRange.IndexOf('=')), rangeSpecifier, StringComparison.OrdinalIgnoreCase) != 0)
                throw new NotSupportedException("Invalid Range: " + curRange);
            curRange = string.Empty;
        }
        curRange += from.ToString();
        if (to != null) {
            curRange += "-" + to;
        }
        request.Headers[HttpRequestHeader.Range] = curRange;
    }

    public static void Close(this HttpWebResponse response)
    {
        response.Dispose();
    }
#endif
    public static void SetUserAgent(this HttpWebRequest request, string userAgent)
    {
#if !NETFRAMEWORK
        request.Headers[HttpRequestHeader.UserAgent] = userAgent;
#else
        request.UserAgent = userAgent;
#endif           
    }

    public static void SetContentLength(this HttpWebRequest request, int contentLength)
    {
#if !NETFRAMEWORK
        request.Headers[HttpRequestHeader.ContentLength] = contentLength.ToString();
#else
        request.ContentLength = contentLength;
#endif           
    }
}
