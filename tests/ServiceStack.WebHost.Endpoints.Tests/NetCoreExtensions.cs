using System;
using System.Net;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public static class NetCoreExtensions
    {
#if NETCORE
        public static HttpWebResponse GetResponse(this HttpWebRequest request)
        {
            return (HttpWebResponse)PclExport.Instance.GetResponse(request);
        }

        public static bool AddRange(this HttpWebRequest request, int from, int? to) 
        {
            string rangeSpecifier = "bytes";
            string curRange = request.Headers[HttpRequestHeader.Range];
 
            if ((curRange == null) || (curRange.Length == 0)) {
                curRange = rangeSpecifier + "=";
            }
            else {
                if (String.Compare(curRange.Substring(0, curRange.IndexOf('=')), rangeSpecifier, StringComparison.OrdinalIgnoreCase) != 0) {
                    return false;
                }
                curRange = string.Empty;
            }
            curRange += from.ToString();
            if (to != null) {
                curRange += "-" + to.ToString();
            }
            request.Headers[HttpRequestHeader.Range] = curRange;
            return true;
        }

        public static void Close(this HttpWebResponse response)
        {
            response.Dispose();
        }
#endif
        public static void SetUserAgent(this HttpWebRequest request, string userAgent)
        {
#if NETCORE
            request.Headers[HttpRequestHeader.UserAgent] = userAgent;
#else
            request.UserAgent = userAgent;
#endif           
        }

        public static void SetContentLength(this HttpWebRequest request, int contentLength)
        {
#if NETCORE
            request.Headers[HttpRequestHeader.ContentLength] = contentLength.ToString();
#else
            request.ContentLength = contentLength;
#endif           
        }
    }
}