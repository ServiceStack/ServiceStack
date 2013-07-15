using System;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.MiniProfiler;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints
{
    public class RestHandler
        : EndpointHandlerBase
    {
        public RestHandler()
        {
            this.HandlerAttributes = EndpointAttributes.Reply;
        }

        private static readonly ILog Log = LogManager.GetLogger(typeof(RestHandler));

        public static IRestPath FindMatchingRestPath(string httpMethod, string pathInfo, out string contentType)
        {
            var controller = ServiceManager != null
                ? ServiceManager.ServiceController
                : EndpointHost.Config.ServiceController;

            pathInfo = GetSanitizedPathInfo(pathInfo, out contentType);

            return controller.GetRestPathForRequest(httpMethod, pathInfo);
        }

        private static string GetSanitizedPathInfo(string pathInfo, out string contentType)
        {
            contentType = null;
            if (EndpointHost.Config.AllowRouteContentTypeExtensions)
            {
                var pos = pathInfo.LastIndexOf('.');
                if (pos >= 0)
                {
                    var format = pathInfo.Substring(pos + 1);
                    contentType = EndpointHost.ContentTypeFilter.GetFormatContentType(format);
                    if (contentType != null)
                    {
                        pathInfo = pathInfo.Substring(0, pos);
                    }
                }
            }
            return pathInfo;
        }

        public IRestPath GetRestPath(string httpMethod, string pathInfo)
        {
            if (this.RestPath == null)
            {
                string contentType;
                this.RestPath = FindMatchingRestPath(httpMethod, pathInfo, out contentType);
                
                if (contentType != null)
                    ResponseContentType = contentType;
            }
            return this.RestPath;
        }

        public IRestPath RestPath { get; set; }

        // Set from SSHHF.GetHandlerForPathInfo()
        public string ResponseContentType { get; set; }

        public override void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName, Action closeAction = null)
        {
            try
            {
                if (EndpointHost.ApplyPreRequestFilters(httpReq, httpRes)) return;

                var restPath = GetRestPath(httpReq.HttpMethod, httpReq.PathInfo);
                if (restPath == null)
                    throw new NotSupportedException("No RestPath found for: " + httpReq.HttpMethod + " " + httpReq.PathInfo);

                operationName = restPath.RequestType.Name;

                if (ResponseContentType != null)
                    httpReq.ResponseContentType = ResponseContentType;

                var responseContentType = httpReq.ResponseContentType;
                EndpointHost.Config.AssertContentType(responseContentType);

                var request = GetRequest(httpReq, restPath);
                if (EndpointHost.ApplyRequestFilters(httpReq, httpRes, request)) return;

                var response = GetResponse(httpReq, httpRes, request);

	            if (response is IAsyncResult)
	            {
		            AsyncResultFactory.ProcessAsyncResponse(response as IAsyncResult, result => ProcessResponse(httpReq, httpRes, result, closeAction));
		            return;
	            }
							ProcessResponse(httpReq, httpRes, response, closeAction);
            }
            catch (Exception ex)
            {
                if (!EndpointHost.Config.WriteErrorsToResponse) throw;
                HandleException(httpReq, httpRes, operationName, ex);
            }
        }




	    private static void ProcessResponse(IHttpRequest httpReq, IHttpResponse httpRes, object response, Action closeAction)
	    {
		    if (EndpointHost.ApplyResponseFilters(httpReq, httpRes, response)) return;

		    if (httpReq.ResponseContentType.Contains("jsv") && !string.IsNullOrEmpty(httpReq.QueryString["debug"]))
		    {
			    JsvSyncReplyHandler.WriteDebugResponse(httpRes, response);
					if (closeAction != null)
						closeAction();
			    return;
		    }
		    var callback = httpReq.GetJsonpCallback();
		    var doJsonp = EndpointHost.Config.AllowJsonpRequests
		                  && !string.IsNullOrEmpty(callback);
		    if (doJsonp && !(response is CompressedResult))
			    httpRes.WriteToResponse(httpReq, response, (callback + "(").ToUtf8Bytes(), ")".ToUtf8Bytes());
		    else
			    httpRes.WriteToResponse(httpReq, response);
				if (closeAction != null)
					closeAction();
	    }

	    public override object GetResponse(IHttpRequest httpReq, IHttpResponse httpRes, object request)
        {
            var requestContentType = ContentType.GetEndpointAttributes(httpReq.ResponseContentType);

            return ExecuteService(request,
                HandlerAttributes | requestContentType | httpReq.GetAttributes(), httpReq, httpRes);
        }

        private static object GetRequest(IHttpRequest httpReq, IRestPath restPath)
        {
            var requestType = restPath.RequestType;
            using (Profiler.Current.Step("Deserialize Request"))
            {
                try
                {
                    var requestDto = GetCustomRequestFromBinder(httpReq, requestType);
                    if (requestDto != null) return requestDto;

                    var requestParams = httpReq.GetRequestParams();
                    requestDto = CreateContentTypeRequest(httpReq, requestType, httpReq.ContentType);

                    string contentType;
                    var pathInfo = !restPath.IsWildCardPath 
                        ? GetSanitizedPathInfo(httpReq.PathInfo, out contentType)
                        : httpReq.PathInfo;

                    return restPath.CreateRequest(pathInfo, requestParams, requestDto);
                }
                catch (SerializationException e)
                {
                    throw new RequestBindingException("Unable to bind request", e);
                }
                catch (ArgumentException e)
                {
                    throw new RequestBindingException("Unable to bind request", e);
                }
            }
        }

        /// <summary>
        /// Used in Unit tests
        /// </summary>
        /// <returns></returns>
        public override object CreateRequest(IHttpRequest httpReq, string operationName)
        {
            if (this.RestPath == null)
                throw new ArgumentNullException("No RestPath found");

            return GetRequest(httpReq, this.RestPath);
        }
    }

}
