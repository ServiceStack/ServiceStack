using System;
using System.IO;
using System.Net;
using ServiceStack.Text;
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
            var httpResult = response as IHttpResult;
            return httpResult != null ? httpResult.Response : response;
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
            var httpResult = response as IHttpResult;
            return (httpResult != null ? httpResult.Response : response) as TResponse;
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

            return new ErrorResponse {
                ResponseStatus = new ResponseStatus {
                    ErrorCode = httpError.ErrorCode,
                    Message = httpError.Message,
                    StackTrace = httpError.StackTrace,
                }
            };
        }

        /// <summary>
        /// Whether the response is an IHttpError or Exception
        /// </summary>
        public static bool IsErrorResponse(this object response)
        {
            return response != null && (response is IHttpError || response is Exception);
        }

        /// <summary>
        /// rangeHeader should be of the format "bytes=0-" or "bytes=0-12345" or "bytes=123-456"
        /// </summary>
        public static void ExtractHttpRanges(this string rangeHeader, long contentLength, out long rangeStart, out long rangeEnd)
        {
            var rangeParts = rangeHeader.SplitOnFirst("=")[1].SplitOnFirst("-");
            rangeStart = Int64.Parse(rangeParts[0]);
            rangeEnd = rangeParts.Length == 2 && !String.IsNullOrEmpty(rangeParts[1])
                           ? Int32.Parse(rangeParts[1]) //the client requested a chunk
                           : contentLength - 1;
        }

        /// <summary>
        /// Adds 206 PartialContent Status, Content-Range and Content-Length headers
        /// </summary>
        public static void AddHttpRangeResponseHeaders(this IResponse response, long rangeStart, long rangeEnd, long contentLength)
        {
            response.AddHeader(HttpHeaders.ContentRange, "bytes {0}-{1}/{2}".Fmt(rangeStart, rangeEnd, contentLength));
            response.StatusCode = (int)HttpStatusCode.PartialContent;
            response.SetContentLength(rangeEnd - rangeStart + 1);
        }

        public static int PartialBufferSize = 32 * 1024;
        [ThreadStatic] private static byte[] PartialBuffer;

        /// <summary>
        /// Writes partial range as specified by start-end, from fromStream to toStream.
        /// </summary>
        public static void WritePartialTo(this Stream fromStream, Stream toStream, long start, long end)
        {
            if (!fromStream.CanSeek)
                throw new InvalidOperationException(
                    "Sending Range Responses requires a seekable stream eg. FileStream or MemoryStream");

            long totalBytesToSend = end - start + 1;

            if (PartialBuffer == null || PartialBufferSize != PartialBuffer.Length)
                PartialBuffer = new byte[PartialBufferSize];

            long bytesRemaining = totalBytesToSend;

            fromStream.Seek(start, SeekOrigin.Begin);
            while (bytesRemaining > 0)
            {
                var count = bytesRemaining <= PartialBuffer.Length
                    ? fromStream.Read(PartialBuffer, 0, (int)Math.Min(bytesRemaining, int.MaxValue))
                    : fromStream.Read(PartialBuffer, 0, PartialBuffer.Length);

                try
                {
                    //Log.DebugFormat("Writing {0} to response",System.Text.Encoding.UTF8.GetString(buffer));
                    toStream.Write(PartialBuffer, 0, count);
                    toStream.Flush();
                    bytesRemaining -= count;
                }
                catch (Exception httpException)
                {
                    /* in Asp.Net we can call HttpResponseBase.IsClientConnected
                        * to see if the client broke off the connection
                        * and avoid trying to flush the response stream.
                        * I'm not quite I can do the same here without some invasive changes,
                        * so instead I'll swallow the exception that IIS throws in this situation.*/

                    if (httpException.Message == "An error occurred while communicating with the remote host. The error code is 0x80070057.")
                        return;
                    
                    throw;
                }
            }
        }

    }
}