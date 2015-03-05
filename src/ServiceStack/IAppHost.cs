using System;
using System.Collections.Generic;
using System.Net;
using System.Web;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.Html;
using ServiceStack.IO;
using ServiceStack.Web;

namespace ServiceStack
{
    /// <summary>
    /// ASP.NET or HttpListener ServiceStack host
    /// </summary>
    public interface IAppHost : IResolver
    {
        /// <summary>
        /// Register dependency in AppHost IOC on Startup
        /// </summary>
        void Register<T>(T instance);

        /// <summary>
        /// AutoWired Registration of an interface with a concrete type in AppHost IOC on Startup.
        /// </summary>
        void RegisterAs<T, TAs>() where T : TAs;

        /// <summary>
        /// Allows the clean up for executed autowired services and filters.
        /// Calls directly after services and filters are executed.
        /// </summary>
        void Release(object instance);

        /// <summary>
        /// Called at the end of each request. Enables Request Scope.
        /// </summary>
        void OnEndRequest(IRequest request = null);

        /// <summary>
        /// Register user-defined custom routes.
        /// </summary>
        IServiceRoutes Routes { get; }

        /// <summary>
        /// Inferred Metadata available from existing services 
        /// </summary>
        ServiceMetadata Metadata { get; }

        /// <summary>
        /// Register custom ContentType serializers
        /// </summary>
        IContentTypes ContentTypes { get; }

        /// <summary>
        /// Add Request Filters, to be applied before the dto is deserialized
        /// </summary>
        List<Action<IRequest, IResponse>> PreRequestFilters { get; }

        /// <summary>
        /// Add Request Filters for HTTP Requests
        /// </summary>
        List<Action<IRequest, IResponse, object>> GlobalRequestFilters { get; }

        /// <summary>
        /// Add Response Filters for HTTP Responses
        /// </summary>
        List<Action<IRequest, IResponse, object>> GlobalResponseFilters { get; }

        /// <summary>
        /// Add Request Filters for MQ/TCP Requests
        /// </summary>
        List<Action<IRequest, IResponse, object>> GlobalMessageRequestFilters { get; }

        /// <summary>
        /// Add Response Filters for MQ/TCP Responses
        /// </summary>
        List<Action<IRequest, IResponse, object>> GlobalMessageResponseFilters { get; }

        /// <summary>
        /// Add alternative HTML View Engines
        /// </summary>
        List<IViewEngine> ViewEngines { get; }

        /// <summary>
        /// Provide an exception handler for unhandled exceptions
        /// </summary>
        List<HandleServiceExceptionDelegate> ServiceExceptionHandlers { get; }

        /// <summary>
        /// Provide an exception handler for un-caught exceptions
        /// </summary>
        List<HandleUncaughtExceptionDelegate> UncaughtExceptionHandlers { get; }

        /// <summary>
        /// Provide callbacks to be fired after the AppHost has finished initializing
        /// </summary>
        List<Action<IAppHost>> AfterInitCallbacks { get; }

        /// <summary>
        /// Provide callbacks to be fired when AppHost is being disposed
        /// </summary>
        List<Action<IAppHost>> OnDisposeCallbacks { get; }

        /// <summary>
        /// Skip the ServiceStack Request Pipeline and process the returned IHttpHandler instead
        /// </summary>
        List<Func<IHttpRequest, IHttpHandler>> RawHttpHandlers { get; }

        /// <summary>
        /// Provide a catch-all handler that doesn't match any routes
        /// </summary>
        List<HttpHandlerResolverDelegate> CatchAllHandlers { get; }

        /// <summary>
        /// Use a fall-back Error Handler for handling global errors
        /// </summary>
        IServiceStackHandler GlobalHtmlErrorHttpHandler { get; }

        /// <summary>
        /// Use a Custom Error Handler for handling specific error HttpStatusCodes
        /// </summary>
        Dictionary<HttpStatusCode, IServiceStackHandler> CustomErrorHttpHandlers { get; }

        /// <summary>
        /// Provide a custom model minder for a specific Request DTO
        /// </summary>
        Dictionary<Type, Func<IRequest, object>> RequestBinders { get; }

        /// <summary>
        /// The AppHost config
        /// </summary>
        HostConfig Config { get; }

        /// <summary>
        /// The AppHost AppSettings. Defaults to App or Web.config appSettings.
        /// </summary>
        IAppSettings AppSettings { get; }

        /// <summary>
        /// Register an Adhoc web service on Startup
        /// </summary>
        void RegisterService(Type serviceType, params string[] atRestPaths);

        /// <summary>
        /// List of pre-registered and user-defined plugins to be enabled in this AppHost
        /// </summary>
        List<IPlugin> Plugins { get; }

        /// <summary>
        /// Apply plugins to this AppHost
        /// </summary>
        void LoadPlugin(params IPlugin[] plugins);

        /// <summary>
        /// Virtual access to file resources
        /// </summary>
        IVirtualPathProvider VirtualPathProvider { get; set; }

        /// <summary>
        /// Create a service runner for IService actions
        /// </summary>
        IServiceRunner<TRequest> CreateServiceRunner<TRequest>(ActionContext actionContext);

        /// <summary>
        /// Resolve the absolute url for this request
        /// </summary>
        string ResolveAbsoluteUrl(string virtualPath, IRequest httpReq);

        /// <summary>
        /// Resolve localized text, returns itself by default.
        /// The Request is provided when exists.
        /// </summary>
        string ResolveLocalizedString(string text, IRequest request);
    }

    public interface IHasAppHost
    {
        IAppHost AppHost { get; }
    }
}