using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Web;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceModel.Extensions;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints
{
	public class RestHandler 
		: EndpointHandlerBase, IHttpHandler
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(RestHandler));

		public static string[] PreferredContentTypes = new[] {
			ContentType.Json, ContentType.Xml, ContentType.Jsv
		};

		public static IRestPath FindMatchingRestPath(string httpMethod, string pathInfo)
		{
			var controller = ServiceManager != null
				? ServiceManager.ServiceController
				: EndpointHost.Config.ServiceController;

			return controller.GetRestPathForRequest(httpMethod, pathInfo);
		}

		public static string GetDefaultContentType(string[] acceptContentType, string defaultContentType)
		{
			var acceptsAnything = false;
			var hasDefaultContentType = !string.IsNullOrEmpty(defaultContentType);
			foreach (var contentType in acceptContentType)
			{
				acceptsAnything = acceptsAnything || contentType == "*/*";
				if (acceptsAnything && hasDefaultContentType) return defaultContentType;

				foreach (var preferredContentType in PreferredContentTypes)
				{
					if (contentType.StartsWith(preferredContentType)) return preferredContentType;
				}
			}

			//We could also send a '406 Not Acceptable', but this is allowed also
			return EndpointHost.Config.DefaultContentType;
		}

		public IRestPath GetRestPath(string httpMethod, string pathInfo)
		{
			if (this.RestPath == null)
			{
				this.RestPath = FindMatchingRestPath(httpMethod, pathInfo);
			}
			return this.RestPath;
		}

		internal IRestPath RestPath { get; set; }

		public override EndpointAttributes HandlerAttributes
		{
			get { return EndpointAttributes.SyncReply; }
		}

		public override void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
		{

			try
			{
				var restPath = GetRestPath(httpReq.HttpMethod, httpReq.PathInfo);
				if (restPath == null)
					throw new NotSupportedException("No RestPath found for: " + httpReq.HttpMethod + " " + httpReq.PathInfo);

				var defaultContentType = GetDefaultContentType(httpReq.AcceptTypes, restPath.DefaultContentType);

				var requestParams = httpReq.GetRequestParams();
				var request = restPath.CreateRequest(httpReq.PathInfo, requestParams);

				var attrEndpointType = ContentType.GetEndpointAttributes(defaultContentType);

				var result = ExecuteService(request,
					HandlerAttributes | attrEndpointType | GetEndpointAttributes(httpReq));

				httpRes.WriteToResponse(result,
					(dto) => HttpResponseFilter.Instance.Serialize(defaultContentType, dto),
					defaultContentType);
			}
			catch (Exception ex)
			{
				var errorMessage = string.Format("Error occured while Processing Request: {0}", ex.Message);
				Log.Error(errorMessage, ex);

				httpRes.WriteJsonErrorToResponse(operationName, errorMessage, ex);
			}
		}

		public bool IsReusable
		{
			get { return false; }
		}

		/// <summary>
		/// Used in Unit tests
		/// </summary>
		/// <returns></returns>
		public override object CreateRequest(IHttpRequest httpReq, string operationName)
		{
			if (this.RestPath == null)
				throw new ArgumentNullException("No RestPath found");

			var requestParams = httpReq.QueryString.ToDictionary();
			httpReq.FormData.ToDictionary().ForEach(x => requestParams.Add(x.Key, x.Value));

			var request = this.RestPath.CreateRequest(httpReq.PathInfo, requestParams);
			return request;
		}
	}

}