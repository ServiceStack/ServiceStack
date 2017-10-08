//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Collections.Generic;
using System.Threading;
using ServiceStack.Formats;
using ServiceStack.Host;
using ServiceStack.Logging;
using ServiceStack.MiniProfiler;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public static class HttpResponseExtensionsInternal
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(HttpResponseExtensionsInternal));

        [Obsolete("Use WriteToOutputStreamAsync")]
        public static bool WriteToOutputStream(IResponse response, object result, byte[] bodyPrefix, byte[] bodySuffix)
        {
            if (HostContext.Config.AllowPartialResponses && result is IPartialWriter partialResult && partialResult.IsPartialRequest)
            {
                partialResult.WritePartialTo(response);
                return true;
            }

            if (result is IStreamWriter streamWriter)
            {
                if (bodyPrefix != null) response.OutputStream.Write(bodyPrefix, 0, bodyPrefix.Length);
                streamWriter.WriteTo(response.OutputStream);
                if (bodySuffix != null) response.OutputStream.Write(bodySuffix, 0, bodySuffix.Length);
                return true;
            }

            return false;
        }

        public static async Task<bool> WriteToOutputStreamAsync(IResponse response, object result, byte[] bodyPrefix, byte[] bodySuffix, CancellationToken token=default(CancellationToken))
        {
            if (HostContext.Config.AllowPartialResponses && result is IPartialWriterAsync partialResult && partialResult.IsPartialRequest)
            {
                await partialResult.WritePartialToAsync(response, token);
                return true;
            }

            if (result is IStreamWriterAsync streamWriter)
            {
                if (bodyPrefix != null) await response.OutputStream.WriteAsync(bodyPrefix, token);
                await streamWriter.WriteToAsync(response.OutputStream, token);
                if (bodySuffix != null) await response.OutputStream.WriteAsync(bodySuffix, token);
                return true;
            }

            if (result is Stream stream)
            {
                if (bodyPrefix != null) await response.OutputStream.WriteAsync(bodyPrefix, token);
                await stream.CopyToAsync(response.OutputStream, token);
                if (bodySuffix != null) await response.OutputStream.WriteAsync(bodySuffix, token);
                return true;
            }

            if (result is byte[] bytes)
            {
                var len = (bodyPrefix?.Length).GetValueOrDefault() +
                          bytes.Length +
                          (bodySuffix?.Length).GetValueOrDefault();

                response.SetContentLength(len);
                response.ContentType = MimeTypes.Binary;

                if (bodyPrefix != null) await response.OutputStream.WriteAsync(bodyPrefix, token);
                await response.OutputStream.WriteAsync(bytes, token);
                if (bodySuffix != null) await response.OutputStream.WriteAsync(bodySuffix, token);
                return true;
            }

            return false;
        }

        public static Task<bool> WriteToResponse(this IResponse httpRes, object result, string contentType, CancellationToken token=default(CancellationToken))
        {
            var serializer = HostContext.ContentTypes.GetStreamSerializerAsync(contentType);
            return httpRes.WriteToResponse(result, serializer, new BasicRequest { ContentType = contentType }, token);
        }

        public static Task<bool> WriteToResponse(this IResponse httpRes, IRequest httpReq, object result, CancellationToken token = default(CancellationToken))
        {
            return WriteToResponse(httpRes, httpReq, result, null, null, token);
        }

        public static Task<bool> WriteToResponse(this IResponse httpRes, IRequest httpReq, object result, byte[] bodyPrefix, byte[] bodySuffix, CancellationToken token = default(CancellationToken))
        {
            if (result == null)
            {
                httpRes.EndRequestWithNoContent();
                return TypeConstants.TrueTask;
            }

            if (result is IHttpResult httpResult)
            {
                if (httpResult.ResponseFilter == null)
                {
                    httpResult.ResponseFilter = HostContext.ContentTypes;
                }
                httpResult.RequestContext = httpReq;
                httpReq.ResponseContentType = httpResult.ContentType ?? httpReq.ResponseContentType;
                var httpResSerializer = httpResult.ResponseFilter.GetStreamSerializerAsync(httpReq.ResponseContentType)
                    ?? httpResult.ResponseFilter.GetStreamSerializerAsync(HostContext.Config.DefaultContentType);
                return httpRes.WriteToResponse(httpResult, httpResSerializer, httpReq, bodyPrefix, bodySuffix, token);
            }

            var serializer = HostContext.ContentTypes.GetStreamSerializerAsync(httpReq.ResponseContentType);
            return httpRes.WriteToResponse(result, serializer, httpReq, bodyPrefix, bodySuffix, token);
        }

        public static Task<bool> WriteToResponse(this IResponse httpRes, object result, StreamSerializerDelegateAsync serializer, IRequest serializationContext, CancellationToken token = default(CancellationToken))
        {
            return httpRes.WriteToResponse(result, serializer, serializationContext, null, null, token);
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
        public static async Task<bool> WriteToResponse(this IResponse response, object result, StreamSerializerDelegateAsync defaultAction, IRequest request, byte[] bodyPrefix, byte[] bodySuffix, CancellationToken token = default(CancellationToken))
        {
            using (Profiler.Current.Step("Writing to Response"))
            {
                var defaultContentType = request.ResponseContentType;
                try
                {
                    if (result == null)
                    {
                        response.EndRequestWithNoContent();
                        return true;
                    }

                    ApplyGlobalResponseHeaders(response);

                    IDisposable resultScope = null;

                    var httpResult = result as IHttpResult;
                    if (httpResult != null)
                    {
                        if (httpResult.ResultScope != null)
                            resultScope = httpResult.ResultScope();

                        if (httpResult.RequestContext == null)
                            httpResult.RequestContext = request;

                        var paddingLength = bodyPrefix?.Length ?? 0;
                        if (bodySuffix != null)
                            paddingLength += bodySuffix.Length;

                        httpResult.PaddingLength = paddingLength;

                        if (httpResult is IHttpError httpError)
                        {
                            response.Dto = httpError.CreateErrorResponse();
                            if (response.HandleCustomErrorHandler(request,
                                defaultContentType, httpError.Status, response.Dto))
                            {
                                return true;
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

                        if (httpResult.Cookies != null)
                        {
                            foreach (var cookie in httpResult.Cookies)
                            {
                                response.SetCookie(cookie);
                            }
                        }
                    }
                    else
                    {
                        response.Dto = result;
                    }

                    /* Mono Error: Exception: Method not found: 'System.Web.HttpResponse.get_Headers' */
                    if (result is IHasOptions responseOptions)
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

                            if (responseHeaders.Key.EqualsIgnoreCase(HttpHeaders.ContentType))
                            {
                                response.ContentType = responseHeaders.Value;
                                continue;
                            }

                            if (Log.IsDebugEnabled)
                                Log.Debug($"Setting Custom HTTP Header: {responseHeaders.Key}: {responseHeaders.Value}");

                            response.AddHeader(responseHeaders.Key, responseHeaders.Value);
                        }
                    }

                    //ContentType='text/html' is the default for a HttpResponse
                    //Do not override if another has been set
                    if (response.ContentType == null || response.ContentType == MimeTypes.Html)
                    {
                        response.ContentType = defaultContentType;
                    }
                    if (bodyPrefix != null && response.ContentType.IndexOf(MimeTypes.Json, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        response.ContentType = MimeTypes.JavaScript;
                    }

                    if (HostContext.Config.AppendUtf8CharsetOnContentTypes.Contains(response.ContentType))
                    {
                        response.ContentType += ContentFormat.Utf8Suffix;
                    }

                    using (resultScope)
                    using (HostContext.Config.AllowJsConfig ? JsConfig.CreateScope(request.QueryString[Keywords.JsConfig]) : null)
                    {
                        var disposableResult = result as IDisposable;
                        if (WriteToOutputStream(response, result, bodyPrefix, bodySuffix))
                        {
                            response.Flush(); //required for Compression
                            disposableResult?.Dispose();
                            return true;
                        }

                        if (await WriteToOutputStreamAsync(response, result, bodyPrefix, bodySuffix, token))
                        {
                            await response.FlushAsync(token);
                            disposableResult?.Dispose();
                            return true;
                        }

                        if (httpResult != null)
                            result = httpResult.Response;

                        if (result is string responseText)
                        {
                            var strBytes = responseText.ToUtf8Bytes();
                            var len = (bodyPrefix?.Length).GetValueOrDefault() +
                                      strBytes.Length +
                                      (bodySuffix?.Length).GetValueOrDefault();

                            response.SetContentLength(len);
                            
                            if (response.ContentType == null || response.ContentType == MimeTypes.Html)
                                response.ContentType = defaultContentType;
                            
                            //retain behavior with ASP.NET's response.Write(string)
                            if (response.ContentType.IndexOf(';') == -1)
                                response.ContentType += ContentFormat.Utf8Suffix;

                            if (bodyPrefix != null) 
                                await response.OutputStream.WriteAsync(bodyPrefix, token);

                            await response.OutputStream.WriteAsync(strBytes, token);

                            if (bodySuffix != null) 
                                await response.OutputStream.WriteAsync(bodySuffix, token);

                            return true;
                        }

                        if (defaultAction == null)
                        {
                            throw new ArgumentNullException(nameof(defaultAction),
                                $"As result '{(result != null ? result.GetType().GetOperationName() : "")}' is not a supported responseType, a defaultAction must be supplied");
                        }

                        if (bodyPrefix != null)
                            await response.OutputStream.WriteAsync(bodyPrefix, token);

                        if (result != null)
                            await defaultAction(request, result, response.OutputStream);

                        if (bodySuffix != null)
                            await response.OutputStream.WriteAsync(bodySuffix, token);

                        disposableResult?.Dispose();
                    }

                    return false;
                }
                catch (Exception originalEx)
                {
                    return await HandleResponseWriteException(originalEx, request, response, defaultContentType);
                }
                finally
                {
                    await response.EndRequestAsync(skipHeaders: true);
                }
            }
        }

        internal static Task<bool> HandleResponseWriteException(this Exception originalEx, IRequest request, IResponse response, string defaultContentType)
        {
            HostContext.RaiseAndHandleUncaughtException(request, response, request.OperationName, originalEx);

            if (!HostContext.Config.WriteErrorsToResponse)
                return originalEx.AsTaskException<bool>();

            var errorMessage = $"Error occured while Processing Request: [{originalEx.GetType().GetOperationName()}] {originalEx.Message}";

            try
            {
                if (!response.IsClosed)
                {
                    return response.WriteErrorToResponse(
                        request,
                        defaultContentType ?? request.ResponseContentType,
                        request.OperationName,
                        errorMessage,
                        originalEx,
                        (int) HttpStatusCode.InternalServerError)
                    .ContinueWith(x => true);
                }
            }
            catch (Exception writeErrorEx)
            {
                //Exception in writing to response should not hide the original exception
                Log.Info("Failed to write error to response: {0}", writeErrorEx);
                return originalEx.AsTaskException<bool>();
            }
            return TypeConstants.TrueTask;
        }

        public static async Task WriteBytesToResponse(this IResponse res, byte[] responseBytes, string contentType, CancellationToken token = default(CancellationToken))
        {
            res.ContentType = HostContext.Config.AppendUtf8CharsetOnContentTypes.Contains(contentType)
                ? contentType + ContentFormat.Utf8Suffix
                : contentType;

            res.ApplyGlobalResponseHeaders();
            res.SetContentLength(responseBytes.Length);

            try
            {
                await res.OutputStream.WriteAsync(responseBytes, token);
                await res.FlushAsync(token);
            }
            catch (Exception ex)
            {
                await ex.HandleResponseWriteException(res.Request, res, contentType);
            }
            finally
            {
                res.EndRequest(skipHeaders: true);
            }
        }

        public static Task WriteError(this IResponse httpRes, IRequest httpReq, object dto, string errorMessage)
        {
            return httpRes.WriteErrorToResponse(httpReq, httpReq.ResponseContentType, dto.GetType().Name, errorMessage, null,
                (int)HttpStatusCode.InternalServerError);
        }

        public static Task WriteError(this IResponse httpRes, object dto, string errorMessage)
        {
            var httpReq = httpRes.Request;
            return httpRes.WriteErrorToResponse(httpReq, httpReq.ResponseContentType, dto.GetType().Name, errorMessage, null,
                (int)HttpStatusCode.InternalServerError);
        }

        public static Task WriteError(this IResponse httpRes, Exception ex, int statusCode = 500, string errorMessage = null, string contentType = null)
        {
            return httpRes.WriteErrorToResponse(httpRes.Request,
                contentType ?? httpRes.Request.ResponseContentType ?? HostContext.Config.DefaultContentType,
                httpRes.Request.OperationName,
                errorMessage,
                ex,
                statusCode);
        }

        /// <summary>
        /// When HTTP Headers have already been written and only the Body can be written
        /// </summary>
        public static Task WriteErrorBody(this IResponse httpRes, Exception ex)
        {
            var req = httpRes.Request;
            var errorDto = ex.ToErrorResponse();
            HostContext.AppHost.OnExceptionTypeFilter(ex, errorDto.ResponseStatus);
            var serializer = HostContext.ContentTypes.GetStreamSerializerAsync(MimeTypes.Html);
            serializer?.Invoke(req, errorDto, httpRes.OutputStream);
            httpRes.EndHttpHandlerRequest(skipHeaders: true);
            return TypeConstants.EmptyTask;
        }

        public static async Task WriteErrorToResponse(this IResponse httpRes, IRequest httpReq,
            string contentType, string operationName, string errorMessage, Exception ex, int statusCode)
        {
            if (ex == null)
                ex = new Exception(errorMessage);

            var errorDto = ex.ToErrorResponse();
            HostContext.AppHost.OnExceptionTypeFilter(ex, errorDto.ResponseStatus);

            if (HandleCustomErrorHandler(httpRes, httpReq, contentType, statusCode, errorDto))
                return;

            if ((httpRes.ContentType == null || httpRes.ContentType == MimeTypes.Html) 
                && contentType != null && contentType != httpRes.ContentType)
            {
                httpRes.ContentType = contentType;
            }
            if (HostContext.Config.AppendUtf8CharsetOnContentTypes.Contains(contentType))
            {
                httpRes.ContentType += ContentFormat.Utf8Suffix;
            }

            var hold = httpRes.StatusDescription;
            var hasDefaultStatusDescription = hold == null || hold == "OK";

            httpRes.StatusCode = statusCode;

            httpRes.StatusDescription = hasDefaultStatusDescription
                ? (errorMessage ?? HttpStatus.GetStatusDescription(statusCode))
                : hold;

            httpRes.ApplyGlobalResponseHeaders();

            var serializer = HostContext.ContentTypes.GetStreamSerializerAsync(contentType);
            if (serializer != null)
                await serializer(httpReq, errorDto, httpRes.OutputStream);

            httpRes.EndHttpHandlerRequest(skipHeaders: true);
        }

        private static bool HandleCustomErrorHandler(this IResponse httpRes, IRequest httpReq,
            string contentType, int statusCode, object errorDto)
        {
            if (httpReq != null && MimeTypes.Html.MatchesContentType(contentType))
            {
                var errorHandler = HostContext.AppHost.GetCustomErrorHandler(statusCode)
                    ?? HostContext.AppHost.GlobalHtmlErrorHttpHandler;
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
            if (HostContext.DebugMode)
            {
#if !NETSTANDARD2_0
                if (ex is HttpCompileException compileEx && compileEx.Results.Errors.HasErrors)
                {
                    errors = new List<ResponseError>();
                    foreach (var err in compileEx.Results.Errors)
                    {
                        errors.Add(new ResponseError { Message = err.ToString() });
                    }
                }
#endif
            }

            var dto = new ErrorResponse
            {
                ResponseStatus = new ResponseStatus
                {
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

#if !NETSTANDARD2_0
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
#endif

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
