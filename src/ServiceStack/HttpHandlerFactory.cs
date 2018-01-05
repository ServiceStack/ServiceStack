using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.Text;
using ServiceStack.VirtualPath;
using ServiceStack.Web;

namespace ServiceStack
{
    public class HttpHandlerFactory : IHttpHandlerFactory
    {
        public static string WebHostPhysicalPath;
        public static string DefaultRootFileName;

        private static IHttpHandler DefaultHttpHandler;
        private static RedirectHttpHandler NonRootModeDefaultHttpHandler;
        private static IHttpHandler ForbiddenHttpHandler;
        private static IHttpHandler NotFoundHttpHandler;
        private static readonly IHttpHandler StaticFilesHandler = new StaticFileHandler();

        [ThreadStatic]
        public static string DebugLastHandlerArgs;

        internal static void Init()
        {
            try
            {
                var isIntegratedPipeline = false;
#if !NETSTANDARD2_0
                //MONO doesn't implement this property
                var pi = typeof(HttpRuntime).GetProperty("UsingIntegratedPipeline");
                if (pi != null)
                {
                    isIntegratedPipeline = (bool) pi.GetGetMethod().Invoke(null, TypeConstants.EmptyObjectArray);
                }
#endif
                var appHost = HostContext.AppHost;
                var config = appHost.Config;

                var isAspNetHost = HostContext.IsAspNetHost;
                WebHostPhysicalPath = appHost.VirtualFileSources.RootDirectory.RealPath;

                //Apache+mod_mono treats path="servicestack*" as path="*" so takes over root path, so we need to serve matching resources
                var hostedAtRootPath = config.HandlerFactoryPath == null;

                //DefaultHttpHandler not supported in IntegratedPipeline mode
                if (!isIntegratedPipeline && isAspNetHost && !hostedAtRootPath && !Env.IsMono)
                    DefaultHttpHandler = new DefaultHttpHandler();

                var rootFiles = appHost.VirtualFileSources.GetRootFiles().ToList();
                foreach (var file in rootFiles)
                {
                    var fileNameLower = file.Name.ToLowerInvariant();
                    if (DefaultRootFileName == null && config.DefaultDocuments.Contains(fileNameLower))
                    {
                        //Can't serve Default.aspx pages so ignore and allow for next default document
                        if (!fileNameLower.EndsWith(".aspx"))
                        {
                            DefaultRootFileName = fileNameLower;
                            StaticFileHandler.SetDefaultFile(file.VirtualPath, file.ReadAllBytes(), file.LastModified);

                            if (DefaultHttpHandler == null)
                                DefaultHttpHandler = new StaticFileHandler(file);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(config.DefaultRedirectPath))
                {
                    DefaultHttpHandler = new RedirectHttpHandler { RelativeUrl = config.DefaultRedirectPath };
                    NonRootModeDefaultHttpHandler = new RedirectHttpHandler { RelativeUrl = config.DefaultRedirectPath };
                }

                if (DefaultHttpHandler == null && !string.IsNullOrEmpty(config.MetadataRedirectPath))
                {
                    DefaultHttpHandler = new RedirectHttpHandler { RelativeUrl = config.MetadataRedirectPath };
                    NonRootModeDefaultHttpHandler = new RedirectHttpHandler { RelativeUrl = config.MetadataRedirectPath };
                }

                if (DefaultHttpHandler == null)
                    DefaultHttpHandler = NotFoundHttpHandler;

                var debugDefaultHandler = DefaultHttpHandler is RedirectHttpHandler defaultRedirectHanlder
                    ? defaultRedirectHanlder.RelativeUrl
                    : typeof(DefaultHttpHandler).GetOperationName();

                ForbiddenHttpHandler = appHost.GetCustomErrorHttpHandler(HttpStatusCode.Forbidden);
                if (ForbiddenHttpHandler == null)
                {
                    ForbiddenHttpHandler = new ForbiddenHttpHandler
                    {
                        WebHostPhysicalPath = WebHostPhysicalPath,
                        WebHostUrl = config.WebHostUrl,
                        DefaultRootFileName = DefaultRootFileName,
                        DefaultHandler = debugDefaultHandler,
                    };
                }

                NotFoundHttpHandler = appHost.GetCustomErrorHttpHandler(HttpStatusCode.NotFound);
                if (NotFoundHttpHandler == null)
                {
                    NotFoundHttpHandler = new NotFoundHttpHandler
                    {
                        WebHostPhysicalPath = WebHostPhysicalPath,
                        WebHostUrl = config.WebHostUrl,
                        DefaultRootFileName = DefaultRootFileName,
                        DefaultHandler = debugDefaultHandler,
                    };
                }
            }
            catch (Exception ex)
            {
                HostContext.AppHost.OnStartupException(ex);
            }
        }

#if !NETSTANDARD2_0
        // Entry point for ASP.NET
        public IHttpHandler GetHandler(HttpContext ctx, string requestType, string url, string pathTranslated)
        {
            var appHost = HostContext.AppHost;

            DebugLastHandlerArgs = requestType + "|" + url + "|" + pathTranslated;
            var httpReq = new Host.AspNet.AspNetRequest(ctx.Request.RequestContext.HttpContext, url.SanitizedVirtualPath());

            foreach (var rawHttpHandler in appHost.RawHttpHandlersArray)
            {
                var handler = rawHttpHandler(httpReq);
                if (handler != null) 
                    return handler;
            }

            var pathInfo = httpReq.PathInfo;

            //WebDev Server auto requests '/default.aspx' so recorrect path to different default document
            var mode = appHost.Config.HandlerFactoryPath;
            if (mode == null && (url == "/default.aspx" || url == "/Default.aspx"))
                pathInfo = "/";

            //Default Request /
            if (string.IsNullOrEmpty(pathInfo) || pathInfo == "/")
            {
                //If the fallback route can handle it, let it
                var restPath = appHost.Config.FallbackRestPath?.Invoke(httpReq);
                if (restPath != null)
                {
                    var sanitizedPath = RestHandler.GetSanitizedPathInfo(pathInfo, out var contentType);
                    return new RestHandler { RestPath = restPath, RequestName = restPath.RequestType.GetOperationName(), ResponseContentType = contentType };
                }

                //e.g. CatchAllHandler to Process Markdown files
                var catchAllHandler = GetCatchAllHandlerIfAny(appHost, httpReq.HttpMethod, pathInfo, httpReq.GetPhysicalPath());
                if (catchAllHandler != null) return catchAllHandler;

                if (mode == null)
                    return DefaultHttpHandler;

                if (DefaultRootFileName != null)
                    return StaticFilesHandler;

                return NonRootModeDefaultHttpHandler;
            }

            return GetHandlerForPathInfo(httpReq, pathTranslated)
               ?? NotFoundHttpHandler;
        }
#endif

        // Entry point for HttpListener and .NET Core
        public static IHttpHandler GetHandler(IHttpRequest httpReq)
        {
            var appHost = HostContext.AppHost;

            foreach (var rawHttpHandler in appHost.RawHttpHandlersArray)
            {
                var handler = rawHttpHandler(httpReq);
                if (handler != null) 
                    return handler;
            }

            var mode = appHost.Config.HandlerFactoryPath;
            var pathInfo = httpReq.PathInfo;

            //Default Request /
            if (string.IsNullOrEmpty(pathInfo) || pathInfo == "/")
            {
                //If the fallback route can handle it, let it
                RestPath restPath = appHost.Config.FallbackRestPath?.Invoke(httpReq);
                if (restPath != null)
                {
                    var sanitizedPath = RestHandler.GetSanitizedPathInfo(pathInfo, out var contentType);
                    return new RestHandler { RestPath = restPath, RequestName = restPath.RequestType.GetOperationName(), ResponseContentType = contentType };
                }

                //e.g. CatchAllHandler to Process Markdown files
                var catchAllHandler = GetCatchAllHandlerIfAny(appHost, httpReq.HttpMethod, pathInfo, httpReq.GetPhysicalPath());
                if (catchAllHandler != null) return catchAllHandler;

                if (mode == null)
                    return DefaultHttpHandler;

                if (DefaultRootFileName != null)
                    return StaticFilesHandler;

                return NonRootModeDefaultHttpHandler;
            }

            return GetHandlerForPathInfo(httpReq, httpReq.GetPhysicalPath())
                   ?? NotFoundHttpHandler;
        }

        // no handler registered 
        // serve the file from the filesystem, restricting to a safelist of extensions
        public static bool ShouldAllow(string pathInfo)
        {
            if (string.IsNullOrEmpty(pathInfo) || pathInfo == "/")
                return true;

            var config = HostContext.Config;
            foreach (var path in config.ForbiddenPaths)
            {
                if (pathInfo.StartsWith(path))
                    return false;
            }
            
            var parts = pathInfo.SplitOnLast('.');
            if (parts.Length == 1 || string.IsNullOrEmpty(parts[1]))
                return false;

            var fileExt = parts[1];
            if (config.AllowFileExtensions.Contains(fileExt))
                return true;

            foreach (var pathGlob in config.AllowFilePaths)
            {
                if (pathInfo.GlobPath(pathGlob))
                    return true;
            }
            
            return false;
        }

        public static IHttpHandler GetHandlerForPathInfo(IHttpRequest httpReq, string filePath)
        {
            var appHost = HostContext.AppHost;

            var pathInfo = httpReq.PathInfo;
            var httpMethod = httpReq.Verb;
            
            var pathParts = pathInfo.TrimStart('/').Split('/');
            if (pathParts.Length == 0) return NotFoundHttpHandler;

            var restPath = RestHandler.FindMatchingRestPath(httpReq, out var contentType);
            if (restPath != null)
                return new RestHandler { RestPath = restPath, RequestName = restPath.RequestType.GetOperationName(), ResponseContentType = contentType };

            var catchAllHandler = GetCatchAllHandlerIfAny(appHost, httpMethod, pathInfo, filePath);
            if (catchAllHandler != null)
                return catchAllHandler;

            var isFile = httpReq.IsFile();
            var isDirectory = httpReq.IsDirectory();

            if (!isFile && !isDirectory && Env.IsMono)
                isDirectory = StaticFileHandler.MonoDirectoryExists(filePath, filePath.Substring(0, filePath.Length - pathInfo.Length));

            if (isFile || isDirectory)
            {
                //If pathInfo is for Directory try again with redirect including '/' suffix
                if (appHost.Config.RedirectDirectoriesToTrailingSlashes && isDirectory && !httpReq.OriginalPathInfo.EndsWith("/"))
                    return new RedirectHttpHandler { RelativeUrl = pathInfo + "/" };

                if (isDirectory)
                    return StaticFilesHandler;

                return ShouldAllow(pathInfo)
                    ? StaticFilesHandler
                    : ForbiddenHttpHandler;
            }

            if (appHost.Config.FallbackRestPath != null)
            {
                restPath = appHost.Config.FallbackRestPath(httpReq);
                if (restPath != null)
                    return new RestHandler { RestPath = restPath, RequestName = restPath.RequestType.GetOperationName(), ResponseContentType = contentType };
            }

            return null;
        }

        private static IHttpHandler GetCatchAllHandlerIfAny(ServiceStackHost appHost, string httpMethod, string pathInfo, string filePath)
        {
            foreach (var httpHandlerResolver in appHost.CatchAllHandlersArray)
            {
                var httpHandler = httpHandlerResolver(httpMethod, pathInfo, filePath);
                if (httpHandler != null)
                    return httpHandler;
            }

            return null;
        }

        public void ReleaseHandler(IHttpHandler handler)
        {
        }
    }
}