using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Web;
using Funq;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Host.HttpListener;
using ServiceStack.IO;
using ServiceStack.Metadata;
using ServiceStack.MiniProfiler;
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
                throw new ConfigurationErrorsException(
                    "ServiceStack: AppHost does not exist or has not been initialized. " +
                    "Make sure you have created an AppHost and started it with 'new AppHost().Init();' " +
                    " in your Global.asax Application_Start() or alternative Application StartUp");

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

        public static IAppSettings AppSettings
        {
            get { return AssertAppHost().AppSettings; }
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

        public static List<Func<IHttpRequest, IHttpHandler>> RawHttpHandlers
        {
            get { return AssertAppHost().RawHttpHandlers; }
        }

        public static List<Action<IRequest, IResponse, object>> GlobalRequestFilters
        {
            get { return AssertAppHost().GlobalRequestFilters; }
        }

        public static List<Action<IRequest, IResponse, object>> GlobalResponseFilters
        {
            get { return AssertAppHost().GlobalResponseFilters; }
        }

        public static List<Action<IRequest, IResponse, object>> GlobalMessageRequestFilters
        {
            get { return AssertAppHost().GlobalMessageRequestFilters; }
        }

        public static List<Action<IRequest, IResponse, object>> GlobalMessageResponseFilters
        {
            get { return AssertAppHost().GlobalMessageResponseFilters; }
        }

        public static bool ApplyCustomHandlerRequestFilters(IRequest httpReq, IResponse httpRes)
        {
            return AssertAppHost().ApplyCustomHandlerRequestFilters(httpReq, httpRes);
        }

        public static bool ApplyPreRequestFilters(IRequest httpReq, IResponse httpRes)
        {
            return AssertAppHost().ApplyPreRequestFilters(httpReq, httpRes);
        }

        public static bool ApplyRequestFilters(IRequest httpReq, IResponse httpRes, object requestDto)
        {
            return AssertAppHost().ApplyRequestFilters(httpReq, httpRes, requestDto);
        }

        public static bool ApplyMessageResponseFilters(IRequest req, IResponse res, object response)
        {
            return AssertAppHost().ApplyMessageResponseFilters(req, res, response);
        }

        public static bool ApplyMessageRequestFilters(IRequest req, IResponse res, object requestDto)
        {
            return AssertAppHost().ApplyMessageRequestFilters(req, res, requestDto);
        }

        public static bool ApplyResponseFilters(IRequest httpReq, IResponse httpRes, object response)
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

        internal static object ExecuteService(object request, IRequest httpReq)
        {
            using (Profiler.Current.Step("Execute Service"))
            {
                return AssertAppHost().ServiceController.Execute(request, httpReq);
            }
        }

        public static T GetPlugin<T>() where T : class, IPlugin
        {
            var appHost = AppHost;
            return appHost == null ? default(T) : appHost.GetPlugin<T>();
        }

        public static bool HasPlugin<T>() where T : class, IPlugin
        {
            var appHost = AppHost;
            return appHost != null && appHost.HasPlugin<T>();
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

        public static UnauthorizedAccessException UnauthorizedAccess(RequestAttributes requestAttrs)
        {
            return new UnauthorizedAccessException(
                "Request with '{0}' is not allowed".Fmt(requestAttrs));
        }

        public static string ResolveLocalizedString(string text, IRequest request=null)
        {
            return AssertAppHost().ResolveLocalizedString(text, request);
        }

        public static string ResolveAbsoluteUrl(string virtualPath, IRequest httpReq)
        {
            return AssertAppHost().ResolveAbsoluteUrl(virtualPath, httpReq);
        }

        public static string ResolvePhysicalPath(string virtualPath, IRequest httpReq)
        {
            return AssertAppHost().ResolvePhysicalPath(virtualPath, httpReq);
        }

        public static IVirtualFile ResolveVirtualFile(string virtualPath, IRequest httpReq)
        {
            return AssertAppHost().ResolveVirtualFile(virtualPath, httpReq);
        }

        public static IVirtualDirectory ResolveVirtualDirectory(string virtualPath, IRequest httpReq)
        {
            return AssertAppHost().ResolveVirtualDirectory(virtualPath, httpReq);
        }

        public static IVirtualNode ResolveVirtualNode(string virtualPath, IRequest httpReq)
        {
            return AssertAppHost().ResolveVirtualNode(virtualPath, httpReq);
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

        public static object RaiseServiceException(IRequest httpReq, object request, Exception ex)
        {
            return AssertAppHost().OnServiceException(httpReq, request, ex);
        }

        public static void RaiseUncaughtException(IRequest httpReq, IResponse httpRes, string operationName, Exception ex)
        {
            AssertAppHost().OnUncaughtException(httpReq, httpRes, operationName, ex);
        }

        /// <summary>
        /// Resolves and auto-wires a ServiceStack Service from a ASP.NET HttpContext.
        /// </summary>
        public static T ResolveService<T>(HttpContextBase httpCtx=null) where T : class, IRequiresRequest
        {
            var service = AssertAppHost().Container.Resolve<T>();
            if (service == null) return null;
            service.Request = httpCtx != null ? httpCtx.ToRequest() : GetCurrentRequest();
            return service;
        }

        /// <summary>
        /// Resolves and auto-wires a ServiceStack Service from a HttpListenerContext.
        /// </summary>
        public static T ResolveService<T>(HttpListenerContext httpCtx) where T : class, IRequiresRequest
        {
            var service = AssertAppHost().Container.Resolve<T>();
            if (service == null) return null;
            service.Request = httpCtx.ToRequest();
            return service;
        }

        /// <summary>
        /// Resolves and auto-wires a ServiceStack Service.
        /// </summary>
        public static T ResolveService<T>(IHttpRequest httpReq) where T : class, IRequiresRequest
        {
            var service = AssertAppHost().Container.Resolve<T>();
            if (service == null) return null;
            service.Request = httpReq;
            return service;
        }

        public static bool HasValidAuthSecret(IRequest httpReq)
        {
            return AssertAppHost().HasValidAuthSecret(httpReq);
        }

        public static bool HasFeature(Feature feature)
        {
            return AssertAppHost().HasFeature(feature);
        }

        public static void OnExceptionTypeFilter(Exception exception, ResponseStatus responseStatus)
        {
            AssertAppHost().OnExceptionTypeFilter(exception, responseStatus);
        }

        public static IRequest GetCurrentRequest()
        {
            return AssertAppHost().GetCurrentRequest();
        }
    }
}