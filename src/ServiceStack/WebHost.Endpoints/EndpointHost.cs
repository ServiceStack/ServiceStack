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
		private static readonly EndpointHostInstance _instance = new EndpointHostInstance();

		internal static EndpointHostInstance Instance { get { return _instance; } }

		private static Dictionary<string, EndpointHostInstance> _namedHosts = new Dictionary<string, EndpointHostInstance>();
		private readonly static object _syncRoot = new object();

		/// <summary>
		/// Gets a <see cref="EndpointHostInstance"/> by name, and creates a new host if one doesn't exist by that name.
		/// </summary>
		/// <param name="name">The name of the endpoint to create.</param>
		/// <returns>Returns the instance.</returns>
		/// <remarks>This method is thread safe.</remarks>
		internal static EndpointHostInstance GetNamedHost(string name)
		{
			if (string.IsNullOrEmpty(name)) throw new ArgumentException("Agument must not be not or empty", "name");

			EndpointHostInstance host;
			if (_namedHosts.TryGetValue(name, out host))
			{
				return host;
			}

			lock (_syncRoot) 
			{
				if (_namedHosts.TryGetValue(name, out host)) //double checked locking works fine in .Net
				{
					return host;
				}
				var namedHosts = new Dictionary<string, EndpointHostInstance>(_namedHosts);
				namedHosts.Add(name, host = new EndpointHostInstance(true));
				//publish last
				_namedHosts = namedHosts;
				return host;
			}
		}

		internal static void RemoveNamedHost(string name)
		{
			if (string.IsNullOrEmpty(name)) return;

			lock (_syncRoot)
			{
				var namedHosts = new Dictionary<string, EndpointHostInstance>(_namedHosts);
				namedHosts.Remove(name);
				_namedHosts = namedHosts;
			}
		}

		public static ServiceOperations ServiceOperations { get { return _instance.ServiceOperations; }  }
		public static ServiceOperations AllServiceOperations { get { return _instance.AllServiceOperations; } }

		public static IAppHost AppHost { get { return _instance.AppHost; } internal set { _instance.AppHost = value; } }

		public static IContentTypeFilter ContentTypeFilter { get { return _instance.ContentTypeFilter; } set { _instance.ContentTypeFilter = value; } }

		public static List<Action<IHttpRequest, IHttpResponse>> RawRequestFilters { get { return _instance.RawRequestFilters; } }

		public static List<Action<IHttpRequest, IHttpResponse, object>> RequestFilters { get { return _instance.RequestFilters; } }

		public static List<Action<IHttpRequest, IHttpResponse, object>> ResponseFilters { get { return _instance.ResponseFilters; } }

		public static List<IViewEngine> ViewEngines { get { return _instance.ViewEngines; } set { _instance.ViewEngines = value; } }

		public static HandleUncaughtExceptionDelegate ExceptionHandler { get { return _instance.ExceptionHandler; } set { _instance.ExceptionHandler = value; } }

		public static HandleServiceExceptionDelegate ServiceExceptionHandler { get { return _instance.ServiceExceptionHandler; } set { _instance.ServiceExceptionHandler = value; } }

		public static List<HttpHandlerResolverDelegate> CatchAllHandlers { get { return _instance.CatchAllHandlers; } set { _instance.CatchAllHandlers = value; } }

		public static List<IPlugin> Plugins { get { return _instance.Plugins; } set { _instance.Plugins = value; } }

		public static IVirtualPathProvider VirtualPathProvider { get { return _instance.VirtualPathProvider; } set { _instance.VirtualPathProvider = value; } }

		public static DateTime StartedAt { get { return _instance.StartedAt; } set { _instance.StartedAt = value; } }

		public static DateTime ReadyAt { get { return _instance.ReadyAt; } set { _instance.ReadyAt = value; } }
		
		// Pre user config
		public static void ConfigureHost(IAppHost appHost, string serviceName, ServiceManager serviceManager)
		{
			_instance.ConfigureHost(appHost, serviceName, serviceManager);
		}

		//After configure called
		public static void AfterInit()
		{
			_instance.AfterInit();
		}

        public static T TryResolve<T>()
        {
			return _instance.TryResolve<T>();
        }

        /// <summary>
        /// The AppHost.Container. Note: it is not thread safe to register dependencies after AppStart.
        /// </summary>
	    public static Container Container
	    {
	        get {
				return _instance.Container;
	        }
	    }

		public static void AddPlugin(params IPlugin[] plugins)
		{
			_instance.AddPlugin(plugins);
		}

		public static ServiceManager ServiceManager
		{
			get { return _instance.ServiceManager; }
			set
			{
				_instance.ServiceManager = value;
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
				return _instance.Config;
			}
			set
			{
				_instance.Config = value;
			}
		}

		/// <summary>
		/// Applies the raw request filters. Returns whether or not the request has been handled 
		/// and no more processing should be done.
		/// </summary>
		/// <returns></returns>
		public static bool ApplyPreRequestFilters(IHttpRequest httpReq, IHttpResponse httpRes)
		{
			return _instance.ApplyPreRequestFilters(httpReq, httpRes);
		}

		/// <summary>
		/// Applies the request filters. Returns whether or not the request has been handled 
		/// and no more processing should be done.
		/// </summary>
		/// <returns></returns>
		public static bool ApplyRequestFilters(IHttpRequest httpReq, IHttpResponse httpRes, object requestDto)
		{
			return _instance.ApplyRequestFilters(httpReq, httpRes, requestDto);
		}

		/// <summary>
		/// Applies the response filters. Returns whether or not the request has been handled 
		/// and no more processing should be done.
		/// </summary>
		/// <returns></returns>
		public static bool ApplyResponseFilters(IHttpRequest httpReq, IHttpResponse httpRes, object response)
		{
			return _instance.ApplyResponseFilters(httpReq, httpRes, response);
		}

		public static void SetOperationTypes(ServiceOperations operationTypes, ServiceOperations allOperationTypes)
		{
			_instance.SetOperationTypes(operationTypes, allOperationTypes);
		}

		internal static object ExecuteService(object request, EndpointAttributes endpointAttributes, IHttpRequest httpReq, IHttpResponse httpRes)
		{
			return _instance.ExecuteService(request, endpointAttributes, httpReq, httpRes);
		}

        public static IServiceRunner<TRequest> CreateServiceRunner<TRequest>(ActionContext actionContext)
        {
			return _instance.CreateServiceRunner<TRequest>(actionContext);
        }

	    /// <summary>
        /// Call to signal the completion of a ServiceStack-handled Request
        /// </summary>
	    internal static void CompleteRequest()
        {
			_instance.CompleteRequest();
        }
	}
}