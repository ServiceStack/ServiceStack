using System;
using System.Collections.Generic;
using Funq;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.Html;
using ServiceStack.MiniProfiler;
using ServiceStack.ServiceHost;
using ServiceStack.VirtualPath;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Formats;
using ServiceStack.WebHost.Endpoints.Support;
using ServiceStack.WebHost.Endpoints.Utils;

namespace ServiceStack.WebHost.Endpoints
{
	/// <summary>
	/// Responsible for access to the default <see cref="EndpointHost"/>.  
	/// </summary>
	public class EndpointHost
	{
		private static readonly EndpointHostInstance _singletonInstance = new EndpointHostInstance();

		/// <summary>
		/// Do not expose.
		/// </summary>
		internal static EndpointHostInstance SingletonInstance
		{
			get { return _singletonInstance; }
		}

		/// <summary>
		/// Use this instance for all access to this class.
		/// </summary>
		internal static EndpointHostInstance Instance 
		{ 
			get 
			{
				var ts = _threadSpecificInstance;
				if (ts != null) return ts;
				else return _singletonInstance;
			} 
		}

		[ThreadStatic]
		private static EndpointHostInstance _threadSpecificInstance;

		internal static IDisposable SetThreadSpecificHost(EndpointHostInstance useInstance)
		{
			_threadSpecificInstance = useInstance;
			return new ThreadCleanup();
		}

		private class ThreadCleanup : IDisposable
		{
			public void Dispose()
			{
				EndpointHost._threadSpecificInstance = null;	
			}
		}

		private readonly static object _syncRoot = new object();

		/// <summary>
		/// Creates a new <see cref="EndpointHostInstance"/> to run as a seperate instance. 
		/// </summary>
		/// <param name="name">The name of the endpoint to create.</param>
		/// <returns>Returns the instance.</returns>
		/// <remarks>This method is thread safe.</remarks>
		internal static EndpointHostInstance Create(IAppHost appHost)
		{
			return EndpointHostInstance.CreateInstance(appHost);
		}

		public static ServiceOperations ServiceOperations { get { return Instance.ServiceOperations; }  }
		public static ServiceOperations AllServiceOperations { get { return Instance.AllServiceOperations; } }

		public static IAppHost AppHost { get { return Instance != null ? Instance.AppHost: null; } internal set { Instance.AppHost = value; } }

		public static IContentTypeFilter ContentTypeFilter { get { return Instance.ContentTypeFilter; } set { Instance.ContentTypeFilter = value; } }

		public static List<Action<IHttpRequest, IHttpResponse>> RawRequestFilters { get { return Instance.RawRequestFilters; } }

		public static List<Action<IHttpRequest, IHttpResponse, object>> RequestFilters { get { return Instance.RequestFilters; } }

		public static List<Action<IHttpRequest, IHttpResponse, object>> ResponseFilters { get { return Instance.ResponseFilters; } }

		public static List<IViewEngine> ViewEngines { get { return Instance.ViewEngines; } set { Instance.ViewEngines = value; } }

		public static HandleUncaughtExceptionDelegate ExceptionHandler { get { return Instance.ExceptionHandler; } set { Instance.ExceptionHandler = value; } }

		public static HandleServiceExceptionDelegate ServiceExceptionHandler { get { return Instance.ServiceExceptionHandler; } set { Instance.ServiceExceptionHandler = value; } }

		public static List<HttpHandlerResolverDelegate> CatchAllHandlers { get { return Instance.CatchAllHandlers; } set { Instance.CatchAllHandlers = value; } }

		public static List<IPlugin> Plugins { get { return Instance.Plugins; } set { Instance.Plugins = value; } }

		public static IVirtualPathProvider VirtualPathProvider { get { return Instance.VirtualPathProvider; } set { Instance.VirtualPathProvider = value; } }

		public static DateTime StartedAt { get { return Instance.StartedAt; } set { Instance.StartedAt = value; } }

		public static DateTime ReadyAt { get { return Instance.ReadyAt; } set { Instance.ReadyAt = value; } }
		
		// Pre user config
		public static void ConfigureHost(IAppHost appHost, string serviceName, ServiceManager serviceManager)
		{
			Instance.ConfigureHost(appHost, serviceName, serviceManager);
		}

		//After configure called
		public static void AfterInit()
		{
			Instance.AfterInit();
		}

        public static T TryResolve<T>()
        {
			return Instance.TryResolve<T>();
        }

        /// <summary>
        /// The AppHost.Container. Note: it is not thread safe to register dependencies after AppStart.
        /// </summary>
	    public static Container Container
	    {
	        get {
				return Instance.Container;
	        }
	    }

		public static void AddPlugin(params IPlugin[] plugins)
		{
			Instance.AddPlugin(plugins);
		}

		public static ServiceManager ServiceManager
		{
			get { return Instance.ServiceManager; }
			set
			{
				Instance.ServiceManager = value;
			}
		}

		public static class UserConfig
		{
			public static bool DebugMode
			{
				get { return Config != null && Config.DebugMode; }
			}
		}

		//private static EndpointHostConfig config;

		public static EndpointHostConfig Config
		{
			get
			{
				return Instance.Config;
			}
			set
			{
				Instance.Config = value;
			}
		}

		/// <summary>
		/// Applies the raw request filters. Returns whether or not the request has been handled 
		/// and no more processing should be done.
		/// </summary>
		/// <returns></returns>
		public static bool ApplyPreRequestFilters(IHttpRequest httpReq, IHttpResponse httpRes)
		{
			return Instance.ApplyPreRequestFilters(httpReq, httpRes);
		}

		/// <summary>
		/// Applies the request filters. Returns whether or not the request has been handled 
		/// and no more processing should be done.
		/// </summary>
		/// <returns></returns>
		public static bool ApplyRequestFilters(IHttpRequest httpReq, IHttpResponse httpRes, object requestDto)
		{
			return Instance.ApplyRequestFilters(httpReq, httpRes, requestDto);
		}

		/// <summary>
		/// Applies the response filters. Returns whether or not the request has been handled 
		/// and no more processing should be done.
		/// </summary>
		/// <returns></returns>
		public static bool ApplyResponseFilters(IHttpRequest httpReq, IHttpResponse httpRes, object response)
		{
			return Instance.ApplyResponseFilters(httpReq, httpRes, response);
		}

		public static void SetOperationTypes(ServiceOperations operationTypes, ServiceOperations allOperationTypes)
		{
			Instance.SetOperationTypes(operationTypes, allOperationTypes);
		}

		internal static object ExecuteService(object request, EndpointAttributes endpointAttributes, IHttpRequest httpReq, IHttpResponse httpRes)
		{
			return Instance.ExecuteService(request, endpointAttributes, httpReq, httpRes);
		}

        public static IServiceRunner<TRequest> CreateServiceRunner<TRequest>(ActionContext actionContext)
        {
			return Instance.CreateServiceRunner<TRequest>(actionContext);
        }

	    /// <summary>
        /// Call to signal the completion of a ServiceStack-handled Request
        /// </summary>
	    internal static void CompleteRequest()
        {
			Instance.CompleteRequest();
        }
	}
}