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
			var pathController = string.Intern(pathParts[0]);
			if (pathParts.Length == 1)
			{
				if (pathController == "Metadata")
					return new IndexMetadataHandler();
				if (pathController == "Soap11")
					return new Soap11MessageSyncReplyHttpHandler();
				if (pathController == "Soap12")
					return new Soap12MessageSyncReplyHttpHandler();

				return new NotFoundHttpHandler();
			}

			var pathAction = string.Intern(pathParts[1]);
			var requestName = pathParts.Length > 2 ? pathParts[2] : null;
			switch (pathController)
			{
				case "Json":
					if (pathAction == "SyncReply")
						return new JsonSyncReplyHandler { RequestName = requestName };
					if (pathAction == "AsyncOneWay")
						return new JsonAsyncOneWayHandler { RequestName = requestName };
					if (pathAction == "Metadata")
						return new JsonMetadataHandler();
					break;

				case "Xml":
					if (pathAction == "SyncReply")
						return new XmlSyncReplyHandler { RequestName = requestName };
					if (pathAction == "AsyncOneWay")
						return new XmlAsyncOneWayHandler { RequestName = requestName };
					if (pathAction == "Metadata")
						return new XmlMetadataHandler();
					break;

				case "Jsv":
					if (pathAction == "SyncReply")
						return new JsvSyncReplyHandler { RequestName = requestName };
					if (pathAction == "AsyncOneWay")
						return new JsvAsyncOneWayHandler { RequestName = requestName };
					if (pathAction == "Metadata")
						return new JsvMetadataHandler();
					break;

				case "Soap11":
					if (pathAction == "Wsdl")
						return new Soap11WsdlMetadataHandler();
					if (pathAction == "Metadata")
						return new Soap11WsdlMetadataHandler();
					break;

				case "Soap12":
					if (pathAction == "Wsdl")
						return new Soap12WsdlMetadataHandler();
					if (pathAction == "Metadata")
						return new Soap12WsdlMetadataHandler();
					break;
			}

			return new NotFoundHttpHandler();
		}

		public void ReleaseHandler(IHttpHandler handler)
		{
		}
	}
}