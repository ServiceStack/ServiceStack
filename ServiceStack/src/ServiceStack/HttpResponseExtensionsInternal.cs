#pragma warning disable CS0618
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
using ServiceStack.Support;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack;

public static class HttpResponseExtensionsInternal
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(HttpResponseExtensionsInternal));

    [Obsolete("Use WriteToOutputStreamAsync")]
    public static bool WriteToOutputStream(IResponse response, object result, byte[] bodyPrefix, byte[] bodySuffix)
    {
        if (HostContext.Config.AllowPartialResponses && result is IPartialWriter { IsPartialRequest: true } partialResult)
        {
            response.AllowSyncIO();
            partialResult.WritePartialTo(response);
            return true;
        }

        if (result is IStreamWriter streamWriter)
        {
            response.AllowSyncIO();
            if (bodyPrefix != null) response.OutputStream.Write(bodyPrefix, 0, bodyPrefix.Length);
            streamWriter.WriteTo(response.OutputStream);
            if (bodySuffix != null) response.OutputStream.Write(bodySuffix, 0, bodySuffix.Length);
            return true;
        }

        return false;
    }

    public static async Task<bool> WriteToOutputStreamAsync(IResponse response, object result, byte[] bodyPrefix, byte[] bodySuffix, CancellationToken token=default(CancellationToken))
    {
        if (HostContext.Config.AllowPartialResponses && result is IPartialWriterAsync { IsPartialRequest: true } partialResult)
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
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }
            await stream.WriteToAsync(response.OutputStream, token);
            if (bodySuffix != null) await response.OutputStream.WriteAsync(bodySuffix, token);
            return true;
        }

        if (result is byte[] bytes)
        {
            var len = (bodyPrefix?.Length).GetValueOrDefault() +
                      bytes.Length +
                      (bodySuffix?.Length).GetValueOrDefault();

            response.SetContentLength(len);

            if (bodyPrefix != null) await response.OutputStream.WriteAsync(bodyPrefix, token);
            await response.OutputStream.WriteAsync(bytes, token);
            if (bodySuffix != null) await response.OutputStream.WriteAsync(bodySuffix, token);
            return true;
        }

        if (result is ReadOnlyMemory<byte> rom)
        {
            var len = (bodyPrefix?.Length).GetValueOrDefault() +
                      rom.Length +
                      (bodySuffix?.Length).GetValueOrDefault();
                
            if (bodyPrefix != null) await response.OutputStream.WriteAsync(bodyPrefix, token);
            await response.OutputStream.WriteAsync(rom, token);
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
    /// <param name="result">Whether or not it was implicitly handled by ServiceStack's built-in handlers.</param>
    /// <param name="defaultAction">The default action.</param>
    /// <param name="request">The serialization context.</param>
    /// <param name="bodyPrefix">Add prefix to response body if any</param>
    /// <param name="bodySuffix">Add suffix to response body if any</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public static async Task<bool> WriteToResponse(this IResponse response, object result, StreamSerializerDelegateAsync defaultAction, IRequest request, byte[] bodyPrefix, byte[] bodySuffix, CancellationToken token = default(CancellationToken))
    {
        using (Profiler.Current.Step("Writing to Response"))
        {
            var defaultContentType = request.ResponseContentType;
            var disposableResult = result as IDisposable; 
            bool flushAsync = false;

            try
            {
                if (result == null)
                {
                    response.EndRequestWithNoContent();
                    return true;
                }

                ApplyGlobalResponseHeaders(response);

                IDisposable resultScope = null;


                if (result is Exception)
                {
                    if (response.Request.Items.TryGetValue(Keywords.ErrorView, out var oErrorView))
                        response.Request.Items[Keywords.View] = oErrorView;
                }

                var httpResult = result as IHttpResult;
                if (httpResult != null)
                {
                    response.Items[Keywords.Result] = result;
                    if (httpResult.ResultScope != null)
                        resultScope = httpResult.ResultScope();

                    httpResult.RequestContext ??= request;

                    var paddingLength = bodyPrefix?.Length ?? 0;
                    if (bodySuffix != null)
                        paddingLength += bodySuffix.Length;

                    httpResult.PaddingLength = paddingLength;

                    if (httpResult is IHttpError httpError)
                    {
                        response.Dto = httpError.CreateErrorResponse();
                        if (await response.HandleCustomErrorHandler(request, defaultContentType, httpError.Status, response.Dto, httpError as Exception))
                        {
                            return true;
                        }
                    }

                    response.Dto ??= httpResult.GetDto();

                    if (!response.HasStarted)
                    {
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
                }
                else
                {
                    response.Dto = result;
                }

                var config = HostContext.Config;
                if (!response.HasStarted)
                {
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
                    if (response.ContentType is null or MimeTypes.Html)
                    {
                        response.ContentType = defaultContentType == (config.DefaultContentType ?? MimeTypes.Html) && result is byte[]
                            ? MimeTypes.Binary
                            : defaultContentType;
                    }
                    if (bodyPrefix != null && response.ContentType.IndexOf(MimeTypes.Json, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        response.ContentType = MimeTypes.JavaScript;
                    }

                    if (config.AppendUtf8CharsetOnContentTypes.Contains(response.ContentType))
                    {
                        response.ContentType += ContentFormat.Utf8Suffix;
                    }
                }

                var jsconfig = config.AllowJsConfig ? request.QueryString[Keywords.JsConfig] : null;
                using (resultScope)
                {
                    var jsScope = resultScope == null && jsconfig != null ? JsConfig.CreateScope(jsconfig) : null;
                    using (jsScope)
                    {
                        if (WriteToOutputStream(response, result, bodyPrefix, bodySuffix))
                        {
                            await response.FlushAsync(token); //required for Compression
                            return true;
                        }

#if NETFX || NET472
                        //JsConfigScope uses ThreadStatic in .NET v4.5 so avoid async thread hops by writing sync to MemoryStream
                        if (resultScope != null || jsconfig != null)
                            response.UseBufferedStream = true;
#endif

                        if (await WriteToOutputStreamAsync(response, result, bodyPrefix, bodySuffix, token))
                        {
                            flushAsync = true;
                            return true;
                        }

                        if (httpResult != null)
                            result = httpResult.Response;

                        ReadOnlyMemory<byte>? uf8Bytes = null;
                        if (result is string responseText)
                            uf8Bytes = MemoryProvider.Instance.ToUtf8(responseText.AsSpan());
                        else if (result is ReadOnlyMemory<char> rom)
                            uf8Bytes = MemoryProvider.Instance.ToUtf8(rom.Span);

                        if (uf8Bytes != null)
                        {
                            var len = (bodyPrefix?.Length).GetValueOrDefault() +
                                      uf8Bytes.Value.Length +
                                      (bodySuffix?.Length).GetValueOrDefault();

                            response.SetContentLength(len);
                                
                            if (response.ContentType is null or MimeTypes.Html)
                                response.ContentType = defaultContentType;
                                
                            //retain behavior with ASP.NET's response.Write(string)
                            if (response.ContentType.IndexOf(';') == -1)
                                response.ContentType += ContentFormat.Utf8Suffix;

                            if (bodyPrefix != null) 
                                await response.OutputStream.WriteAsync(bodyPrefix, token);

                            await response.OutputStream.WriteAsync(uf8Bytes.Value, token);

                            if (bodySuffix != null) 
                                await response.OutputStream.WriteAsync(bodySuffix, token);

                            return true;
                        }

                        if (defaultAction == null)
                        {
                            throw new ArgumentNullException(nameof(defaultAction),
                                $@"As result '{(result != null ? result.GetType().GetOperationName() : "")}' is not a supported responseType, a defaultAction must be supplied");
                        }

                        if (bodyPrefix != null)
                            await response.OutputStream.WriteAsync(bodyPrefix, token);

                        if (result != null)
                        {
                            bool handled = false;
#if NET8_0_OR_GREATER
                            var isJson = response.ContentType is MimeTypes.Json or MimeTypes.JsonUtf8Suffix;
                            if (isJson && request.Dto is not null)
                            {
                                var appHost = ServiceStackHost.Instance;
                                var op = appHost?.Metadata.GetOperation(request.Dto.GetType());
                                if (op?.UseSystemJson != null && op.UseSystemJson.HasFlag(UseSystemJson.Response))
                                {
                                    handled = true;
                                    var systemJsonOptions = jsScope != null
                                        ? TextConfig.CustomSystemJsonOptions(TextConfig.SystemJsonOptions, jsScope)
                                        : TextConfig.SystemJsonOptions;
                                    await System.Text.Json.JsonSerializer.SerializeAsync(response.OutputStream, result, systemJsonOptions, token).ConfigAwait();
                                }
                            }
#endif
                            if (!handled)
                                await defaultAction(request, result, response.OutputStream);
                        }

                        if (bodySuffix != null)
                            await response.OutputStream.WriteAsync(bodySuffix, token);
                    }
                }

                return false;
            }
            catch (Exception originalEx)
            {
                //.NET Core prohibits some status codes from having a body
                if (originalEx is InvalidOperationException invalidEx)
                {
                    Log.Error(invalidEx.Message, invalidEx);
                    try
                    {
                        flushAsync = false;
                        await response.OutputStream.FlushAsync(token); // Prevent hanging clients
                    }
                    catch(Exception flushEx) { Log.Error("response.OutputStream.FlushAsync()", flushEx); }
                }

                await HandleResponseWriteException(originalEx, request, response, defaultContentType);
                return true;
            }
            finally
            {
                if (flushAsync) // move async Thread Hop to outside JsConfigScope so .NET v4.5 disposes same scope
                {
                    try
                    {
                        await response.FlushAsync(token);
                    }
                    catch(Exception flushEx) { Log.Error("response.FlushAsync()", flushEx); }
                }
                disposableResult?.Dispose();
                await response.EndRequestAsync(skipHeaders: true);
            }
        }
    }

    internal static async Task HandleResponseWriteException(this Exception originalEx, IRequest request, IResponse response, string defaultContentType)
    {
        await HostContext.RaiseAndHandleException(request, response, request.OperationName, originalEx);

        if (!HostContext.Config.WriteErrorsToResponse)
            throw originalEx;

        var errorMessage = $"Error occured while Processing Request: [{originalEx.GetType().GetOperationName()}] {originalEx.Message}";

        try
        {
            if (!response.IsClosed)
            {
                await response.WriteErrorToResponse(
                    request,
                    defaultContentType ?? request.ResponseContentType,
                    request.OperationName,
                    errorMessage,
                    originalEx,
                    (int) HttpStatusCode.InternalServerError);
            }
        }
        catch (Exception writeErrorEx)
        {
            //Exception in writing to response should not hide the original exception
            Log.Info("Failed to write error to response: {0}", writeErrorEx);
            throw originalEx;
        }
    }

    public static async Task WriteBytesToResponse(this IResponse res, byte[] responseBytes, string contentType, CancellationToken token = default)
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
            await res.EndRequestAsync(skipHeaders: true);
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
        httpRes.Dto = HostContext.AppHost.CreateErrorResponse(ex);
        var serializer = HostContext.ContentTypes.GetStreamSerializerAsync(MimeTypes.Html);
        serializer?.Invoke(req, httpRes.Dto, httpRes.OutputStream);
        httpRes.EndHttpHandlerRequest(skipHeaders: true);
        return TypeConstants.EmptyTask;
    }

    public static async Task WriteErrorToResponse(this IResponse httpRes, IRequest httpReq,
        string contentType, string operationName, string errorMessage, Exception ex, int statusCode)
    {
        if (ex == null)
            ex = new Exception(errorMessage);

        httpRes.Dto = HostContext.AppHost.CreateErrorResponse(ex, request:httpReq?.Dto);

        if (await HandleCustomErrorHandler(httpRes, httpReq, contentType, statusCode, httpRes.Dto, ex))
            return;

        var hostConfig = HostContext.Config;
        if (!httpRes.HasStarted)
        {
            if (httpRes.ContentType is null or MimeTypes.Html 
                && contentType != null && contentType != httpRes.ContentType)
            {
                httpRes.ContentType = contentType;
            }
            if (hostConfig.AppendUtf8CharsetOnContentTypes.Contains(contentType))
            {
                httpRes.ContentType += ContentFormat.Utf8Suffix;
            }

            var hold = httpRes.StatusDescription;
            var hasDefaultStatusDescription = hold is null or "OK";

            httpRes.StatusCode = statusCode;

            httpRes.StatusDescription = hasDefaultStatusDescription
                ? (errorMessage ?? HttpStatus.GetStatusDescription(statusCode))
                : hold;

            httpRes.ApplyGlobalResponseHeaders();
        }

        var callback = httpReq.GetJsonpCallback();
        var doJsonp = hostConfig.AllowJsonpRequests && !string.IsNullOrEmpty(callback);
        if (doJsonp)
        {
            httpRes.StatusCode = 200;
            httpRes.ContentType = hostConfig.JsonpContentType;
            await httpRes.OutputStream.WriteAsync(DataCache.CreateJsonpPrefix(callback));
        }

        var serializer = HostContext.ContentTypes.GetStreamSerializerAsync(contentType ?? httpRes.ContentType);
        if (serializer != null)
        {
            var jsconfig = hostConfig.AllowJsConfig ? httpReq?.QueryString[Keywords.JsConfig] : null;
            using (jsconfig != null ? JsConfig.CreateScope(jsconfig) : null)
            {
                await serializer(httpReq, httpRes.Dto, httpRes.OutputStream);                    
            }
        }
            
        if (doJsonp)
            await httpRes.OutputStream.WriteAsync(DataCache.JsonpSuffix);

        httpRes.EndHttpHandlerRequest(skipHeaders: true);
    }

    private static async Task<bool> HandleCustomErrorHandler(this IResponse httpRes, IRequest httpReq,
        string contentType, int statusCode, object errorDto, Exception ex)
    {
        if (httpReq != null && MimeTypes.Html.MatchesContentType(contentType) && httpReq.GetView() == null)
        {
            var errorHandler = HostContext.AppHost.GetCustomErrorHandler(statusCode)
                               ?? HostContext.AppHost.GlobalHtmlErrorHttpHandler;
            if (errorHandler != null)
            {
                httpReq.Items[Keywords.Model] = errorDto;
                httpReq.Items[Keywords.ErrorStatus] = errorDto.GetResponseStatus();
                if (ex != null)
                {
                    httpReq.Items[Keywords.Error] = ex;
                }
                await errorHandler.ProcessRequestAsync(httpReq, httpRes, httpReq.OperationName);
                return true;
            }
        }
        return false;
    }

    public static bool ShouldWriteGlobalHeaders(IResponse httpRes)
    {
        if (!httpRes.HasStarted && HostContext.Config != null && !httpRes.Items.ContainsKey(Keywords.HasGlobalHeaders))
        {
            httpRes.Items[Keywords.HasGlobalHeaders] = bool.TrueString;
            return true;
        }
        return false;
    }

#if !NETCORE
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