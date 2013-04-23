using System;
using System.IO;
using System.Net;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.MiniProfiler;
using ServiceStack.Service;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Extensions
{
    public static class HttpResponseExtensions
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(HttpResponseExtensions));

        public static bool WriteToOutputStream(IHttpResponse response, object result, byte[] bodyPrefix, byte[] bodySuffix)
        {
            var partialResult = result as IPartialWriter;
            if (partialResult != null && EndpointHost.Config.AllowPartialResponses && partialResult.IsPartialRequest)
            {
                partialResult.WritePartialTo(response);
                return true;
            }

            var streamWriter = result as IStreamWriter;
            if (streamWriter != null)
            {
                if (bodyPrefix != null) response.OutputStream.Write(bodyPrefix, 0, bodyPrefix.Length);
                streamWriter.WriteTo(response.OutputStream);
                if (bodySuffix != null) response.OutputStream.Write(bodySuffix, 0, bodySuffix.Length);
                return true;
            }

            var stream = result as Stream;
            if (stream != null)
            {
                if (bodyPrefix != null) response.OutputStream.Write(bodyPrefix, 0, bodyPrefix.Length);
                stream.WriteTo(response.OutputStream);
                if (bodySuffix != null) response.OutputStream.Write(bodySuffix, 0, bodySuffix.Length);
                return true;
            }

            var bytes = result as byte[];
            if (bytes != null)
            {
                response.ContentType = ContentType.Binary;
                if (bodyPrefix != null) response.OutputStream.Write(bodyPrefix, 0, bodyPrefix.Length);
                response.OutputStream.Write(bytes, 0, bytes.Length);
                if (bodySuffix != null) response.OutputStream.Write(bodySuffix, 0, bodySuffix.Length);
                return true;
            }

            return false;
        }

        public static bool WriteToResponse(this IHttpResponse httpRes, object result, string contentType)
        {
            var serializer = EndpointHost.AppHost.ContentTypeFilters.GetResponseSerializer(contentType);
            return httpRes.WriteToResponse(result, serializer, new SerializationContext(contentType));
        }

        public static bool WriteToResponse(this IHttpResponse httpRes, IHttpRequest httpReq, object result)
        {
            return WriteToResponse(httpRes, httpReq, result, null, null);
        }

        public static bool WriteToResponse(this IHttpResponse httpRes, IHttpRequest httpReq, object result, byte[] bodyPrefix, byte[] bodySuffix)
        {
            if (result == null)
            {
                httpRes.EndHttpRequestWithNoContent();
                return true;
            }

            var serializationContext = new HttpRequestContext(httpReq, httpRes, result);
            var httpResult = result as IHttpResult;
            if (httpResult != null)
            {
                if (httpResult.ResponseFilter == null)
                {
                    httpResult.ResponseFilter = EndpointHost.AppHost.ContentTypeFilters;
                }
                httpResult.RequestContext = serializationContext;
                serializationContext.ResponseContentType = httpResult.ContentType ?? httpReq.ResponseContentType;
                var httpResSerializer = httpResult.ResponseFilter.GetResponseSerializer(serializationContext.ResponseContentType);
                return httpRes.WriteToResponse(httpResult, httpResSerializer, serializationContext, bodyPrefix, bodySuffix);
            }

            var serializer = EndpointHost.AppHost.ContentTypeFilters.GetResponseSerializer(httpReq.ResponseContentType);
            return httpRes.WriteToResponse(result, serializer, serializationContext, bodyPrefix, bodySuffix);
        }

        public static bool WriteToResponse(this IHttpResponse httpRes, object result, ResponseSerializerDelegate serializer, IRequestContext serializationContext)
        {
            return httpRes.WriteToResponse(result, serializer, serializationContext, null, null);
        }

        /// <summary>
        /// Writes to response.
        /// Response headers are customizable by implementing IHasOptions an returning Dictionary of Http headers.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="result">Whether or not it was implicity handled by ServiceStack's built-in handlers.</param>
        /// <param name="defaultAction">The default action.</param>
        /// <param name="serializerCtx">The serialization context.</param>
        /// <param name="bodyPrefix">Add prefix to response body if any</param>
        /// <param name="bodySuffix">Add suffix to response body if any</param>
        /// <returns></returns>
        public static bool WriteToResponse(this IHttpResponse response, object result, ResponseSerializerDelegate defaultAction, IRequestContext serializerCtx, byte[] bodyPrefix, byte[] bodySuffix)
        {
            using (Profiler.Current.Step("Writing to Response"))
            {
                var defaultContentType = serializerCtx.ResponseContentType;
                try
                {
                    if (result == null)
                    {
                        response.EndHttpRequestWithNoContent();
                        return true;
                    }

                    ApplyGlobalResponseHeaders(response);

                    var httpResult = result as IHttpResult;
                    if (httpResult != null)
                    {
                        if (httpResult.RequestContext == null)
                        {
                            httpResult.RequestContext = serializerCtx;
                        }

                        var httpError = httpResult as IHttpError;
                        if (httpError != null)
                        {
                            if (response.HandleCustomErrorHandler(serializerCtx.Get<IHttpRequest>(),
                                defaultContentType, httpError.Status, httpError.ToErrorResponse()))
                            {
                                return true;
                            }
                        }

                        response.StatusCode = httpResult.Status;
                        response.StatusDescription = httpResult.StatusDescription ?? httpResult.StatusCode.ToString();
                        if (string.IsNullOrEmpty(httpResult.ContentType))
                        {
                            httpResult.ContentType = defaultContentType;
                        }
                        response.ContentType = httpResult.ContentType;
                    }

                    /* Mono Error: Exception: Method not found: 'System.Web.HttpResponse.get_Headers' */
                    var responseOptions = result as IHasOptions;
                    if (responseOptions != null)
                    {
                        //Reserving options with keys in the format 'xx.xxx' (No Http headers contain a '.' so its a safe restriction)
                        const string reservedOptions = ".";

                        foreach (var responseHeaders in responseOptions.Options)
                        {
                            if (responseHeaders.Key.Contains(reservedOptions)) continue;

                            Log.DebugFormat("Setting Custom HTTP Header: {0}: {1}", responseHeaders.Key, responseHeaders.Value);
                            response.AddHeader(responseHeaders.Key, responseHeaders.Value);
                        }
                    }

                    var disposableResult = result as IDisposable;
                    if (WriteToOutputStream(response, result, bodyPrefix, bodySuffix))
                    {
                        response.Flush(); //required for Compression
                        if (disposableResult != null) disposableResult.Dispose();
                        return true;
                    }

                    if (httpResult != null)
                    {
                        result = httpResult.Response;
                    }

                    //ContentType='text/html' is the default for a HttpResponse
                    //Do not override if another has been set
                    if (response.ContentType == null || response.ContentType == ContentType.Html)
                    {
                        response.ContentType = defaultContentType;
                    }
                    if (bodyPrefix != null && response.ContentType.IndexOf(ContentType.Json, StringComparison.InvariantCultureIgnoreCase) >= 0)
                    {
                        response.ContentType = ContentType.JavaScript;
                    }

                    if (EndpointHost.Config.AppendUtf8CharsetOnContentTypes.Contains(response.ContentType))
                    {
                        response.ContentType += ContentType.Utf8Suffix;
                    }

                    var responseText = result as string;
                    if (responseText != null)
                    {
                        if (bodyPrefix != null) response.OutputStream.Write(bodyPrefix, 0, bodyPrefix.Length);
                        WriteTextToResponse(response, responseText, defaultContentType);
                        if (bodySuffix != null) response.OutputStream.Write(bodySuffix, 0, bodySuffix.Length);
                        return true;
                    }

                    if (defaultAction == null)
                    {
                        throw new ArgumentNullException("defaultAction", String.Format(
                        "As result '{0}' is not a supported responseType, a defaultAction must be supplied",
                        (result != null ? result.GetType().Name : "")));
                    }

                    if (bodyPrefix != null) response.OutputStream.Write(bodyPrefix, 0, bodyPrefix.Length);
                    if (result != null) defaultAction(serializerCtx, result, response);
                    if (bodySuffix != null) response.OutputStream.Write(bodySuffix, 0, bodySuffix.Length);

                    if (disposableResult != null) disposableResult.Dispose();

                    return false;
                }
                catch (Exception originalEx)
                {
                    //TM: It would be good to handle 'remote end dropped connection' problems here. Arguably they should at least be suppressible via configuration

                    //DB: Using standard ServiceStack configuration method
                    if (!EndpointHost.Config.WriteErrorsToResponse) throw;

                    var errorMessage = String.Format(
                    "Error occured while Processing Request: [{0}] {1}", originalEx.GetType().Name, originalEx.Message);

                    Log.Error(errorMessage, originalEx);

                    var operationName = result != null
                        ? result.GetType().Name.Replace("Response", "")
                        : "OperationName";

                    try
                    {
                        if (!response.IsClosed)
                        {
                            response.WriteErrorToResponse(
                                serializerCtx.Get<IHttpRequest>(),
                                defaultContentType,
                                operationName,
                                errorMessage,
                                originalEx,
                                (int)HttpStatusCode.InternalServerError);
                        }
                    }
                    catch (Exception writeErrorEx)
                    {
                        //Exception in writing to response should not hide the original exception
                        Log.Info("Failed to write error to response: {0}", writeErrorEx);
                        throw originalEx;
                    }
                    return true;
                }
                finally
                {
                    response.EndServiceStackRequest(skipHeaders: true);
                }
            }
        }

        public static void WriteTextToResponse(this IHttpResponse response, string text, string defaultContentType)
        {
            try
            {
                //ContentType='text/html' is the default for a HttpResponse
                //Do not override if another has been set
                if (response.ContentType == null || response.ContentType == ContentType.Html)
                {
                    response.ContentType = defaultContentType;
                }

                response.Write(text);
            }
            catch (Exception ex)
            {
                Log.Error("Could not WriteTextToResponse: " + ex.Message, ex);
                throw;
            }
        }

        public static void WriteError(this IHttpResponse httpRes, IHttpRequest httpReq, object dto, string errorMessage)
        {
            httpRes.WriteErrorToResponse(httpReq, httpReq.ResponseContentType, dto.GetType().Name, errorMessage, null,
                (int)HttpStatusCode.InternalServerError);
        }

        public static void WriteErrorToResponse(this IHttpResponse httpRes, IHttpRequest httpReq,
            string contentType, string operationName, string errorMessage, Exception ex, int statusCode)
        {
            var errorDto = ex.ToErrorResponse();
            if (HandleCustomErrorHandler(httpRes, httpReq, contentType, statusCode, errorDto)) return;

            if (httpRes.ContentType == null || httpRes.ContentType == ContentType.Html)
            {
                httpRes.ContentType = contentType;
            }
            if (EndpointHost.Config.AppendUtf8CharsetOnContentTypes.Contains(contentType))
            {
                httpRes.ContentType += ContentType.Utf8Suffix;
            }

            httpRes.StatusCode = statusCode;
            var serializationCtx = new SerializationContext(contentType);

            var serializer = EndpointHost.AppHost.ContentTypeFilters.GetResponseSerializer(contentType);
            if (serializer != null)
            {
                serializer(serializationCtx, errorDto, httpRes);
            }
            
            httpRes.EndHttpRequest(skipHeaders: true);
        }

        private static bool HandleCustomErrorHandler(this IHttpResponse httpRes, IHttpRequest httpReq,
            string contentType, int statusCode, object errorDto)
        {
            if (httpReq != null && ContentType.Html.MatchesContentType(contentType))
            {
                var errorHandler = EndpointHost.Config.GetCustomErrorHandler(statusCode);
                if (errorHandler != null)
                {
                    httpReq.Items["Model"] = errorDto;
                    errorHandler.ProcessRequest(httpReq, httpRes, httpReq.OperationName);
                    return true;
                }
            }
            return false;
        }

        private static ErrorResponse ToErrorResponse(this Exception ex)
        {
            var dto = new ErrorResponse {
                ResponseStatus = new ResponseStatus {
                    ErrorCode = ex.ToErrorCode(),
                    Message = ex.Message,
                    StackTrace = EndpointHost.DebugMode ? ex.StackTrace : null,
                }
            };
            return dto;
        }

        public static void ApplyGlobalResponseHeaders(this HttpListenerResponse httpRes)
        {
            if (EndpointHost.Config == null) return;
            foreach (var globalResponseHeader in EndpointHost.Config.GlobalResponseHeaders)
            {
                httpRes.AddHeader(globalResponseHeader.Key, globalResponseHeader.Value);
            }
        }

        public static void ApplyGlobalResponseHeaders(this HttpResponse httpRes)
        {
            if (EndpointHost.Config == null) return;
            foreach (var globalResponseHeader in EndpointHost.Config.GlobalResponseHeaders)
            {
                httpRes.AddHeader(globalResponseHeader.Key, globalResponseHeader.Value);
            }
        }

        public static void ApplyGlobalResponseHeaders(this IHttpResponse httpRes)
        {
            if (EndpointHost.Config == null) return;
            foreach (var globalResponseHeader in EndpointHost.Config.GlobalResponseHeaders)
            {
                httpRes.AddHeader(globalResponseHeader.Key, globalResponseHeader.Value);
            }
        }

        public static void EndServiceStackRequest(this HttpResponse httpRes, bool skipHeaders = false)
        {
            if (!skipHeaders) httpRes.ApplyGlobalResponseHeaders();
            httpRes.Close();
            EndpointHost.CompleteRequest();
        }

        public static void EndServiceStackRequest(this IHttpResponse httpRes, bool skipHeaders = false)
        {
            httpRes.EndHttpRequest(skipHeaders: skipHeaders);
            EndpointHost.CompleteRequest();
        }

        public static void EndHttpRequest(this HttpResponse httpRes, bool skipHeaders = false, bool skipClose = false, bool closeOutputStream = false, Action<HttpResponse> afterBody = null)
        {
            if (!skipHeaders) httpRes.ApplyGlobalResponseHeaders();
            if (afterBody != null) afterBody(httpRes);
            if (closeOutputStream) httpRes.CloseOutputStream();
            else if (!skipClose) httpRes.Close();

            //skipHeaders used when Apache+mod_mono doesn't like:
            //response.OutputStream.Flush();
            //response.Close();
        }

        public static void EndHttpRequest(this IHttpResponse httpRes, bool skipHeaders = false, bool skipClose = false, Action<IHttpResponse> afterBody = null)
        {
            if (!skipHeaders) httpRes.ApplyGlobalResponseHeaders();
            if (afterBody != null) afterBody(httpRes);
            if (!skipClose) httpRes.Close();

            //skipHeaders used when Apache+mod_mono doesn't like:
            //response.OutputStream.Flush();
            //response.Close();
        }

        public static void EndHttpRequestWithNoContent(this IHttpResponse httpRes)
        {
            if (EndpointHost.Config == null || EndpointHost.Config.Return204NoContentForEmptyResponse)
            {
                if (httpRes.StatusCode == (int)HttpStatusCode.OK)
                {
                    httpRes.StatusCode = (int)HttpStatusCode.NoContent;
                }
            }

            httpRes.SetContentLength(0);
            httpRes.EndServiceStackRequest();
        }

    }
}
