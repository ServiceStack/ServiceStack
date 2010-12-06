using System;
using System.Collections.Generic;
using System.Web;
using ServiceStack.WebHost.Endpoints.Metadata;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints
{
	public class ServiceStackHttpHandlerFactory
		: IHttpHandlerFactory
	{
		public IHttpHandler GetHandler(HttpContext context, string requestType, string url, string pathTranslated)
		{
			var pathInfo = context.Request.PathInfo;
			return !string.IsNullOrEmpty(pathInfo) 
				? GetHandlerForPathInfo(pathInfo)
				: GetHandlerForPath(context.Request.Path);
		}

		public static IHttpHandler GetHandlerForPathInfo(string pathInfo)
		{
			var pathParts = pathInfo.TrimStart('/').Split('/');
			if (pathParts.Length == 0) return new NotFoundHttpHandler();

			return GetHandlerForPathParts(pathParts);
		}

		public static IHttpHandler GetHandlerForPath(string fullPath)
		{
			var mappedPathRoot = EndpointHost.Config.ServiceStackHandlerFactoryPath;
			var pathParts = new List<string>();

			var fullPathParts = fullPath.Split('/');
			var pathRootFound = false;
			foreach (var fullPathPart in fullPathParts)
			{
				if (pathRootFound)
				{
					pathParts.Add(fullPathPart);
				}
				else
				{
					pathRootFound = fullPathPart == mappedPathRoot;
				}
			}

			return GetHandlerForPathParts(pathParts.ToArray());
		}

		private static IHttpHandler GetHandlerForPathParts(string[] pathParts)
		{
			var pathController = string.Intern(pathParts[0].ToLower());
			if (pathParts.Length == 1)
			{
				if (pathController == "metadata")
					return new IndexMetadataHandler();
				if (pathController == "soap11")
					return new Soap11MessageSyncReplyHttpHandler();
				if (pathController == "soap12")
					return new Soap12MessageSyncReplyHttpHandler();

				return new NotFoundHttpHandler();
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
						return new JsvSyncReplyHandler { RequestName = requestName };
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

			return new NotFoundHttpHandler();
		}

		public void ReleaseHandler(IHttpHandler handler)
		{
		}
	}
}