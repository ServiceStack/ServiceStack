using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Web;
using ServiceStack.Common;
using ServiceStack.Common.Utils;
using ServiceStack.MiniProfiler.UI;
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
		static private string ApplicationBaseUrl = null;
		static private readonly IHttpHandler DefaultHttpHandler = null;
		static private readonly RedirectHttpHandler NonRootModeDefaultHttpHandler = null;
		static private readonly IHttpHandler ForbiddenHttpHandler = null;
		static private readonly IHttpHandler NotFoundHttpHandler = null;
		static private readonly IHttpHandler StaticFileHandler = new StaticFileHandler();
		private static readonly bool IsIntegratedPipeline = false;
		private static readonly bool ServeDefaultHandler = false;
		private static Func<IHttpRequest, IHttpHandler>[] RawHttpHandlers;

		[ThreadStatic]
		public static string DebugLastHandlerArgs;

		static ServiceStackHttpHandlerFactory()
		{
			//MONO doesn't implement this property
			var pi = typeof(HttpRuntime).GetProperty("UsingIntegratedPipeline");
			if (pi != null)
			{
				IsIntegratedPipeline = (bool)pi.GetGetMethod().Invoke(null, new object[0]);
			}

			if (EndpointHost.Config == null)
			{
				throw new ConfigurationErrorsException(
					"ServiceStack: AppHost does not exist or has not been initialized. "
					+ "Make sure you have created an AppHost and started it with 'new AppHost().Init();' in your Global.asax Application_Start()",
					new ArgumentNullException("EndpointHost.Config"));
			}

			var isAspNetHost = HttpListenerBase.Instance == null || HttpContext.Current != null;
			WebHostPhysicalPath = EndpointHost.Config.WebHostPhysicalPath;

			//Apache+mod_mono treats path="servicestack*" as path="*" so takes over root path, so we need to serve matching resources
			var hostedAtRootPath = EndpointHost.Config.ServiceStackHandlerFactoryPath == null;

			//DefaultHttpHandler not supported in IntegratedPipeline mode
			if (!IsIntegratedPipeline && isAspNetHost && !hostedAtRootPath && !Env.IsMono)
				DefaultHttpHandler = new DefaultHttpHandler();

			ServeDefaultHandler = hostedAtRootPath || Env.IsMono;
			if (ServeDefaultHandler)
			{
				foreach (var filePath in Directory.GetFiles(WebHostPhysicalPath))
				{
					var fileNameLower = Path.GetFileName(filePath).ToLower();
					if (DefaultRootFileName == null && EndpointHost.Config.DefaultDocuments.Contains(fileNameLower))
					{
						//Can't serve Default.aspx pages when hostedAtRootPath so ignore and allow for next default document
						if (!(hostedAtRootPath && fileNameLower.EndsWith(".aspx")))
						{
							DefaultRootFileName = fileNameLower;
							((StaticFileHandler)StaticFileHandler).SetDefaultFile(filePath);

							if (DefaultHttpHandler == null)
								DefaultHttpHandler = new RedirectHttpHandler { RelativeUrl = DefaultRootFileName };
						}
					}
					WebHostRootFileNames.Add(Path.GetFileName(fileNameLower));
				}
				foreach (var dirName in Directory.GetDirectories(WebHostPhysicalPath))
				{
					var dirNameLower = Path.GetFileName(dirName).ToLower();
					WebHostRootFileNames.Add(Path.GetFileName(dirNameLower));
				}
			}

			if (!string.IsNullOrEmpty(EndpointHost.Config.DefaultRedirectPath))
				DefaultHttpHandler = new RedirectHttpHandler { RelativeUrl = EndpointHost.Config.DefaultRedirectPath };

			if (DefaultHttpHandler == null && !string.IsNullOrEmpty(EndpointHost.Config.MetadataRedirectPath))
				DefaultHttpHandler = new RedirectHttpHandler { RelativeUrl = EndpointHost.Config.MetadataRedirectPath };

			if (!string.IsNullOrEmpty(EndpointHost.Config.MetadataRedirectPath))
				NonRootModeDefaultHttpHandler = new RedirectHttpHandler { RelativeUrl = EndpointHost.Config.MetadataRedirectPath };

			if (DefaultHttpHandler == null)
				DefaultHttpHandler = NotFoundHttpHandler;

			var defaultRedirectHanlder = DefaultHttpHandler as RedirectHttpHandler;
			var debugDefaultHandler = defaultRedirectHanlder != null
				? defaultRedirectHanlder.RelativeUrl
				: typeof(DefaultHttpHandler).Name;

			SetApplicationBaseUrl(EndpointHost.Config.WebHostUrl);

			var httpHandlers = EndpointHost.Config.CustomHttpHandlers;

			httpHandlers.TryGetValue(HttpStatusCode.Forbidden, out ForbiddenHttpHandler);
			if (ForbiddenHttpHandler == null)
			{
				ForbiddenHttpHandler = new ForbiddenHttpHandler {
					IsIntegratedPipeline = IsIntegratedPipeline,
					WebHostPhysicalPath = WebHostPhysicalPath,
					WebHostRootFileNames = WebHostRootFileNames,
					ApplicationBaseUrl = ApplicationBaseUrl,
					DefaultRootFileName = DefaultRootFileName,
					DefaultHandler = debugDefaultHandler,
				};
			}

			httpHandlers.TryGetValue(HttpStatusCode.NotFound, out NotFoundHttpHandler);
			if (NotFoundHttpHandler == null)
			{
				NotFoundHttpHandler = new NotFoundHttpHandler {
					IsIntegratedPipeline = IsIntegratedPipeline,
					WebHostPhysicalPath = WebHostPhysicalPath,
					WebHostRootFileNames = WebHostRootFileNames,
					ApplicationBaseUrl = ApplicationBaseUrl,
					DefaultRootFileName = DefaultRootFileName,
					DefaultHandler = debugDefaultHandler,
				};
			}

			var rawHandlers = EndpointHost.Config.RawHttpHandlers;
			rawHandlers.Add(ReturnRequestInfo);
			rawHandlers.Add(MiniProfilerHandler.MatchesRequest);
			RawHttpHandlers = rawHandlers.ToArray();
		}

		// Entry point for ASP.NET
		public IHttpHandler GetHandler(HttpContext context, string requestType, string url, string pathTranslated)
		{
			DebugLastHandlerArgs = requestType + "|" + url + "|" + pathTranslated;
			var httpReq = new HttpRequestWrapper(pathTranslated, context.Request);
			foreach (var rawHttpHandler in RawHttpHandlers)
			{
				var reqInfo = rawHttpHandler(httpReq);
				if (reqInfo != null) return reqInfo;
			}

			var mode = EndpointHost.Config.ServiceStackHandlerFactoryPath;
			var pathInfo = context.Request.GetPathInfo();

			//WebDev Server auto requests '/default.aspx' so recorrect path to different default document
			if (mode == null && (url == "/default.aspx" || url == "/Default.aspx"))
				pathInfo = "/";

            //Default Request /
            if (string.IsNullOrEmpty(pathInfo) || pathInfo == "/")
			{
				//Exception calling context.Request.Url on Apache+mod_mono
				var absoluteUrl = Env.IsMono ? url.ToParentPath() : context.Request.GetApplicationUrl();
				if (ApplicationBaseUrl == null)
					SetApplicationBaseUrl(absoluteUrl);

                //e.g. CatchAllHandler to Process Markdown files
                var catchAllHandler = GetCatchAllHandlerIfAny(httpReq.HttpMethod, pathInfo, httpReq.GetPhysicalPath());
                if (catchAllHandler != null) return catchAllHandler;

				return ServeDefaultHandler ? DefaultHttpHandler : NonRootModeDefaultHttpHandler;
			}

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

			return GetHandlerForPathInfo(
				context.Request.HttpMethod, pathInfo, context.Request.FilePath, pathTranslated)
				   ?? NotFoundHttpHandler;
		}

		private static void SetApplicationBaseUrl(string absoluteUrl)
		{
			if (absoluteUrl == null) return;

			ApplicationBaseUrl = absoluteUrl;

			var defaultRedirectUrl = DefaultHttpHandler as RedirectHttpHandler;
			if (defaultRedirectUrl != null && defaultRedirectUrl.AbsoluteUrl == null)
				defaultRedirectUrl.AbsoluteUrl = ApplicationBaseUrl.CombineWith(
				defaultRedirectUrl.RelativeUrl);

			if (NonRootModeDefaultHttpHandler != null && NonRootModeDefaultHttpHandler.AbsoluteUrl == null)
				NonRootModeDefaultHttpHandler.AbsoluteUrl = ApplicationBaseUrl.CombineWith(
				NonRootModeDefaultHttpHandler.RelativeUrl);
		}

		public static string GetBaseUrl()
		{
			return EndpointHost.Config.WebHostUrl ?? ApplicationBaseUrl;
		}

		// Entry point for HttpListener
		public static IHttpHandler GetHandler(IHttpRequest httpReq)
		{
			foreach (var rawHttpHandler in RawHttpHandlers)
			{
				var reqInfo = rawHttpHandler(httpReq);
				if (reqInfo != null) return reqInfo;
			}

			var mode = EndpointHost.Config.ServiceStackHandlerFactoryPath;
			var pathInfo = httpReq.PathInfo;

            //Default Request /
			if (string.IsNullOrEmpty(pathInfo) || pathInfo == "/")
			{
				if (ApplicationBaseUrl == null)
					SetApplicationBaseUrl(httpReq.GetPathUrl());

                //e.g. CatchAllHandler to Process Markdown files
                var catchAllHandler = GetCatchAllHandlerIfAny(httpReq.HttpMethod, pathInfo, httpReq.GetPhysicalPath());
                if (catchAllHandler != null) return catchAllHandler;

				return ServeDefaultHandler ? DefaultHttpHandler : NonRootModeDefaultHttpHandler;
			}

			if (mode != null && pathInfo.EndsWith(mode))
			{
				var requestPath = pathInfo;
				if (requestPath == "/" + mode
					|| requestPath == mode
					|| requestPath == mode + "/")
				{
					//TODO: write test for this
					if (httpReq.GetPhysicalPath() != WebHostPhysicalPath
						|| !File.Exists(Path.Combine(httpReq.ApplicationFilePath, DefaultRootFileName ?? "")))
					{
						return new IndexPageHttpHandler();
					}
				}

				var okToServe = ShouldAllow(httpReq.GetPhysicalPath());
				return okToServe ? DefaultHttpHandler : ForbiddenHttpHandler;
			}

			return GetHandlerForPathInfo(httpReq.HttpMethod, pathInfo, pathInfo, httpReq.GetPhysicalPath())
				   ?? NotFoundHttpHandler;
		}


		/// <summary>
		/// If enabled, just returns the Request Info as it understands
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		private static IHttpHandler ReturnRequestInfo(HttpRequest request)
		{
			if (EndpointHost.Config.DebugOnlyReturnRequestInfo)
			{
				var reqInfo = RequestInfoHandler.GetRequestInfo(
					new HttpRequestWrapper(typeof(RequestInfo).Name, request));

				reqInfo.Host = EndpointHost.Config.DebugAspNetHostEnvironment + "_v" + Env.ServiceStackVersion + "_" + EndpointHost.Config.ServiceName;
				//reqInfo.FactoryUrl = url; //Just RawUrl without QueryString 
				//reqInfo.FactoryPathTranslated = pathTranslated; //Local path on filesystem
				reqInfo.PathInfo = request.PathInfo;
				reqInfo.Path = request.Path;
				reqInfo.ApplicationPath = request.ApplicationPath;

				return new RequestInfoHandler { RequestInfo = reqInfo };
			}

			return null;
		}

		private static IHttpHandler ReturnRequestInfo(IHttpRequest httpReq)
		{
			if (EndpointHost.Config.DebugOnlyReturnRequestInfo)
			{
				var reqInfo = RequestInfoHandler.GetRequestInfo(httpReq);

				reqInfo.Host = EndpointHost.Config.DebugHttpListenerHostEnvironment + "_v" + Env.ServiceStackVersion + "_" + EndpointHost.Config.ServiceName;
				reqInfo.PathInfo = httpReq.PathInfo;
				reqInfo.Path = httpReq.GetPathUrl();

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

		public static IHttpHandler GetHandlerForPathInfo(string httpMethod, string pathInfo, string requestPath, string filePath)
		{
			var pathParts = pathInfo.TrimStart('/').Split('/');
			if (pathParts.Length == 0) return NotFoundHttpHandler;

			var handler = GetHandlerForPathParts(pathParts);
			if (handler != null) return handler;

			var existingFile = pathParts[0].ToLower();
			if (WebHostRootFileNames.Contains(existingFile))
			{
				//e.g. CatchAllHandler to Process Markdown files
				var catchAllHandler = GetCatchAllHandlerIfAny(httpMethod, pathInfo, filePath);
				if (catchAllHandler != null) return catchAllHandler;

                var fileExt = Path.GetExtension(filePath);
                if (string.IsNullOrEmpty(fileExt)) return NotFoundHttpHandler;

				return ShouldAllow(requestPath) ? StaticFileHandler : ForbiddenHttpHandler;
			}

			var restPath = RestHandler.FindMatchingRestPath(httpMethod, pathInfo);
			if (restPath != null)
				return new RestHandler { RestPath = restPath, RequestName = restPath.RequestType.Name };

			return GetCatchAllHandlerIfAny(httpMethod, pathInfo, filePath);
		}

		private static IHttpHandler GetCatchAllHandlerIfAny(string httpMethod, string pathInfo, string filePath)
		{
			if (EndpointHost.CatchAllHandlers != null)
			{
				foreach (var httpHandlerResolver in EndpointHost.CatchAllHandlers)
				{
					var httpHandler = httpHandlerResolver(httpMethod, pathInfo, filePath);
					if (httpHandler != null)
						return httpHandler;
				}
			}

			return null;
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
						var feature = Common.Web.ContentType.GetFeature(contentType);
						if (feature == Feature.None) feature = Feature.CustomFormat;

						var format = Common.Web.ContentType.GetContentFormat(contentType);
						if (pathAction == "syncreply")
							return new GenericHandler(contentType, EndpointAttributes.SyncReply, feature) {
								RequestName = requestName
							};
						if (pathAction == "asynconeway")
							return new GenericHandler(contentType, EndpointAttributes.AsyncOneWay, feature) {
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