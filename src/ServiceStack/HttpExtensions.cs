using System;
using System.Net;
using System.Web;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack
{
    public static class HttpExtensions
    {
        public static HttpRequestContext ToRequestContext(this HttpContext httpContext, object requestDto = null)
        {
            return new HttpRequestContext(
                httpContext.Request.ToRequest(),
                httpContext.Response.ToResponse(),
                requestDto);
        }

        public static HttpRequestContext ToRequestContext(this HttpListenerContext httpContext, object requestDto = null)
        {
            return new HttpRequestContext(
                httpContext.Request.ToRequest(),
                httpContext.Response.ToResponse(),
                requestDto);
        }

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

        public static string ToAbsoluteUri(this string relativeUrl)
        {
            var absoluteUrl = HostContext.Config.WebHostUrl.CombineWith(relativeUrl);
            return absoluteUrl;
        }

        /// <summary>
        /// End a ServiceStack Request
        /// </summary>
        public static void EndRequest(this HttpResponse httpRes, bool skipHeaders = false)
        {
            if (!skipHeaders) httpRes.ApplyGlobalResponseHeaders();
            httpRes.Close();
            HostContext.CompleteRequest();
        }

        /// <summary>
        /// End a ServiceStack Request
        /// </summary>
        public static void EndRequest(this IHttpResponse httpRes, bool skipHeaders = false)
        {
            httpRes.EndHttpHandlerRequest(skipHeaders: skipHeaders);
            HostContext.CompleteRequest();
        }

        /// <summary>
        /// End a HttpHandler Request
        /// </summary>
        public static void EndHttpHandlerRequest(this HttpResponse httpRes, bool skipHeaders = false, bool skipClose = false, bool closeOutputStream = false, Action<HttpResponse> afterBody = null)
        {
            if (!skipHeaders) httpRes.ApplyGlobalResponseHeaders();
            if (afterBody != null) afterBody(httpRes);
            if (closeOutputStream) httpRes.CloseOutputStream();
            else if (!skipClose) httpRes.Close();

            //skipHeaders used when Apache+mod_mono doesn't like:
            //response.OutputStream.Flush();
            //response.Close();
        }

        /// <summary>
        /// End a HttpHandler Request
        /// </summary>
        public static void EndHttpHandlerRequest(this IHttpResponse httpRes, bool skipHeaders = false, bool skipClose = false, Action<IHttpResponse> afterBody = null)
        {
            if (!skipHeaders) httpRes.ApplyGlobalResponseHeaders();
            if (afterBody != null) afterBody(httpRes);
            if (!skipClose) httpRes.Close();

            //skipHeaders used when Apache+mod_mono doesn't like:
            //response.OutputStream.Flush();
            //response.Close();
        }

        /// <summary>
        /// End a ServiceStack Request with no content
        /// </summary>
        public static void EndRequestWithNoContent(this IHttpResponse httpRes)
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