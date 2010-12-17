using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Metadata;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints
{
	public class ServiceStackHttpHandlerFactory
		: IHttpHandlerFactory
	{
		public IHttpHandler GetHandler(HttpContext context, string requestType, string url, string pathTranslated)
		{
			var pathInfo = context.Request.GetPathInfo();

			if (string.IsNullOrEmpty(pathInfo) || pathInfo == "/")
			{
				var rawUrl = context.Request.RawUrl.ToLower();
				if (rawUrl.Contains(EndpointHost.Config.ServiceStackHandlerFactoryPath))
				{
					return new IndexPageHttpHandler();
				}
				return new DefaultHttpHandler();
			}

			return GetHandlerForPathInfo(context.Request.HttpMethod, pathInfo)
				?? new DefaultHttpHandler();
		}

		public static IHttpHandler GetHandlerForPathInfo(string httpMethod, string pathInfo)
		{
			var pathParts = pathInfo.TrimStart('/').Split('/');
			if (pathParts.Length == 0) return new NotFoundHttpHandler();

			var handler = GetHandlerForPathParts(pathParts);
			if (handler != null) return handler;

			var restPath = RestHandler.FindMatchingRestPath(httpMethod, pathInfo);
			return restPath != null 
				? new RestHandler { RestPath = restPath, RequestName = pathInfo } 
				: null;
		}

		private static IHttpHandler GetHandlerForPathParts(string[] pathParts)
		{
			var pathController = string.Intern(pathParts[0].ToLower());
			if (pathParts.Length == 1)
			{
				if (pathController == "metadata")
					return new IndexMetadataHandler();
				if (pathController == "soap11")
					return new Soap11Handlers();
				if (pathController == "soap12")
					return new Soap12MessageSyncReplyHttpHandler();

				return null;
			}

			var pathAction = string.Intern(pathParts[1].ToLower());
			var requestName = pathParts.Length > 2 ? pathParts[2] : null;
			switch (pathController)
			{
				case "json":
					if (pathAction == "syncreply")
						return new JsonSyncReplyHandler { RequestName = requestName };
					if (pathAction == "asynconeway")
						return new JsonAsyncOneWayHandler { RequestName = requestName };
					if (pathAction == "metadata")
						return new JsonMetadataHandler();
					break;

				case "xml":
					if (pathAction == "syncreply")
						return new XmlSyncReplyHandler { RequestName = requestName };
					if (pathAction == "asynconeway")
						return new XmlAsyncOneWayHandler { RequestName = requestName };
					if (pathAction == "metadata")
						return new XmlMetadataHandler();
					break;

				case "jsv":
					if (pathAction == "syncreply")
						return new JsvHttpHandlers { RequestName = requestName };
					if (pathAction == "asynconeway")
						return new JsvAsyncOneWayHandler { RequestName = requestName };
					if (pathAction == "metadata")
						return new JsvMetadataHandler();
					break;

				case "soap11":
					if (pathAction == "wsdl")
						return new Soap11WsdlMetadataHandler();
					if (pathAction == "metadata")
						return new Soap11MetadataHandler();
					break;

				case "soap12":
					if (pathAction == "wsdl")
						return new Soap12WsdlMetadataHandler();
					if (pathAction == "metadata")
						return new Soap12MetadataHandler();
					break;
			}

			return null;
		}

		public void ReleaseHandler(IHttpHandler handler)
		{
		}
	}
}