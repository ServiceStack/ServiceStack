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
        public static string WebHostPhysicalPath = null;
        public static string DefaultRootFileName = null;
        public static bool HostAutoRedirectsDirs = false;

        //internal static string ApplicationBaseUrl = null;
        private static IHttpHandler DefaultHttpHandler = null;
        private static RedirectHttpHandler NonRootModeDefaultHttpHandler = null;
        private static IHttpHandler ForbiddenHttpHandler = null;
        private static IHttpHandler NotFoundHttpHandler = null;
        private static readonly IHttpHandler StaticFilesHandler = new StaticFileHandler();
        private static bool IsIntegratedPipeline = false;

        [ThreadStatic]
        public static string DebugLastHandlerArgs;

        internal static void Init()
        {
            try
            {
#if !NETSTANDARD1_6
                //MONO doesn't implement this property
                var pi = typeof(HttpRuntime).GetProperty("UsingIntegratedPipeline");
                if (pi != null)
                {
                    IsIntegratedPipeline = (bool) pi.GetGetMethod().Invoke(null, TypeConstants.EmptyObjectArray);
                }
#endif
                var appHost = HostContext.AppHost;
                var config = appHost.Config;

                var isAspNetHost = HostContext.IsAspNetHost;
                WebHostPhysicalPath = appHost.VirtualFileSources.RootDirectory.RealPath;

                //Apache+mod_mono treats path="servicestack*" as path="*" so takes over root path, so we need to serve matching resources
                var hostedAtRootPath = config.HandlerFactoryPath == null;

                //DefaultHttpHandler not supported in IntegratedPipeline mode
                if (!IsIntegratedPipeline && isAspNetHost && !hostedAtRootPath && !Env.IsMono)
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

                var defaultRedirectHanlder = DefaultHttpHandler as RedirectHttpHandler;
                var debugDefaultHandler = defaultRedirectHanlder != null
                    ? defaultRedirectHanlder.RelativeUrl
                    : typeof(DefaultHttpHandler).GetOperationName();

                ForbiddenHttpHandler = appHost.GetCustomErrorHttpHandler(HttpStatusCode.Forbidden);
                if (ForbiddenHttpHandler == null)
                {
                    ForbiddenHttpHandler = new ForbiddenHttpHandler
                    {
                        IsIntegratedPipeline = IsIntegratedPipeline,
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
                        IsIntegratedPipeline = IsIntegratedPipeline,
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

#if !NETSTANDARD1_6
        // Entry point for ASP.NET
        public IHttpHandler GetHandler(HttpContext ctx, string requestType, string url, string pathTranslated)
        {
            var appHost = HostContext.AppHost;

            DebugLastHandlerArgs = requestType + "|" + url + "|" + pathTranslated;
            var httpReq = new ServiceStack.Host.AspNet.AspNetRequest(ctx.Request.RequestContext.HttpContext, url.SanitizedVirtualPath());

            foreach (var rawHttpHandler in appHost.RawHttpHandlers)
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
                if (appHost.Config.FallbackRestPath != null)
                {
                    string contentType;
                    var sanitizedPath = RestHandler.GetSanitizedPathInfo(pathInfo, out contentType);

                    var restPath = appHost.Config.FallbackRestPath(ctx.Request.HttpMethod, sanitizedPath, pathTranslated);
                    if (restPath != null)
                    {
                        return new RestHandler { RestPath = restPath, RequestName = restPath.RequestType.GetOperationName(), ResponseContentType = contentType };
                    }
                }

                //e.g. CatchAllHandler to Process Markdown files
                var catchAllHandler = GetCatchAllHandlerIfAny(httpReq.HttpMethod, pathInfo, httpReq.GetPhysicalPath());
                if (catchAllHandler != null) return catchAllHandler;

                if (mode == null)
                    return DefaultHttpHandler;

                if (DefaultRootFileName != null)
                    return StaticFilesHandler;

                return NonRootModeDefaultHttpHandler;
            }

            if (mode != null && pathInfo.EndsWith("/" + mode))
            {
                return ReturnDefaultHandler(httpReq);
            }

            return GetHandlerForPathInfo(httpReq, pathTranslated)
               ?? NotFoundHttpHandler;
        }
#endif
        public static string GetBaseUrl()
        {
            return HostContext.Config.WebHostUrl;
        }

        // Entry point for HttpListener and .NET Core
        public static IHttpHandler GetHandler(IHttpRequest httpReq)
        {
            var appHost = HostContext.AppHost;

            foreach (var rawHttpHandler in appHost.RawHttpHandlers)
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
                if (appHost.Config.FallbackRestPath != null)
                {
                    string contentType;
                    var sanitizedPath = RestHandler.GetSanitizedPathInfo(pathInfo, out contentType);

                    var restPath = appHost.Config.FallbackRestPath(httpReq.HttpMethod, sanitizedPath, httpReq.GetPhysicalPath());
                    if (restPath != null)
                    {
                        return new RestHandler { RestPath = restPath, RequestName = restPath.RequestType.GetOperationName(), ResponseContentType = contentType };
                    }
                }

                //e.g. CatchAllHandler to Process Markdown files
                var catchAllHandler = GetCatchAllHandlerIfAny(httpReq.HttpMethod, pathInfo, httpReq.GetPhysicalPath());
                if (catchAllHandler != null) return catchAllHandler;

                if (mode == null)
                    return DefaultHttpHandler;

                if (DefaultRootFileName != null)
                    return StaticFilesHandler;

                return NonRootModeDefaultHttpHandler;
            }

            if (mode != null && pathInfo.EndsWith("/" + mode))
                return ReturnDefaultHandler(httpReq);

            return GetHandlerForPathInfo(httpReq, httpReq.GetPhysicalPath())
                   ?? NotFoundHttpHandler;
        }

        private static IHttpHandler ReturnDefaultHandler(IHttpRequest httpReq)
        {
            var pathProvider = HostContext.VirtualFileSources;

            var defaultDoc = pathProvider.GetFile(DefaultRootFileName ?? "");
            if (httpReq.GetPhysicalPath() != WebHostPhysicalPath
                || defaultDoc == null)
            {
                return new IndexPageHttpHandler();
            }

            var okToServe = ShouldAllow(httpReq.GetPhysicalPath());
            return okToServe ? DefaultHttpHandler : ForbiddenHttpHandler;
        }

        // no handler registered 
        // serve the file from the filesystem, restricting to a safelist of extensions
        public static bool ShouldAllow(string filePath)
        {
            var parts = filePath.SplitOnLast('.');
            if (parts.Length == 1 || string.IsNullOrEmpty(parts[1]))
                return false;

            var fileExt = parts[1];
            if (HostContext.Config.AllowFileExtensions.Contains(fileExt))
                return true;

            foreach (var pathGlob in HostContext.Config.AllowFilePaths)
            {
                if (filePath.GlobPath(pathGlob))
                    return true;
            }
            return false;
        }

        public static IHttpHandler GetHandlerForPathInfo(IRequest httpReq, string filePath)
        {
            var appHost = HostContext.AppHost;

            var pathInfo = httpReq.PathInfo;
            var isFile = httpReq.IsFile();
            var isDirectory = !isFile && httpReq.IsDirectory();

            if (!isDirectory && Env.IsMono)
                isDirectory = StaticFileHandler.MonoDirectoryExists(filePath, filePath.Substring(0, filePath.Length - pathInfo.Length));

            var httpMethod = httpReq.Verb;
            
            var pathParts = pathInfo.TrimStart('/').Split('/');
            if (pathParts.Length == 0) return NotFoundHttpHandler;

            string contentType;
            var restPath = RestHandler.FindMatchingRestPath(httpMethod, pathInfo, out contentType);
            if (restPath != null)
                return new RestHandler { RestPath = restPath, RequestName = restPath.RequestType.GetOperationName(), ResponseContentType = contentType };

            if (isFile || isDirectory)
            {
                //If pathInfo is for Directory try again with redirect including '/' suffix
                if (appHost.Config.RedirectDirectoriesToTrailingSlashes && isDirectory && !HostAutoRedirectsDirs && !httpReq.OriginalPathInfo.EndsWith("/"))
                    return new RedirectHttpHandler { RelativeUrl = pathInfo + "/" };

                var catchAllHandler = GetCatchAllHandlerIfAny(httpMethod, pathInfo, filePath);
                if (catchAllHandler != null) 
                    return catchAllHandler;

                if (isFile)
                    return StaticFilesHandler;

                return ShouldAllow(pathInfo)
                    ? StaticFilesHandler
                    : ForbiddenHttpHandler;
            }

            var handler = GetCatchAllHandlerIfAny(httpMethod, pathInfo, filePath);
            if (handler != null) return handler;

            if (appHost.Config.FallbackRestPath != null)
            {
                restPath = appHost.Config.FallbackRestPath(httpMethod, pathInfo, filePath);
                if (restPath != null)
                    return new RestHandler { RestPath = restPath, RequestName = restPath.RequestType.GetOperationName(), ResponseContentType = contentType };
            }

            return null;
        }

        private static IHttpHandler GetCatchAllHandlerIfAny(string httpMethod, string pathInfo, string filePath)
        {
            foreach (var httpHandlerResolver in HostContext.CatchAllHandlers)
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