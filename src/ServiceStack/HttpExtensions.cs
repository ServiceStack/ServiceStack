﻿using System;
using System.Net;
using System.Web;
using ServiceStack.Web;

namespace ServiceStack
{
    public static class HttpExtensions
    {
        public static string ToAbsoluteUri(this IReturn requestDto, string httpMethod = null, string formatFallbackToPredefinedRoute = null)
        {
            var relativeUrl = requestDto.ToUrl(
                httpMethod ?? HttpMethods.Get,
                formatFallbackToPredefinedRoute ?? HostContext.Config.DefaultContentType.ToContentFormat());

            return relativeUrl.ToAbsoluteUri();
        }

        public static string ToAbsoluteUri(this object requestDto, string httpMethod = null, string formatFallbackToPredefinedRoute = null)
        {
            var relativeUrl = requestDto.ToUrl(
                httpMethod ?? HttpMethods.Get,
                formatFallbackToPredefinedRoute ?? HostContext.Config.DefaultContentType.ToContentFormat());

            return relativeUrl.ToAbsoluteUri();
        }

        public static string ToAbsoluteUri(this object requestDto, IRequest req, string httpMethod = null, string formatFallbackToPredefinedRoute = null)
        {
            var relativeUrl = requestDto.ToUrl(
                httpMethod ?? HttpMethods.Get,
                formatFallbackToPredefinedRoute ?? HostContext.Config.DefaultContentType.ToContentFormat());

            return relativeUrl.ToAbsoluteUri(req);
        }

        public static string ToAbsoluteUri(this string relativeUrl, IRequest req = null)
        {
            if (req == null)
                req = HostContext.TryGetCurrentRequest();

            var absoluteUrl = HostContext.ResolveAbsoluteUrl("~/".CombineWith(relativeUrl), req);
            return absoluteUrl;
        }

        /// <summary>
        /// End a ServiceStack Request
        /// </summary>
        public static void EndRequest(this IResponse httpRes, bool skipHeaders = false)
        {
            httpRes.EndHttpHandlerRequest(skipHeaders: skipHeaders);
        }

#if !NETSTANDARD1_6
        /// <summary>
        /// End a ServiceStack Request
        /// </summary>
        public static void EndRequest(this HttpResponseBase httpRes, bool skipHeaders = false)
        {
            if (!skipHeaders) httpRes.ApplyGlobalResponseHeaders();
            httpRes.Close();
            HostContext.CompleteRequest(null);
        }

        /// <summary>
        /// End a HttpHandler Request
        /// </summary>
        public static void EndHttpHandlerRequest(this HttpContextBase context, bool skipHeaders = false, bool skipClose = false, bool closeOutputStream = false, Action<HttpResponseBase> afterHeaders = null)
        {
            var httpRes = context.Response;
            if (!skipHeaders) httpRes.ApplyGlobalResponseHeaders();
            afterHeaders?.Invoke(httpRes);
            if (closeOutputStream) httpRes.CloseOutputStream();
            else if (!skipClose) httpRes.Close();
            HostContext.CompleteRequest(context.ToRequest());

            //skipHeaders used when Apache+mod_mono doesn't like:
            //response.OutputStream.Flush();
            //response.Close();
        }
#endif

        /// <summary>
        /// End a HttpHandler Request
        /// </summary>
        public static void EndHttpHandlerRequest(this IResponse httpRes, bool skipHeaders = false, bool skipClose = false, Action<IResponse> afterHeaders = null)
        {
            if (!skipHeaders) httpRes.ApplyGlobalResponseHeaders();
            afterHeaders?.Invoke(httpRes);
            if (!skipClose && !httpRes.IsClosed) httpRes.Close();
            HostContext.CompleteRequest(httpRes.Request);
        }

        /// <summary>
        /// End an MQ Request
        /// </summary>
        public static void EndMqRequest(this IResponse httpRes, bool skipClose = false)
        {
            if (!skipClose && !httpRes.IsClosed) httpRes.Close();
            HostContext.CompleteRequest(httpRes.Request);
        }

        /// <summary>
        /// End a ServiceStack Request with no content
        /// </summary>
        public static void EndRequestWithNoContent(this IResponse httpRes)
        {
            if (HostContext.Config == null || HostContext.Config.Return204NoContentForEmptyResponse)
            {
                if (httpRes.StatusCode == (int)HttpStatusCode.OK)
                {
                    httpRes.StatusCode = (int)HttpStatusCode.NoContent;
                }
            }

            httpRes.SetContentLength(0);
            httpRes.EndRequest();
        }
    }
}