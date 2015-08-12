using System;
using System.Net;
using System.Web;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Extensions;

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

        public static string ToAbsoluteUri(this IReturn request, string httpMethod = null, string formatFallbackToPredefinedRoute = null)
        {
            var relativeUrl = request.ToUrl(
                httpMethod ?? HttpMethods.Get,
                formatFallbackToPredefinedRoute ?? EndpointHost.Config.DefaultContentType.ToContentFormat());

            var absoluteUrl = EndpointHost.Config.WebHostUrl.CombineWith(relativeUrl);
            return absoluteUrl;
        }

        /// <summary>
        /// End a ServiceStack Request
        /// </summary>
        public static void EndRequest(this HttpResponse httpRes, bool skipHeaders = false)
        {
            if (!skipHeaders) httpRes.ApplyGlobalResponseHeaders();
            httpRes.Close();
            EndpointHost.CompleteRequest();
        }

        /// <summary>
        /// End a ServiceStack Request
        /// </summary>
        public static void EndRequest(this IHttpResponse httpRes, bool skipHeaders = false)
        {
            httpRes.EndHttpHandlerRequest(skipHeaders: skipHeaders);
            EndpointHost.CompleteRequest();
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
        /// End an MQ Request
        /// </summary>
        public static void EndMqRequest(this IHttpResponse httpRes, bool skipClose = false)
        {
            if (!skipClose && !httpRes.IsClosed) httpRes.Close();
            httpRes.EndRequest();
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
            if (EndpointHost.Config == null || EndpointHost.Config.Return204NoContentForEmptyResponse)
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