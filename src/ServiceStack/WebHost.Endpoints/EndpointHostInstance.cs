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
	public class EndpointHostInstance
	{
		public  ServiceOperations ServiceOperations { get; private set; }
		public  ServiceOperations AllServiceOperations { get; private set; }

		public  IAppHost AppHost { get; internal set; }

		public  IContentTypeFilter ContentTypeFilter { get; set; }

        public  List<Action<IHttpRequest, IHttpResponse>> RawRequestFilters { get; private set; }

		public  List<Action<IHttpRequest, IHttpResponse, object>> RequestFilters { get; private set; }

		public  List<Action<IHttpRequest, IHttpResponse, object>> ResponseFilters { get; private set; }

        public  List<IViewEngine> ViewEngines { get; set; }

        public  HandleUncaughtExceptionDelegate ExceptionHandler { get; set; }
        
        public  HandleServiceExceptionDelegate ServiceExceptionHandler { get; set; }

		public  List<HttpHandlerResolverDelegate> CatchAllHandlers { get; set; }

		private  bool pluginsLoaded = false;

		public  List<IPlugin> Plugins { get; set; }

		public  IVirtualPathProvider VirtualPathProvider { get; set; }

        public  DateTime StartedAt { get; set; }

        public  DateTime ReadyAt { get; set; }

		private bool _runAsInstance = false;
		private static EndpointHostInstance _singleton;

		internal static EndpointHostInstance Singleton
		{
			get
			{
				if (_singleton == null) throw new InvalidOperationException("Must initialize app host before accessing EndpointHost.");
				return _singleton;
			}
		}

		internal static bool HasSingleton
		{
			get { return (_singleton != null); }
		}

		internal static void CreateSingleton(IAppHost appHost)
		{
			if (_singleton != null) throw new InvalidOperationException("Endpoint already initialized.");
			lock(_syncRoot)
			{
				var single = new EndpointHostInstance(false, appHost );
				//we have to do it this way because the Init code requires the static singleton to already be setup.
				//it's a bit of chicken and egg.  
				_singleton = single;
				single.Init();
			}
		}

		internal static EndpointHostInstance CreateInstance(IAppHost appHost)
		{
			var newInstance = new EndpointHostInstance(true, appHost);
			newInstance.Init();
			return newInstance;
		}

		private void Init()
		{
			//the config must be created after the AppHost assignment, because the AppHost is used in the config setup.
			if (_runAsInstance)
			{
			//	Config = EndpointHostConfig.Create();
				ContentTypeFilter = new HttpResponseFilter();
			}
			else
			{
		//		EndpointHostConfig.CreateSingleton(AppHost);
			//	Config = EndpointHostConfig.Instance;
				ContentTypeFilter = HttpResponseFilter.Instance;
			}

		
			RawRequestFilters = new List<Action<IHttpRequest, IHttpResponse>>();
			RequestFilters = new List<Action<IHttpRequest, IHttpResponse, object>>();
			ResponseFilters = new List<Action<IHttpRequest, IHttpResponse, object>>();
			ViewEngines = new List<IViewEngine>();
			CatchAllHandlers = new List<HttpHandlerResolverDelegate>();
			Plugins = new List<IPlugin> {
				new HtmlFormat(),
				new CsvFormat(),
				new MarkdownFormat(),
                new PredefinedRoutesFeature(),
                new MetadataFeature(),
			};
		//	ApplyConfigChanges();
		}

		/// <summary>
		/// Initializes an instance of <see cref="EndpointHostInstance"/>
		/// </summary>
		/// <param name="runAsInstance">Set this to <see langword="true"/> to run as a seperate instance; otherwise, false will cause this to run as a singleton.</param>
		private EndpointHostInstance(bool runAsInstance, IAppHost appHost )
		{
			_runAsInstance = runAsInstance;
			AppHost = appHost;
			//Init() needs to be called
		}

		// Pre user config
		public void ConfigureHost(IAppHost appHost, string serviceName, ServiceManager serviceManager)
		{
			AppHost = appHost;

			if (!_runAsInstance)
			{ //must be assigned after AppHost
			//	Config = EndpointHostConfig.Instance;
			}

			Config.ServiceName = serviceName;
			Config.ServiceManager = serviceManager;

			VirtualPathProvider = new FileSystemVirtualPathProvider(AppHost, Config.WebHostPhysicalPath);

		    Config.DebugMode = appHost.GetType().Assembly.IsDebugBuild();             
            if (Config.DebugMode)
            {
                Plugins.Add(new RequestInfoFeature());
            }
		}

		// Config has changed
		private  void ApplyConfigChanges()
		{
			Config.ServiceEndpointsMetadataConfig = ServiceEndpointsMetadataConfig.Create(Config.ServiceStackHandlerFactoryPath);

			//will overwrite for whole app domain.
			JsonDataContractSerializer.Instance.UseBcl = Config.UseBclJsonSerializers;
			JsonDataContractDeserializer.Instance.UseBcl = Config.UseBclJsonSerializers;
		}

		//After configure called
		public  void AfterInit()
		{
            StartedAt = DateTime.Now;

			if (Config.EnableFeatures != Feature.All)
			{
				if ((Feature.Xml & Config.EnableFeatures) != Feature.Xml)
					Config.IgnoreFormatsInMetadata.Add("xml");
				if ((Feature.Json & Config.EnableFeatures) != Feature.Json)
					Config.IgnoreFormatsInMetadata.Add("json");
				if ((Feature.Jsv & Config.EnableFeatures) != Feature.Jsv)
					Config.IgnoreFormatsInMetadata.Add("jsv");
				if ((Feature.Csv & Config.EnableFeatures) != Feature.Csv)
					Config.IgnoreFormatsInMetadata.Add("csv");
				if ((Feature.Html & Config.EnableFeatures) != Feature.Html)
					Config.IgnoreFormatsInMetadata.Add("html");
				if ((Feature.Soap11 & Config.EnableFeatures) != Feature.Soap11)
					Config.IgnoreFormatsInMetadata.Add("soap11");
				if ((Feature.Soap12 & Config.EnableFeatures) != Feature.Soap12)
					Config.IgnoreFormatsInMetadata.Add("soap12");
			}

			if ((Feature.Html & Config.EnableFeatures) != Feature.Html)
				Plugins.RemoveAll(x => x is HtmlFormat);

			if ((Feature.Csv & Config.EnableFeatures) != Feature.Csv)
				Plugins.RemoveAll(x => x is CsvFormat);

            if ((Feature.Markdown & Config.EnableFeatures) != Feature.Markdown)
                Plugins.RemoveAll(x => x is MarkdownFormat);

            if ((Feature.PredefinedRoutes & Config.EnableFeatures) != Feature.PredefinedRoutes)
                Plugins.RemoveAll(x => x is PredefinedRoutesFeature);

            if ((Feature.Metadata & Config.EnableFeatures) != Feature.Metadata)
                Plugins.RemoveAll(x => x is MetadataFeature);

            if ((Feature.RequestInfo & Config.EnableFeatures) != Feature.RequestInfo)
                Plugins.RemoveAll(x => x is RequestInfoFeature);

			if ((Feature.Razor & Config.EnableFeatures) != Feature.Razor)
				Plugins.RemoveAll(x => x is IRazorPlugin);    //external

            if ((Feature.ProtoBuf & Config.EnableFeatures) != Feature.ProtoBuf)
                Plugins.RemoveAll(x => x is IProtoBufPlugin); //external

            if ((Feature.MsgPack & Config.EnableFeatures) != Feature.MsgPack)
                Plugins.RemoveAll(x => x is IMsgPackPlugin);  //external

            if (ExceptionHandler == null) {
                ExceptionHandler = (httpReq, httpRes, operationName, ex) => {
                    var errorMessage = string.Format("Error occured while Processing Request: {0}", ex.Message);
                    var statusCode = ex.ToStatusCode();
                    //httpRes.WriteToResponse always calls .Close in it's finally statement so 
                    //if there is a problem writing to response, by now it will be closed
                    if (!httpRes.IsClosed) {
                        httpRes.WriteErrorToResponse(httpReq.ResponseContentType, operationName, errorMessage, ex, statusCode);
                    }
                };
            }

			var specifiedContentType = Config.DefaultContentType; //Before plugins loaded

            ConfigurePlugins();

			AppHost.LoadPlugin(Plugins.ToArray());
			pluginsLoaded = true;

			AfterPluginsLoaded(specifiedContentType);

		    var registeredCacheClient = AppHost.TryResolve<ICacheClient>();
            using (registeredCacheClient)
            {
                if (registeredCacheClient == null)
                {
                    Container.Register<ICacheClient>(new MemoryCacheClient());
                }
            }

		    ReadyAt = DateTime.Now;
		}

        public  T TryResolve<T>()
        {
            return AppHost != null ? AppHost.TryResolve<T>() : default(T);
        }

		private static readonly object _syncRoot = new object();

        /// <summary>
        /// The AppHost.Container. 
        /// </summary>
		/// <remarks>This method is thread safe.</remarks>
	    public  Container Container
	    {
	        get { 
                var aspHost = AppHost as AppHostBase;
                if (aspHost != null) return aspHost.Container;
	            var listenerHost = AppHost as HttpListenerBase;
				if (listenerHost != null) return listenerHost.Container;

				if (_container != null) return _container;
				lock (_syncRoot)
				{
					if (_container != null) return _container;
					_container = new Container();
				}
					//throw new InvalidOperationException("An AppHost must must be initialized before accessing the container.");
				return _container;
	        }
	    }

		private Container _container;

	    private  void ConfigurePlugins()
	    {
            //Some plugins need to initialize before other plugins are registered.

	        foreach (var plugin in Plugins)
	        {
	            var preInitPlugin = plugin as IPreInitPlugin;
	            if (preInitPlugin != null)
	            {
                    preInitPlugin.Configure(AppHost);
                }
	        }
	    }

	    private  void AfterPluginsLoaded(string specifiedContentType)
		{
			if (!string.IsNullOrEmpty(specifiedContentType))
				Config.DefaultContentType = specifiedContentType;
			else if (string.IsNullOrEmpty(Config.DefaultContentType))
				Config.DefaultContentType = ContentType.Json;

			Config.ServiceManager.AfterInit();
			ServiceManager = Config.ServiceManager; //reset operations
		}

		public  void AddPlugin(params IPlugin[] plugins)
		{
			if (pluginsLoaded)
			{
				AppHost.LoadPlugin(plugins);
				ServiceManager.ReloadServiceOperations();
			}
			else
			{
				foreach (var plugin in plugins)
				{
					Plugins.Add(plugin);
				}
			}
		}

		public  ServiceManager ServiceManager
		{
			get { return Config.ServiceManager; }
			set
			{
				Config.ServiceManager = value;
				ServiceOperations = value.ServiceOperations;
				AllServiceOperations = value.AllServiceOperations;
			}
		}

		private EndpointHostConfig _config;

		public  EndpointHostConfig Config
		{
			get
			{
				if (_config != null) return _config;
				lock (_syncRoot)
				{
					if (_config != null) return _config;
					if (_runAsInstance)
					{
						_config = EndpointHostConfig.Create();
						ApplyConfigChanges();
					}
					else
					{
						EndpointHostConfig.CreateSingleton(AppHost);
						_config = EndpointHostConfig.Instance;
						ApplyConfigChanges();
					}
				}

				return _config;
			}
			set
			{
				if (value.ServiceName == null)
					throw new ArgumentNullException("ServiceName");

				if (value.ServiceController == null)
					throw new ArgumentNullException("ServiceController");

				lock (_syncRoot)
				{
					Config = value;
					ApplyConfigChanges();
				}
			}
		}

		/// <summary>
		/// Applies the raw request filters. Returns whether or not the request has been handled 
		/// and no more processing should be done.
		/// </summary>
		/// <returns></returns>
		public  bool ApplyPreRequestFilters(IHttpRequest httpReq, IHttpResponse httpRes)
		{
			foreach (var requestFilter in RawRequestFilters)
			{
				requestFilter(httpReq, httpRes);
				if (httpRes.IsClosed) break;
			}

			return httpRes.IsClosed;
		}

		/// <summary>
		/// Applies the request filters. Returns whether or not the request has been handled 
		/// and no more processing should be done.
		/// </summary>
		/// <returns></returns>
		public  bool ApplyRequestFilters(IHttpRequest httpReq, IHttpResponse httpRes, object requestDto)
		{
			httpReq.ThrowIfNull("httpReq");
			httpRes.ThrowIfNull("httpRes");

			using (Profiler.Current.Step("Executing Request Filters"))
			{
				//Exec all RequestFilter attributes with Priority < 0
				var attributes = FilterAttributeCache.GetRequestFilterAttributes(requestDto.GetType());
				var i = 0;
				for (; i < attributes.Length && attributes[i].Priority < 0; i++)
				{
					var attribute = attributes[i];
					ServiceManager.Container.AutoWire(attribute);
					attribute.RequestFilter(httpReq, httpRes, requestDto);
					if (AppHost != null) //tests
						AppHost.Release(attribute);
					if (httpRes.IsClosed) return httpRes.IsClosed;
				}

				//Exec global filters
				foreach (var requestFilter in RequestFilters)
				{
					requestFilter(httpReq, httpRes, requestDto);
					if (httpRes.IsClosed) return httpRes.IsClosed;
				}

				//Exec remaining RequestFilter attributes with Priority >= 0
				for (; i < attributes.Length; i++)
				{
					var attribute = attributes[i];
					ServiceManager.Container.AutoWire(attribute);
					attribute.RequestFilter(httpReq, httpRes, requestDto);
					if (AppHost != null) //tests
						AppHost.Release(attribute);
					if (httpRes.IsClosed) return httpRes.IsClosed;
				}

				return httpRes.IsClosed;
			}
		}

		/// <summary>
		/// Applies the response filters. Returns whether or not the request has been handled 
		/// and no more processing should be done.
		/// </summary>
		/// <returns></returns>
		public  bool ApplyResponseFilters(IHttpRequest httpReq, IHttpResponse httpRes, object response)
		{
			httpReq.ThrowIfNull("httpReq");
			httpRes.ThrowIfNull("httpRes");

			using (Profiler.Current.Step("Executing Response Filters"))
			{
				var responseDto = response.ToResponseDto();
				var attributes = responseDto != null
					? FilterAttributeCache.GetResponseFilterAttributes(responseDto.GetType())
					: null;

				//Exec all ResponseFilter attributes with Priority < 0
				var i = 0;
				if (attributes != null)
				{
					for (; i < attributes.Length && attributes[i].Priority < 0; i++)
					{
						var attribute = attributes[i];
						ServiceManager.Container.AutoWire(attribute);
						attribute.ResponseFilter(httpReq, httpRes, response);
						if (AppHost != null) //tests
							AppHost.Release(attribute);
						if (httpRes.IsClosed) return httpRes.IsClosed;
					}
				}

				//Exec global filters
				foreach (var responseFilter in ResponseFilters)
				{
					responseFilter(httpReq, httpRes, response);
					if (httpRes.IsClosed) return httpRes.IsClosed;
				}

				//Exec remaining RequestFilter attributes with Priority >= 0
				if (attributes != null)
				{
					for (; i < attributes.Length; i++)
					{
						var attribute = attributes[i];
						ServiceManager.Container.AutoWire(attribute);
						attribute.ResponseFilter(httpReq, httpRes, response);
						if (AppHost != null) //tests
							AppHost.Release(attribute);
						if (httpRes.IsClosed) return httpRes.IsClosed;
					}
				}

				return httpRes.IsClosed;
			}
		}

		public  void SetOperationTypes(ServiceOperations operationTypes, ServiceOperations allOperationTypes)
		{
			ServiceOperations = operationTypes;
			AllServiceOperations = allOperationTypes;
		}

		internal  object ExecuteService(object request, EndpointAttributes endpointAttributes, IHttpRequest httpReq, IHttpResponse httpRes)
		{
			using (Profiler.Current.Step("Execute Service"))
			{
                return Config.ServiceController.Execute(request,
                    new HttpRequestContext(httpReq, httpRes, request, endpointAttributes));
            }
		}

        public  IServiceRunner<TRequest> CreateServiceRunner<TRequest>(ActionContext actionContext)
        {
            return AppHost != null 
                ? AppHost.CreateServiceRunner<TRequest>(actionContext) 
                : new ServiceRunner<TRequest>(null, actionContext);
        }

	    /// <summary>
        /// Call to signal the completion of a ServiceStack-handled Request
        /// </summary>
	    internal  void CompleteRequest()
        {
	        try
	        {
                if (AppHost != null)
                {
                    AppHost.OnEndRequest();
                }
	        }
	        catch (Exception ex) {}
        }
	}
}
