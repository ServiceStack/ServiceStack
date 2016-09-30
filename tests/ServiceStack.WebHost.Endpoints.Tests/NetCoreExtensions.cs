#if NETCORE
using System;
using System.Net;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public static class NetCoreExtensions
    {
        public static HttpWebResponse GetResponse(this HttpWebRequest request)
        {
            var asyncResult = request.BeginGetResponse((result) => {
                result.AsyncState = request.EndGetResponse(result);
            });
            asyncResult.AsyncWaitHandle.WaitOne();

            return (HttpWebResponse) asyncResult.AsyncState;
        }

        public static bool AddRange(this HttpWebRequest request, int from, int to) 
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
    }
}
#endif