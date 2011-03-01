using System.Collections.Generic;
using System.IO;
using System.Web;
using ServiceStack.Common.Utils;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
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
		static private readonly IHttpHandler NotFoundHttpHandler = null;
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

			var isAspNetHost = HttpListenerBase.Instance == null || HttpContext.Current != null;
			WebHostPhysicalPath = isAspNetHost
				? "~".MapHostAbsolutePath()
				: "~".MapAbsolutePath();

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

			NotFoundHttpHandler = string.IsNullOrEmpty(EndpointHost.Config.NotFoundRedirectPath)
				? (IHttpHandler)new NotFoundHttpHandler()
				: new RedirectHttpHandler { RelativeUrl = EndpointHost.Config.NotFoundRedirectPath };

			if (!string.IsNullOrEmpty(EndpointHost.Config.DefaultRedirectPath))
				DefaultHttpHandler = new RedirectHttpHandler { RelativeUrl = EndpointHost.Config.DefaultRedirectPath };

			if (DefaultHttpHandler == null && !string.IsNullOrEmpty(EndpointHost.Config.MetadataRedirectPath))
				DefaultHttpHandler = new RedirectHttpHandler { RelativeUrl = EndpointHost.Config.MetadataRedirectPath };

			if (DefaultHttpHandler == null)
				DefaultHttpHandler = NotFoundHttpHandler;
		}

		public IHttpHandler GetHandler(HttpContext context, string requestType, string url, string pathTranslated)
		{
			var reqInfo = ReturnRequestInfo(context);
			if (reqInfo != null) return reqInfo;

			var mode = EndpointHost.Config.ServiceStackHandlerFactoryPath;
			var pathInfo = context.Request.GetPathInfo();

			if (string.IsNullOrEmpty(pathInfo) || pathInfo == "/")
				return DefaultHttpHandler;

			if (mode != null && pathInfo.EndsWith(mode))
			{
				var requestPath = context.Request.Path.ToLower();
				if (requestPath == "/" + mode
					|| requestPath == mode
					|| requestPath == mode + "/")
				{
					if (context.Request.PhysicalPath != WebHostPhysicalPath
						|| !File.Exists(Path.Combine(context.Request.PhysicalPath, DefaultRootFileName ?? "")))
					{
						return new IndexPageHttpHandler();
					}
				}

				var okToServe = ShouldAllow(context.Request.FilePath);
				return okToServe ? DefaultHttpHandler : ForbiddenHttpHandler;
			}

			return GetHandlerForPathInfo(context.Request.HttpMethod, pathInfo)
				   ?? NotFoundHttpHandler;
		}

		/// <summary>
		/// If enabled, just returns the Request Info as it understa
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		private static RequestInfoHandler ReturnRequestInfo(HttpContext context)
		{
			if (EndpointHost.Config.DebugOnlyReturnRequestInfo)
			{
				var reqInfo = RequestInfoHandler.GetRequestInfo(
					new HttpRequestWrapper(typeof(RequestInfo).Name, context.Request));

				reqInfo.Host = EndpointHost.Config.DebugAspNetHostEnvironment + "_v" + Env.ServiceStackVersion + "_" + EndpointHost.Config.ServiceName;
				//reqInfo.FactoryUrl = url; //Just RawUrl without QueryString 
				//reqInfo.FactoryPathTranslated = pathTranslated; //Local path on filesystem
				reqInfo.Path = context.Request.Path;
				reqInfo.PathInfo = context.Request.PathInfo;
				reqInfo.ApplicationPath = context.Request.ApplicationPath;

				return new RequestInfoHandler { RequestInfo = reqInfo };
			}

			return null;
		}

		// no handler registered 
		// serve the file from the filesystem, restricting to a safelist of extensions
		private static bool ShouldAllow(string filePath)
		{
			var fileExt = Path.GetExtension(filePath);
			if (string.IsNullOrEmpty(fileExt)) return false;
			return EndpointHost.Config.AllowFileExtensions.Contains(fileExt.Substring(1));
		}

		// Entry point for HttpListener
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
				//return !IsIntegratedPipeline ? DefaultHttpHandler : new StaticFileHandler();
				return new StaticFileHandler();
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
				if (pathController == RequestInfoHandler.RestPath)
					return new RequestInfoHandler();

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

				case RequestInfoHandler.RestPath:
					return new RequestInfoHandler();

				default:

					string contentType;
					if (EndpointHost.ContentTypeFilter
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