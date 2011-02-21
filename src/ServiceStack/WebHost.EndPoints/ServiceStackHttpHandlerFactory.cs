using System.Collections.Generic;
using System.IO;
using System.Web;
using ServiceStack.Common.Utils;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Metadata;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints
{
	public class ServiceStackHttpHandlerFactory
		: IHttpHandlerFactory
	{
		static readonly List<string> WebHostRootFileNames = new List<string>();
		static private readonly string WebHostPhysicalPath = null;
		static private readonly string DefaultRootFileName = null;
		static private readonly IHttpHandler DefaultHttpHandler = null;
        static private readonly IHttpHandler ForbiddenHttpHandler = null;
		private static readonly bool IsIntegratedPipeline = false;

		static ServiceStackHttpHandlerFactory()
		{
			//MONO doesn't implement this property
			var pi = typeof(HttpRuntime).GetProperty("UsingIntegratedPipeline");
			if (pi != null)
			{
				IsIntegratedPipeline = (bool)pi.GetGetMethod().Invoke(null, new object[0]);
			}

			//DefaultHttpHandler not supported in IntegratedPipeline mode
			if (!IsIntegratedPipeline)
				DefaultHttpHandler = new DefaultHttpHandler();

		    ForbiddenHttpHandler = new ForbiddenHttpHandler();

			WebHostPhysicalPath = "~".MapHostAbsolutePath();
			foreach (var fileName in Directory.GetFiles(WebHostPhysicalPath))
			{
				var fileNameLower = Path.GetFileName(fileName).ToLower();
				if (DefaultRootFileName == null && EndpointHost.Config.DefaultDocuments.Contains(fileNameLower))
				{
					DefaultRootFileName = fileNameLower;
					if (DefaultHttpHandler == null)
						DefaultHttpHandler = new RedirectHttpHandler { RelativeUrl = DefaultRootFileName };
				}
				WebHostRootFileNames.Add(Path.GetFileName(fileNameLower));
			}
			foreach (var dirName in Directory.GetDirectories(WebHostPhysicalPath))
			{
				var dirNameLower = Path.GetFileName(dirName).ToLower();
				WebHostRootFileNames.Add(Path.GetFileName(dirNameLower));
			}

			if (DefaultHttpHandler == null)
				DefaultHttpHandler = new NotFoundHttpHandler();
		}

        public IHttpHandler GetHandler(HttpContext context, string requestType, string url, string pathTranslated)
        {
            var pathInfo = context.Request.GetPathInfo();

            if (string.IsNullOrEmpty(pathInfo) || pathInfo == "/" ||
                (!string.IsNullOrEmpty(EndpointHost.Config.ServiceStackHandlerFactoryPath) &&
                 pathInfo.EndsWith(EndpointHost.Config.ServiceStackHandlerFactoryPath)))
            {
                var requestPath = context.Request.Path.ToLower();
                var handlerPath = EndpointHost.Config.ServiceStackHandlerFactoryPath;
                if ((requestPath == "/" + handlerPath) || (requestPath == handlerPath) ||
                    (requestPath == handlerPath + "/"))
                {
                    if (context.Request.PhysicalPath != WebHostPhysicalPath
                        || !File.Exists(Path.Combine(context.Request.PhysicalPath, DefaultRootFileName ?? "")))
                    {
                        return new IndexPageHttpHandler();
                    }
                }

                // no handler registered 
                // serve the file from the filesystem, restricting to a safelist of extensions

                var filePath = context.Request.FilePath;
                var filename = System.IO.Path.GetFileName(filePath);
                string[] extensions = { ".js", ".css", ".ico", ".htm", ".html" };
                bool okToServe = false;

                if (!filename.StartsWith("."))
                {
                    foreach (var extension in extensions)
                    {
                        if (filePath.EndsWith(extension))
                        {
                            okToServe = true;
                            break;
                        }
                    }
                }

                if (okToServe) return DefaultHttpHandler;
                else return ForbiddenHttpHandler;
            }

            return GetHandlerForPathInfo(context.Request.HttpMethod, pathInfo)
                   ?? DefaultHttpHandler;
        }

	    public static IHttpHandler GetHandlerForPathInfo(string httpMethod, string pathInfo)
		{
			var pathParts = pathInfo.TrimStart('/').Split('/');
			if (pathParts.Length == 0) return new NotFoundHttpHandler();

			var handler = GetHandlerForPathParts(pathParts);
			if (handler != null) return handler;

			var existingFile = pathParts[0].ToLower();
			if (WebHostRootFileNames.Contains(existingFile))
			{
				//Avoid recursive redirections
				return !IsIntegratedPipeline
					? DefaultHttpHandler
					: new StaticFileHandler();
			}

			var restPath = RestHandler.FindMatchingRestPath(httpMethod, pathInfo);			
			return restPath != null
				? new RestHandler { RestPath = restPath, RequestName = restPath.RequestType.Name }
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

				default:

					string contentType;
					if (EndpointHost.Config.ContentTypeFilter
						.ContentTypeFormats.TryGetValue(pathController, out contentType))
					{
						var format = Common.Web.ContentType.GetContentFormat(contentType);
						if (pathAction == "syncreply")
							return new GenericHandler(contentType, EndpointAttributes.SyncReply)
							{
								RequestName = requestName
							};
						if (pathAction == "asynconeway")
							return new GenericHandler(contentType, EndpointAttributes.AsyncOneWay)
							{
								RequestName = requestName
							};
						if (pathAction == "metadata")
							return new CustomMetadataHandler(contentType, format);
					}
					break;
			}

			return null;
		}

		public void ReleaseHandler(IHttpHandler handler)
		{
		}
	}
}