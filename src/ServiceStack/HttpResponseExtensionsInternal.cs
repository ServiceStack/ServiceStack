//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Collections.Generic;
using ServiceStack.Formats;
using ServiceStack.Host;
using ServiceStack.Logging;
using ServiceStack.MiniProfiler;
using ServiceStack.Web;

namespace ServiceStack
{
    public static class HttpResponseExtensionsInternal
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(HttpResponseExtensionsInternal));

        private static readonly Task<bool> TrueTask;
        private static readonly Task<bool> FalseTask;
        private static readonly Task<object> EmptyTask;

        static HttpResponseExtensionsInternal()
        {
            EmptyTask = ((object)null).AsTaskResult();
            TrueTask = true.AsTaskResult();
            FalseTask = false.AsTaskResult();
        }

        public static bool WriteToOutputStream(IResponse response, object result, byte[] bodyPrefix, byte[] bodySuffix)
        {
            var partialResult = result as IPartialWriter;
            if (HostContext.Config.AllowPartialResponses && partialResult != null && partialResult.IsPartialRequest)
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
                var bodyPadding = bodyPrefix != null ? bodyPrefix.Length : 0;
                if (bodySuffix != null)
                    bodyPadding += bodySuffix.Length;

                response.ContentType = MimeTypes.Binary;
                response.SetContentLength(bytes.LongLength + bodyPadding);

                if (bodyPrefix != null) response.OutputStream.Write(bodyPrefix, 0, bodyPrefix.Length);
                response.OutputStream.Write(bytes, 0, bytes.Length);
                if (bodySuffix != null) response.OutputStream.Write(bodySuffix, 0, bodySuffix.Length);
                return true;
            }

            return false;
        }

        public static Task<bool> WriteToResponse(this IResponse httpRes, object result, string contentType)
        {
            var serializer = HostContext.ContentTypes.GetResponseSerializer(contentType); 
            return httpRes.WriteToResponse(result, serializer, new BasicRequest { ContentType = contentType });
        }

        public static Task<bool> WriteToResponse(this IResponse httpRes, IRequest httpReq, object result)
        {
            return WriteToResponse(httpRes, httpReq, result, null, null);
        }

        public static Task<bool> WriteToResponse(this IResponse httpRes, IRequest httpReq, object result, byte[] bodyPrefix, byte[] bodySuffix)
        {
            if (result == null)
            {
                httpRes.EndRequestWithNoContent();
                return TrueTask;
            }
            
            var httpResult = result as IHttpResult;
            if (httpResult != null)
            {
                if (httpResult.ResponseFilter == null)
                {
                    httpResult.ResponseFilter = HostContext.ContentTypes;
                }
                httpResult.RequestContext = httpReq;
                httpReq.ResponseContentType = httpResult.ContentType ?? httpReq.ResponseContentType;
                var httpResSerializer = httpResult.ResponseFilter.GetResponseSerializer(httpReq.ResponseContentType);
                return httpRes.WriteToResponse(httpResult, httpResSerializer, httpReq, bodyPrefix, bodySuffix);
            }

            var serializer = HostContext.ContentTypes.GetResponseSerializer(httpReq.ResponseContentType);
            return httpRes.WriteToResponse(result, serializer, httpReq, bodyPrefix, bodySuffix);
        }

        public static Task<bool> WriteToResponse(this IResponse httpRes, object result, ResponseSerializerDelegate serializer, IRequest serializationContext)
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
        /// <param name="request">The serialization context.</param>
        /// <param name="bodyPrefix">Add prefix to response body if any</param>
        /// <param name="bodySuffix">Add suffix to response body if any</param>
        /// <returns></returns>
        public static Task<bool> WriteToResponse(this IResponse response, object result, ResponseSerializerDelegate defaultAction, IRequest request, byte[] bodyPrefix, byte[] bodySuffix)
        {
            using (Profiler.Current.Step("Writing to Response"))
            {
                var defaultContentType = request.ResponseContentType;
                try
                {
                    if (result == null)
                    {
                        response.EndRequestWithNoContent();
                        return TrueTask;
                    }

                    ApplyGlobalResponseHeaders(response);

                    var httpResult = result as IHttpResult;
                    if (httpResult != null)
                    {
                        if (httpResult.RequestContext == null)
                        {
                            httpResult.RequestContext = request;
                        }

                        var paddingLength = bodyPrefix != null ? bodyPrefix.Length : 0;
                        if (bodySuffix != null)
                            paddingLength += bodySuffix.Length;

                        httpResult.PaddingLength = paddingLength;

                        var httpError = httpResult as IHttpError;
                        if (httpError != null)
                        {
                            response.Dto = httpError.CreateErrorResponse();
                            if (response.HandleCustomErrorHandler(request,
                                defaultContentType, httpError.Status, response.Dto))
                            {
                                return TrueTask;
                            }
                        }

                        response.Dto = response.Dto ?? httpResult.GetDto();

                        response.StatusCode = httpResult.Status;
                        response.StatusDescription = (httpResult.StatusDescription ?? httpResult.StatusCode.ToString()).Localize(request);
                        if (string.IsNullOrEmpty(httpResult.ContentType))
                        {
                            httpResult.ContentType = defaultContentType;
                        }
                        response.ContentType = httpResult.ContentType;
                    }
                    else
                    {
                        response.Dto = result;
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
                            if (responseHeaders.Key == HttpHeaders.ContentLength)
                            {
                                response.SetContentLength(long.Parse(responseHeaders.Value));
                                continue;
                            }

                            Log.DebugFormat("Setting Custom HTTP Header: {0}: {1}", responseHeaders.Key, responseHeaders.Value);
                            response.AddHeader(responseHeaders.Key, responseHeaders.Value);
                        }
                    }

                    //ContentType='text/html' is the default for a HttpResponse
                    //Do not override if another has been set
                    if (response.ContentType == null || response.ContentType == MimeTypes.Html)
                    {
                        response.ContentType = defaultContentType;
                    }
                    if (bodyPrefix != null && response.ContentType.IndexOf(MimeTypes.Json, StringComparison.InvariantCultureIgnoreCase) >= 0)
                    {
                        response.ContentType = MimeTypes.JavaScript;
                    }

                    if (HostContext.Config.AppendUtf8CharsetOnContentTypes.Contains(response.ContentType))
                    {
                        response.ContentType += ContentFormat.Utf8Suffix;
                    }

                    var disposableResult = result as IDisposable;
                    if (WriteToOutputStream(response, result, bodyPrefix, bodySuffix))
                    {
                        response.Flush(); //required for Compression
                        if (disposableResult != null) disposableResult.Dispose();
                        return TrueTask;
                    }

                    if (httpResult != null)
                    {
                        result = httpResult.Response;
                    }

                    var responseText = result as string;
                    if (responseText != null)
                    {                        
                        if (bodyPrefix != null) response.OutputStream.Write(bodyPrefix, 0, bodyPrefix.Length);
                        WriteTextToResponse(response, responseText, defaultContentType);
                        if (bodySuffix != null) response.OutputStream.Write(bodySuffix, 0, bodySuffix.Length);
                        return TrueTask;
                    }

                    if (defaultAction == null)
                    {
                        throw new ArgumentNullException("defaultAction", String.Format(
                        "As result '{0}' is not a supported responseType, a defaultAction must be supplied",
                        (result != null ? result.GetType().GetOperationName() : "")));
                    }

                    if (bodyPrefix != null) response.OutputStream.Write(bodyPrefix, 0, bodyPrefix.Length);
                    if (result != null) defaultAction(request, result, response);
                    if (bodySuffix != null) response.OutputStream.Write(bodySuffix, 0, bodySuffix.Length);

                    if (disposableResult != null) disposableResult.Dispose();

                    return FalseTask;
                }
                catch (Exception originalEx)
                {
                    HostContext.RaiseUncaughtException(request, response, request.OperationName, originalEx);

                    if (!HostContext.Config.WriteErrorsToResponse) 
                        return originalEx.AsTaskException<bool>();

                    var errorMessage = String.Format(
                    "Error occured while Processing Request: [{0}] {1}", originalEx.GetType().GetOperationName(), originalEx.Message);

                    try
                    {
                        if (!response.IsClosed)
                        {
                            response.WriteErrorToResponse(
                                request,
                                defaultContentType,
                                request.OperationName,
                                errorMessage,
                                originalEx,
                                (int)HttpStatusCode.InternalServerError);
                        }
                    }
                    catch (Exception writeErrorEx)
                    {
                        //Exception in writing to response should not hide the original exception
                        Log.Info("Failed to write error to response: {0}", writeErrorEx);
                        return originalEx.AsTaskException<bool>();
                    }
                    return TrueTask;
                }
                finally
                {
                    response.EndRequest(skipHeaders: true);
                }
            }
        }

        public static void WriteTextToResponse(this IResponse response, string text, string defaultContentType)
        {
            try
            {
                //ContentType='text/html' is the default for a HttpResponse
                //Do not override if another has been set
                if (response.ContentType == null || response.ContentType == MimeTypes.Html)
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

        public static void WriteError(this IResponse httpRes, IRequest httpReq, object dto, string errorMessage)
        {
            httpRes.WriteErrorToResponse(httpReq, httpReq.ResponseContentType, dto.GetType().Name, errorMessage, null,
                (int)HttpStatusCode.InternalServerError);
        }

        public static Task WriteErrorToResponse(this IResponse httpRes, IRequest httpReq,
            string contentType, string operationName, string errorMessage, Exception ex, int statusCode)
        {
            var errorDto = ex.ToErrorResponse();
            if (HandleCustomErrorHandler(httpRes, httpReq, contentType, statusCode, errorDto)) 
                return EmptyTask;

            if (httpRes.ContentType == null || httpRes.ContentType == MimeTypes.Html)
            {
                httpRes.ContentType = contentType;
            }
            if (HostContext.Config.AppendUtf8CharsetOnContentTypes.Contains(contentType))
            {
                httpRes.ContentType += ContentFormat.Utf8Suffix;
            }

            if (errorMessage != null && (httpRes.StatusDescription == null || httpRes.StatusDescription == "OK"))
                httpRes.StatusDescription = errorMessage;

            httpRes.StatusCode = statusCode;

            var serializer = HostContext.ContentTypes.GetResponseSerializer(contentType);
            if (serializer != null)
            {
                serializer(httpReq, errorDto, httpRes);
            }
            
            httpRes.EndHttpHandlerRequest(skipHeaders: true);

            return EmptyTask;
        }

        private static bool HandleCustomErrorHandler(this IResponse httpRes, IRequest httpReq,
            string contentType, int statusCode, object errorDto)
        {
            if (httpReq != null && MimeTypes.Html.MatchesContentType(contentType))
            {
                var errorHandler = HostContext.AppHost.GetCustomErrorHandler(statusCode);
                if (errorHandler != null)
                {
                    httpReq.Items["Model"] = errorDto;
                    httpReq.Items[HtmlFormat.ErrorStatusKey] = errorDto.GetResponseStatus();
                    errorHandler.ProcessRequest(httpReq, httpRes, httpReq.OperationName);
                    return true;
                }
            }
            return false;
        }

        private static ErrorResponse ToErrorResponse(this Exception ex)
        {
            List<ResponseError> errors = null;

            // For some exception types, we'll need to extract additional information in debug mode
            // (for example, so people can fix errors in their pages).
            if(HostContext.DebugMode)
            {
                var compileEx = ex as HttpCompileException;
                if (compileEx != null && compileEx.Results.Errors.HasErrors)
                {
                    errors = new List<ResponseError>();
                    foreach (var err in compileEx.Results.Errors)
                    {
                        errors.Add(new ResponseError { Message = err.ToString() });
                    }
                }
            }

            var dto = new ErrorResponse {
                ResponseStatus = new ResponseStatus {
                    ErrorCode = ex.ToErrorCode(),
                    Message = ex.Message,
                    StackTrace = HostContext.DebugMode ? ex.StackTrace : null,
                    Errors = errors
                }
            };
            return dto;
        }

        public static bool ShouldWriteGlobalHeaders(IResponse httpRes)
        {
            if (HostContext.Config != null && !httpRes.Items.ContainsKey("__global_headers"))
            {
                httpRes.Items["__global_headers"] = true;
                return true;
            }
            return false;
        }

        public static void ApplyGlobalResponseHeaders(this HttpListenerResponse httpRes)
        {
            if (HostContext.Config == null) return;
            foreach (var globalResponseHeader in HostContext.Config.GlobalResponseHeaders)
            {
                httpRes.AddHeader(globalResponseHeader.Key, globalResponseHeader.Value);
            }
        }

        public static void ApplyGlobalResponseHeaders(this HttpResponseBase httpRes)
        {
            if (HostContext.Config == null) return;
            foreach (var globalResponseHeader in HostContext.Config.GlobalResponseHeaders)
            {
                httpRes.AddHeader(globalResponseHeader.Key, globalResponseHeader.Value);
            }
        }

        public static void ApplyGlobalResponseHeaders(this IResponse httpRes)
        {
            if (!ShouldWriteGlobalHeaders(httpRes)) return;
            foreach (var globalResponseHeader in HostContext.Config.GlobalResponseHeaders)
            {
                httpRes.AddHeader(globalResponseHeader.Key, globalResponseHeader.Value);
            }
        }

    }
}
