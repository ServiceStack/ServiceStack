using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Web;
using Funq;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.Host.HttpListener;
using ServiceStack.IO;
using ServiceStack.Metadata;
using ServiceStack.MiniProfiler;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public static class HostContext
    {
        public static RequestContext RequestContext
        {
            get { return RequestContext.Instance; }
        }

        public static ServiceStackHost AppHost
        {
            get { return ServiceStackHost.Instance; }
        }

        private static ServiceStackHost AssertAppHost()
        {
            if (ServiceStackHost.Instance == null)
                throw new InvalidOperationException("ServiceStackHost is not initialized.");
            return ServiceStackHost.Instance;
        }

        public static bool IsAspNetHost
        {
            get { return ServiceStackHost.Instance is AppHostBase; }
        }

        public static bool IsHttpListenerHost
        {
            get { return ServiceStackHost.Instance is HttpListenerBase; }
        }

        public static T TryResolve<T>()
        {
            return AssertAppHost().TryResolve<T>();
        }

        public static T Resolve<T>()
        {
            return AssertAppHost().Resolve<T>();
        }

        public static Container Container
        {
            get { return AssertAppHost().Container; }
        }

        public static ServiceController ServiceController
        {
            get { return AssertAppHost().ServiceController; }
        }

        public static MetadataPagesConfig MetadataPagesConfig
        {
            get { return AssertAppHost().MetadataPagesConfig; }
        }

        public static IContentTypes ContentTypes
        {
            get { return AssertAppHost().ContentTypes; }
        }

        public static HostConfig Config
        {
            get { return AssertAppHost().Config; }
        }

        public static ServiceMetadata Metadata
        {
            get { return AssertAppHost().Metadata; }
        }

        public static string ServiceName
        {
            get { return AssertAppHost().ServiceName; }
        }

        public static bool DebugMode
        {
            get { return Config.DebugMode; }
        }

        public static List<HttpHandlerResolverDelegate> CatchAllHandlers
        {
            get { return AssertAppHost().CatchAllHandlers; }
        }

        public static List<Action<IHttpRequest, IHttpResponse, object>> GlobalRequestFilters
        {
            get { return AssertAppHost().GlobalRequestFilters; }
        }

        public static List<Action<IHttpRequest, IHttpResponse, object>> GlobalResponseFilters
        {
            get { return AssertAppHost().GlobalResponseFilters; }
        }

        public static bool ApplyPreRequestFilters(IHttpRequest httpReq, IHttpResponse httpRes)
        {
            return AssertAppHost().ApplyPreRequestFilters(httpReq, httpRes);
        }

        public static bool ApplyRequestFilters(IHttpRequest httpReq, IHttpResponse httpRes, object requestDto)
        {
            return AssertAppHost().ApplyRequestFilters(httpReq, httpRes, requestDto);
        }

        public static bool ApplyResponseFilters(IHttpRequest httpReq, IHttpResponse httpRes, object response)
        {
            return AssertAppHost().ApplyResponseFilters(httpReq, httpRes, response);
        }

        public static IVirtualPathProvider VirtualPathProvider
        {
            get { return AssertAppHost().VirtualPathProvider; }
        }

        /// <summary>
        /// Call to signal the completion of a ServiceStack-handled Request
        /// </summary>
        internal static void CompleteRequest()
        {
            try
            {
                AssertAppHost().OnEndRequest();
            }
            catch (Exception ex) { }
        }

        public static IServiceRunner<TRequest> CreateServiceRunner<TRequest>(ActionContext actionContext)
        {
            return AssertAppHost().CreateServiceRunner<TRequest>(actionContext);
        }

        internal static object ExecuteService(
            object request, RequestAttributes requestAttrs, IHttpRequest httpReq, IHttpResponse httpRes)
        {
            using (Profiler.Current.Step("Execute Service"))
            {
                return AssertAppHost().ServiceController.Execute(request,
                    new HttpRequestContext(httpReq, httpRes, request, requestAttrs));
            }
        }

        public static T GetPlugin<T>() where T : class, IPlugin
        {
            return AssertAppHost().Plugins.FirstOrDefault(x => x is T) as T;
        }

        public static bool HasPlugin<T>() where T : class, IPlugin
        {
            return AssertAppHost().Plugins.FirstOrDefault(x => x is T) != null;
        }

        public static string GetAppConfigPath()
        {
            if (ServiceStackHost.Instance == null) return null;

            var configPath = "~/web.config".MapHostAbsolutePath();
            if (File.Exists(configPath))
                return configPath;

            configPath = "~/Web.config".MapHostAbsolutePath(); //*nix FS FTW!
            if (File.Exists(configPath))
                return configPath;

            var appHostDll = new FileInfo(ServiceStackHost.Instance.GetType().Assembly.Location).Name;
            configPath = "~/{0}.config".Fmt(appHostDll).MapAbsolutePath();
            return File.Exists(configPath) ? configPath : null;
        }

        public static void Release(object service)
        {
            if (ServiceStackHost.Instance != null)
            {
                ServiceStackHost.Instance.Release(service);
            }
            else
            {
                using (service as IDisposable) { }
            }
        }

        public static bool HasFeature(Feature feature)
        {
            return (feature & Config.EnableFeatures) == feature;
        }

        public static void AssertFeatures(Feature usesFeatures)
        {
            if (Config.EnableFeatures == Feature.All) return;

            if (!HasFeature(usesFeatures))
            {
                throw new UnauthorizedAccessException(
                    String.Format("'{0}' Features have been disabled by your administrator", usesFeatures));
            }
        }

        public static UnauthorizedAccessException UnauthorizedAccess(RequestAttributes requestAttrs)
        {
            return new UnauthorizedAccessException(
                "Request with '{0}' is not allowed".Fmt(requestAttrs));
        }

        public static void AssertContentType(string contentType)
        {
            if (Config.EnableFeatures == Feature.All) return;

            AssertFeatures(contentType.ToFeature());
        }

        public static bool HasAccessToMetadata(IHttpRequest httpReq, IHttpResponse httpRes)
        {
            if (!HasFeature(Feature.Metadata))
            {
                HandleErrorResponse(httpReq, httpRes, HttpStatusCode.Forbidden, "Metadata Not Available");
                return false;
            }

            if (Config.MetadataVisibility != RequestAttributes.Any)
            {
                var actualAttributes = httpReq.GetAttributes();
                if ((actualAttributes & Config.MetadataVisibility) != Config.MetadataVisibility)
                {
                    HandleErrorResponse(httpReq, httpRes, HttpStatusCode.Forbidden, "Metadata Not Visible");
                    return false;
                }
            }
            return true;
        }

        public static void HandleErrorResponse(IHttpRequest httpReq, IHttpResponse httpRes, HttpStatusCode errorStatus, string errorStatusDescription = null)
        {
            if (httpRes.IsClosed) return;

            httpRes.StatusDescription = errorStatusDescription;

            var handler = GetHandlerForErrorStatus(errorStatus);

            handler.ProcessRequest(httpReq, httpRes, httpReq.OperationName);
        }

        public static IServiceStackHttpHandler GetHandlerForErrorStatus(HttpStatusCode errorStatus)
        {
            var httpHandler = GetCustomErrorHandler(errorStatus);

            switch (errorStatus)
            {
                case HttpStatusCode.Forbidden:
                    return httpHandler ?? new ForbiddenHttpHandler();
                case HttpStatusCode.NotFound:
                    return httpHandler ?? new NotFoundHttpHandler();
            }

            if (Config.CustomHttpHandlers != null)
            {
                Config.CustomHttpHandlers.TryGetValue(HttpStatusCode.NotFound, out httpHandler);
            }

            return httpHandler ?? new NotFoundHttpHandler();
        }

        public static IServiceStackHttpHandler GetCustomErrorHandler(int errorStatusCode)
        {
            try
            {
                return GetCustomErrorHandler((HttpStatusCode)errorStatusCode);
            }
            catch
            {
                return null;
            }
        }

        public static IServiceStackHttpHandler GetCustomErrorHandler(HttpStatusCode errorStatus)
        {
            IServiceStackHttpHandler httpHandler = null;
            if (Config.CustomHttpHandlers != null)
            {
                Config.CustomHttpHandlers.TryGetValue(errorStatus, out httpHandler);
            }
            return httpHandler ?? Config.GlobalHtmlErrorHttpHandler;
        }

        public static IHttpHandler GetCustomErrorHttpHandler(HttpStatusCode errorStatus)
        {
            var ssHandler = GetCustomErrorHandler(errorStatus);
            if (ssHandler == null) return null;
            var httpHandler = ssHandler as IHttpHandler;
            return httpHandler ?? new ServiceStackHttpHandler(ssHandler);
        }

        public static bool HasValidAuthSecret(IHttpRequest req)
        {
            if (Config.AdminAuthSecret != null)
            {
                var authSecret = req.GetParam("authsecret");
                return authSecret == Config.AdminAuthSecret;
            }

            return false;
        }

        public static string ResolveAbsoluteUrl(string virtualPath, IHttpRequest httpReq)
        {
            return AssertAppHost().ResolveAbsoluteUrl(virtualPath, httpReq);
        }

        private static string defaultOperationNamespace;
        public static string DefaultOperationNamespace
        {
            get
            {
                if (defaultOperationNamespace == null)
                {
                    defaultOperationNamespace = GetDefaultNamespace();
                }
                return defaultOperationNamespace;
            }
            set
            {
                defaultOperationNamespace = value;
            }
        }

        public static string GetDefaultNamespace()
        {
            if (!String.IsNullOrEmpty(defaultOperationNamespace)) return null;

            foreach (var operationType in Metadata.RequestTypes)
            {
                var attrs = operationType.AllAttributes<DataContractAttribute>();

                if (attrs.Length <= 0) continue;

                var attr = attrs[0];
                if (String.IsNullOrEmpty(attr.Namespace)) continue;

                return attr.Namespace;
            }

            return null;
        }

        public static TimeSpan GetDefaultSessionExpiry()
        {
            return ServiceStackHost.Instance == null 
                ? SessionFeature.DefaultSessionExpiry 
                : ServiceStackHost.Instance.GetDefaultSessionExpiry();
        }

        public static object RaiseServiceException(IHttpRequest httpReq, object request, Exception ex)
        {
            return AssertAppHost().OnServiceException(httpReq, request, ex);
        }

        public static void RaiseUncaughtException(IHttpRequest httpReq, IHttpResponse httpRes, string operationName, Exception ex)
        {
            AssertAppHost().OnUncaughtException(httpReq, httpRes, operationName, ex);
        }

        /// <summary>
        /// Resolves and auto-wires a ServiceStack Service from a ASP.NET HttpContext.
        /// </summary>
        public static T ResolveService<T>(HttpContext httpCtx=null) where T : class, IRequiresRequestContext
        {
            var service = AssertAppHost().Container.Resolve<T>();
            if (service == null) return null;
            service.RequestContext = (httpCtx ?? HttpContext.Current).ToRequestContext();
            return service;
        }

        /// <summary>
        /// Resolves and auto-wires a ServiceStack Service from a HttpListenerContext.
        /// </summary>
        public static T ResolveService<T>(HttpListenerContext httpCtx) where T : class, IRequiresRequestContext
        {
            var service = AssertAppHost().Container.Resolve<T>();
            if (service == null) return null;
            service.RequestContext = httpCtx.ToRequestContext();
            return service;
        }

        /// <summary>
        /// Resolves and auto-wires a ServiceStack Service from a HttpListener Request and Response.
        /// </summary>
        public static T ResolveService<T>(HttpListenerRequest httpReq, HttpListenerResponse httpRes)
            where T : class, IRequiresRequestContext
        {
            return ResolveService<T>(httpReq.ToRequest(), httpRes.ToResponse());
        }

        /// <summary>
        /// Resolves and auto-wires a ServiceStack Service.
        /// </summary>
        public static T ResolveService<T>(IHttpRequest httpReq, IHttpResponse httpRes) where T : class, IRequiresRequestContext
        {
            var service = AssertAppHost().Container.Resolve<T>();
            if (service == null) return null;
            service.RequestContext = new HttpRequestContext(httpReq, httpRes, null);
            return service;
        }
    }
}