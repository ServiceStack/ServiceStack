using System;
using System.Web;
using ServiceStack.WebHost.Endpoints.Metadata;

namespace ServiceStack.WebHost.Endpoints
{

	public class ServiceStackHttpHandlerFactory
		: IHttpHandlerFactory
	{
		public IHttpHandler GetHandler(HttpContext context, string requestType, string url, string pathTranslated)
		{
			var pathInfo = context.Request.PathInfo;
			return GetHandlerForPathInfo(pathInfo);
		}

		public static IHttpHandler GetHandlerForPathInfo(string pathInfo)
		{
			var noMatchFound = new NotSupportedException("No matching Path for Request found: " + pathInfo);

			var pathParts = pathInfo.TrimStart('/').Split('/');
			if (pathParts.Length == 0) throw noMatchFound;

			var pathController = string.Intern(pathParts[0]);
			if (pathParts.Length == 1)
			{
				if (pathController == "Metadata")
					return new IndexMetadataHandler();
				if (pathController == "Soap11")
					return new Soap11MessageSyncReplyHttpHandler();
				if (pathController == "Soap12")
					return new Soap12MessageSyncReplyHttpHandler();

				throw noMatchFound;
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

			throw noMatchFound;
		}

		public void ReleaseHandler(IHttpHandler handler)
		{
		}
	}
}