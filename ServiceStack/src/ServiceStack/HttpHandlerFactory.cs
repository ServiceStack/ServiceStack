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

namespace ServiceStack;

public class HttpHandlerFactory : IHttpHandlerFactory
{
    public static string WebHostPhysicalPath;
    public static string DefaultRootFileName;

    public static IHttpHandler DefaultHttpHandler;
    public static RedirectHttpHandler NonRootModeDefaultHttpHandler;
    public static IHttpHandler ForbiddenHttpHandler;
    public static IHttpHandler NotFoundHttpHandler;
    public static NotFoundHttpHandler PassThruHttpHandler;
    public static IHttpHandler StaticFilesHandler = new StaticFileHandler();

    [ThreadStatic]
    public static string DebugLastHandlerArgs;

    internal static void Init()
    {
        try
        {
            var isIntegratedPipeline = false;
#if !NETCORE
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
            WebHostPhysicalPath = appHost.RootDirectory.RealPath;

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

            DefaultHttpHandler ??= NotFoundHttpHandler;

            var debugDefaultHandler = DefaultHttpHandler is RedirectHttpHandler defaultRedirectHandler
                ? defaultRedirectHandler.RelativeUrl
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

            PassThruHttpHandler = new NotFoundHttpHandler
            {
                WebHostPhysicalPath = WebHostPhysicalPath,
                WebHostUrl = config.WebHostUrl,
                DefaultRootFileName = DefaultRootFileName,
                DefaultHandler = debugDefaultHandler,
            };
            NotFoundHttpHandler = appHost.GetCustomErrorHttpHandler(HttpStatusCode.NotFound) ?? PassThruHttpHandler;
        }
        catch (Exception ex)
        {
            HostContext.AppHost.OnStartupException(ex, nameof(HttpHandlerFactory), nameof(Init));
        }
    }

    public static IHttpHandler InitHandler(IHttpHandler handler, IHttpRequest httpReq)
    {
        if (handler is IServiceStackHandler ssHandler)
            httpReq.OperationName = ssHandler.GetOperationName();

        var appHost = HostContext.AppHost;
        var shouldProfile = appHost.ShouldProfileRequest(httpReq);
        if (appHost.Container.Exists<IRequestLogger>() || shouldProfile)
        {
            httpReq.SetItem(Keywords.RequestDuration, System.Diagnostics.Stopwatch.GetTimestamp());
        }
        if (shouldProfile)
        {
            // https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/ActivityUserGuide.md
            var activity = new System.Diagnostics.Activity(Diagnostics.Activity.HttpBegin);
            activity.SetParentId(httpReq.GetTraceId());
            var id = Diagnostics.ServiceStack.WriteRequestBefore(httpReq);
            activity.AddTag(Diagnostics.Activity.OperationId, id);

            var userId = appHost.TryGetUserId(httpReq);
            if (userId != null)
                activity.AddTag(Diagnostics.Activity.UserId, userId);

            var feature = appHost.GetPlugin<ProfilingFeature>();
            var tag = feature?.TagResolver?.Invoke(httpReq);
            if (tag != null)
                activity.AddTag(Diagnostics.Activity.Tag, tag);

            httpReq.SetItem(Keywords.RequestActivity, activity);
            Diagnostics.ServiceStack.StartActivity(activity, new ServiceStackActivityArgs { Request = httpReq, Activity = activity });
        }

        return handler;
    }

#if !NETCORE
        // Entry point for ASP.NET
        public IHttpHandler GetHandler(HttpContext ctx, string requestType, string url, string pathTranslated) => 
            InitHandler(GetHandlerInternal(ctx, requestType, url, pathTranslated, out var httpReq), httpReq);

        internal IHttpHandler GetHandlerInternal(HttpContext ctx, string requestType, string url, string pathTranslated, out IHttpRequest httpReq)
        {
            var appHost = HostContext.AppHost;

            DebugLastHandlerArgs = requestType + "|" + url + "|" + pathTranslated;
            httpReq = new Host.AspNet.AspNetRequest(ctx.Request.RequestContext.HttpContext, url.SanitizedVirtualPath());

            foreach (var rawHttpHandler in appHost.RawHttpHandlersArray)
            {
                var handler = rawHttpHandler(httpReq);
                if (handler != null) 
                    return handler;
            }

            var pathInfo = httpReq.PathInfo;

            //WebDev Server auto requests '/default.aspx' so re-correct path to different default document
            var mode = appHost.Config.HandlerFactoryPath;
            if (mode == null && url is "/default.aspx" or "/Default.aspx")
                pathInfo = "/";

            //Default Request /
            if (string.IsNullOrEmpty(pathInfo) || pathInfo == "/")
            {
                RestPath matchesFallback = appHost.Config.FallbackRestPath?.Invoke(httpReq);
                if (matchesFallback == null || matchesFallback.Priority > 0 || 
                    (matchesFallback.MatchRule == null && !(matchesFallback.Priority < 0))) // is not targeted fallback
                {
                    //e.g. to Process View Engine requests
                    var catchAllHandler = appHost.GetCatchAllHandler(httpReq);
                    if (catchAllHandler != null) return catchAllHandler;
                }

                //If the fallback route can handle it, let it
                if (matchesFallback != null)
                {
                    var sanitizedPath = RestHandler.GetSanitizedPathInfo(pathInfo, out var contentType);
                    return new RestHandler { RestPath = matchesFallback, RequestName = matchesFallback.RequestType.GetOperationName(), ResponseContentType = contentType };
                }

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
    public static IHttpHandler GetHandler(IHttpRequest httpReq) =>
        InitHandler(GetHandlerInternal(httpReq), httpReq);

    internal static IHttpHandler GetHandlerInternal(IHttpRequest httpReq)
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
            RestPath matchesFallback = appHost.Config.FallbackRestPath?.Invoke(httpReq);
            if (matchesFallback == null || matchesFallback.Priority > 0 || 
                (matchesFallback.MatchRule == null && !(matchesFallback.Priority < 0))) // is not targeted fallback
            {
                //e.g. to Process View Engine requests
                var catchAllHandler = appHost.GetCatchAllHandler(httpReq);
                if (catchAllHandler != null) return catchAllHandler;
            }

            //If the fallback route can handle it, let it
            if (matchesFallback != null)
            {
                var sanitizedPath = RestHandler.GetSanitizedPathInfo(pathInfo, out var contentType);
                return new RestHandler { RestPath = matchesFallback, RequestName = matchesFallback.RequestType.GetOperationName(), ResponseContentType = contentType };
            }

            if (mode == null)
                return DefaultHttpHandler;

            if (DefaultRootFileName != null)
                return StaticFilesHandler;

            return NonRootModeDefaultHttpHandler;
        }

        return GetHandlerForPathInfo(httpReq, httpReq.GetPhysicalPath())
               ?? NotFoundHttpHandler;
    }

    public static IHttpHandler GetHandlerForPathInfo(IHttpRequest httpReq, string filePath)
    {
        var appHost = HostContext.AppHost;

        var pathInfo = httpReq.PathInfo;
        var httpMethod = httpReq.Verb;
            
        if (pathInfo.AsSpan().TrimStart('/').Length == 0) 
            return NotFoundHttpHandler;

        var restPath = RestHandler.FindMatchingRestPath(httpReq, out var contentType);
        if (restPath != null)
            return new RestHandler { RestPath = restPath, RequestName = restPath.RequestType.GetOperationName(), ResponseContentType = contentType };

        var catchAllHandler = appHost.GetCatchAllHandler(httpReq);
        if (catchAllHandler != null)
            return catchAllHandler;

        var fallbackHandler = appHost.GetFallbackHandler(httpReq);
        if (fallbackHandler != null)
            return fallbackHandler;

        return null;
    }

    public void ReleaseHandler(IHttpHandler handler)
    {
    }
}