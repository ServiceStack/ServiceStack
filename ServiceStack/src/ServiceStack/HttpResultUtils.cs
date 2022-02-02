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
            return response != null && (response is IHttpError || response is Exception || response is ErrorResponse  );
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


        /// <summary>
        /// Writes partial range as specified by start-end, from fromStream to toStream.
        /// </summary>
        [Obsolete("Use WritePartialToAsync")]
        public static void WritePartialTo(this Stream fromStream, Stream toStream, long start, long end)
        {
            if (!fromStream.CanSeek)
                throw new InvalidOperationException(
                    "Sending Range Responses requires a seekable stream eg. FileStream or MemoryStream");

            long totalBytesToSend = end - start + 1;

            var buf = SharedPools.AsyncByteArray.Allocate();

            long bytesRemaining = totalBytesToSend;

            fromStream.Seek(start, SeekOrigin.Begin);
            while (bytesRemaining > 0)
            {
                var count = bytesRemaining <= buf.Length
                    ? fromStream.Read(buf, 0, (int)Math.Min(bytesRemaining, int.MaxValue))
                    : fromStream.Read(buf, 0, buf.Length);

                try
                {
                    //Log.DebugFormat("Writing {0} to response",System.Text.Encoding.UTF8.GetString(buffer));
                    toStream.Write(buf, 0, count);
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
            
            SharedPools.AsyncByteArray.Free(buf);
        }
        
        /// <summary>
        /// Writes partial range as specified by start-end, from fromStream to toStream.
        /// </summary>
        public static async Task WritePartialToAsync(this Stream fromStream, Stream toStream, long start, long end, CancellationToken token = default(CancellationToken))
        {
            if (!fromStream.CanSeek)
                throw new InvalidOperationException(
                    "Sending Range Responses requires a seekable stream eg. FileStream or MemoryStream");

            long totalBytesToSend = end - start + 1;

            var buf = SharedPools.AsyncByteArray.Allocate();

            long bytesRemaining = totalBytesToSend;

            fromStream.Seek(start, SeekOrigin.Begin);
            while (bytesRemaining > 0)
            {
                try
                {
                    var count = bytesRemaining <= buf.Length
                        ? await fromStream.ReadAsync(buf, 0, (int)Math.Min(bytesRemaining, int.MaxValue), token).ConfigAwait()
                        : await fromStream.ReadAsync(buf, 0, buf.Length, token).ConfigAwait();

                    //Log.DebugFormat("Writing {0} to response",System.Text.Encoding.UTF8.GetString(buffer));
                    await toStream.WriteAsync(buf, 0, count, token).ConfigAwait();
                    await toStream.FlushAsync(token).ConfigAwait();
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
            
            SharedPools.AsyncByteArray.Free(buf);
        }

    }
}