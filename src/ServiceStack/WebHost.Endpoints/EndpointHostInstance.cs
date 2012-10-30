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

		private bool _runAsNamedInstance = false;

		public EndpointHostInstance(): this(false) { }

		public EndpointHostInstance(bool runAsNamedInstance)
		{
			_runAsNamedInstance = runAsNamedInstance;
			ContentTypeFilter = HttpResponseFilter.Instance;
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
		}


		// Pre user config
		public  void ConfigureHost(IAppHost appHost, string serviceName, ServiceManager serviceManager)
		{
			AppHost = appHost;

			EndpointHostConfig config = _runAsNamedInstance ? EndpointHostConfig.GetNamedConfig(serviceName) : EndpointHostConfig.Instance;

			config.ServiceName = serviceName;
			config.ServiceManager = serviceManager;

			Config = config; 
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
			_config.ServiceEndpointsMetadataConfig = ServiceEndpointsMetadataConfig.Create(_config.ServiceStackHandlerFactoryPath);

			//will overwrite for whole app domain.
			JsonDataContractSerializer.Instance.UseBcl = _config.UseBclJsonSerializers;
			JsonDataContractDeserializer.Instance.UseBcl = _config.UseBclJsonSerializers;
		}

		//After configure called
		public  void AfterInit()
		{
            StartedAt = DateTime.Now;

			if (_config.EnableFeatures != Feature.All)
			{
				if ((Feature.Xml & _config.EnableFeatures) != Feature.Xml)
					_config.IgnoreFormatsInMetadata.Add("xml");
				if ((Feature.Json & _config.EnableFeatures) != Feature.Json)
					_config.IgnoreFormatsInMetadata.Add("json");
				if ((Feature.Jsv & _config.EnableFeatures) != Feature.Jsv)
					_config.IgnoreFormatsInMetadata.Add("jsv");
				if ((Feature.Csv & _config.EnableFeatures) != Feature.Csv)
					_config.IgnoreFormatsInMetadata.Add("csv");
				if ((Feature.Html & _config.EnableFeatures) != Feature.Html)
					_config.IgnoreFormatsInMetadata.Add("html");
				if ((Feature.Soap11 & _config.EnableFeatures) != Feature.Soap11)
					_config.IgnoreFormatsInMetadata.Add("soap11");
				if ((Feature.Soap12 & _config.EnableFeatures) != Feature.Soap12)
					_config.IgnoreFormatsInMetadata.Add("soap12");
			}

			if ((Feature.Html & _config.EnableFeatures) != Feature.Html)
				Plugins.RemoveAll(x => x is HtmlFormat);

			if ((Feature.Csv & _config.EnableFeatures) != Feature.Csv)
				Plugins.RemoveAll(x => x is CsvFormat);

            if ((Feature.Markdown & _config.EnableFeatures) != Feature.Markdown)
                Plugins.RemoveAll(x => x is MarkdownFormat);

            if ((Feature.PredefinedRoutes & _config.EnableFeatures) != Feature.PredefinedRoutes)
                Plugins.RemoveAll(x => x is PredefinedRoutesFeature);

            if ((Feature.Metadata & _config.EnableFeatures) != Feature.Metadata)
                Plugins.RemoveAll(x => x is MetadataFeature);

            if ((Feature.RequestInfo & _config.EnableFeatures) != Feature.RequestInfo)
                Plugins.RemoveAll(x => x is RequestInfoFeature);

			if ((Feature.Razor & _config.EnableFeatures) != Feature.Razor)
				Plugins.RemoveAll(x => x is IRazorPlugin);    //external

            if ((Feature.ProtoBuf & _config.EnableFeatures) != Feature.ProtoBuf)
                Plugins.RemoveAll(x => x is IProtoBufPlugin); //external

            if ((Feature.MsgPack & _config.EnableFeatures) != Feature.MsgPack)
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

			var specifiedContentType = _config.DefaultContentType; //Before plugins loaded

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

        /// <summary>
        /// The AppHost.Container. Note: it is not thread safe to register dependencies after AppStart.
        /// </summary>
	    public  Container Container
	    {
	        get { 
                var aspHost = AppHost as AppHostBase;
                if (aspHost != null)
                    return aspHost.Container;
	            var listenerHost = AppHost as HttpListenerBase;
                return listenerHost != null ? listenerHost.Container : new Container(); //testing may use alt AppHost
	        }
	    }

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
				_config.DefaultContentType = specifiedContentType;
			else if (string.IsNullOrEmpty(_config.DefaultContentType))
				_config.DefaultContentType = ContentType.Json;

			_config.ServiceManager.AfterInit();
			ServiceManager = _config.ServiceManager; //reset operations
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
			get { return _config.ServiceManager; }
			set
			{
				_config.ServiceManager = value;
				ServiceOperations = value.ServiceOperations;
				AllServiceOperations = value.AllServiceOperations;
			}
		}

		private  EndpointHostConfig _config;

		public  EndpointHostConfig Config
		{
			get
			{
				//Q: should we instead retrieve this from EndPointHostConfig for named configs?
				return _config;
			}
			set
			{
				if (value.ServiceName == null)
					throw new ArgumentNullException("ServiceName");

				if (value.ServiceController == null)
					throw new ArgumentNullException("ServiceController");

				_config = value;
				ApplyConfigChanges();
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
                return _config.ServiceController.Execute(request,
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