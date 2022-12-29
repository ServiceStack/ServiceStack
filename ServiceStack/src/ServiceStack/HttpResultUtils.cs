using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;
using ServiceStack.Text.Pools;
using ServiceStack.Web;

namespace ServiceStack
{
    public static class HttpResultUtils
    {
        /// <summary>
        /// Shortcut to get the ResponseDTO whether it's bare or inside a IHttpResult
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static object GetDto(this object response)
        {
            if (response == null) return null;
            return response is IHttpResult httpResult ? httpResult.Response : response;
        }

        /// <summary>
        /// Alias of AsDto
        /// </summary>
        public static object GetResponseDto(this object response)
        {
            return GetDto(response);
        }

        /// <summary>
        /// Shortcut to get the ResponseDTO whether it's bare or inside a IHttpResult
        /// </summary>
        /// <param name="response"></param>
        /// <returns>TResponse if found; otherwise null</returns>
        public static TResponse GetDto<TResponse>(this object response) where TResponse : class
        {
            if (response == null) return default(TResponse);
            return (response is IHttpResult httpResult ? httpResult.Response : response) as TResponse;
        }

        /// <summary>
        /// Alias of AsDto
        /// </summary>
        public static TResponse GetResponseDto<TResponse>(this object response) where TResponse : class
        {
            return GetDto<TResponse>(response);
        }

        public static object CreateErrorResponse(this IHttpError httpError)
        {
            if (httpError == null) return null;
            var errorDto = httpError.GetDto();
            if (errorDto != null) return errorDto;

            return new ErrorResponse
            {
                ResponseStatus = new ResponseStatus
                {
                    ErrorCode = httpError.ErrorCode,
                    Message = httpError.Message,
                    StackTrace = httpError.StackTrace,
                }
            };
        }

        /// <summary>
        /// Whether the response is an IHttpError or Exception or ErrorResponse
        /// </summary>
        public static bool IsErrorResponse(this object response)
        {
            return response is IHttpError or Exception or ErrorResponse;
        }

        /// <summary>
        /// rangeHeader should be of the format "bytes=0-" or "bytes=0-12345" or "bytes=123-456"
        /// </summary>
        public static void ExtractHttpRanges(this string rangeHeader, long contentLength, out long rangeStart, out long rangeEnd)
        {
            if (string.IsNullOrEmpty(rangeHeader) || rangeHeader.LeftPart('=') != "bytes" 
                                                  || rangeHeader.IndexOf('-') == -1)
                throw new HttpError(HttpStatusCode.RequestedRangeNotSatisfiable, $"Unsupported HTTP Range '{rangeHeader}'");
            
            if (rangeHeader.IndexOf(',') >= 0)
                throw new HttpError(HttpStatusCode.RequestedRangeNotSatisfiable, $"Multiple HTTP Ranges in '{rangeHeader}' is not supported");

            var bytesRange = rangeHeader.RightPart("=");
            var rangeStartStr = bytesRange.LeftPart("-");
            var rangeEndStr = bytesRange.RightPart("-");
            if (string.IsNullOrEmpty(rangeStartStr))
            {
                var suffixLength = long.Parse(rangeEndStr);
                rangeEnd = contentLength - 1;
                rangeStart = rangeEnd - suffixLength;
            }
            else
            {
                rangeStart = long.Parse(rangeStartStr);
                rangeEnd = !string.IsNullOrEmpty(rangeEndStr)
                    ? long.Parse(rangeEndStr) //the client requested a chunk
                    : contentLength - 1;
            }

            if (rangeStart < 0)
                rangeStart = 0;
            if (rangeEnd > contentLength - 1)
                rangeEnd = contentLength - 1;
        }

        /// <summary>
        /// Adds 206 PartialContent Status, Content-Range and Content-Length headers
        /// </summary>
        public static void AddHttpRangeResponseHeaders(this IResponse response, long rangeStart, long rangeEnd, long contentLength)
        {
            response.AddHeader(HttpHeaders.ContentRange, $"bytes {rangeStart}-{rangeEnd}/{contentLength}");
            response.StatusCode = (int)HttpStatusCode.PartialContent;
            response.SetContentLength(rangeEnd - rangeStart + 1);
        }

    }
}