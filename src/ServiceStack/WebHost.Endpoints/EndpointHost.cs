using System;
using System.Collections.Generic;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.Html;
using ServiceStack.Logging;
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
	public class EndpointHost
	{
		public static ServiceOperations ServiceOperations { get; private set; }
		public static ServiceOperations AllServiceOperations { get; private set; }

		public static IAppHost AppHost { get; internal set; }

		public static IContentTypeFilter ContentTypeFilter { get; set; }

        public static List<Action<IHttpRequest, IHttpResponse>> RawRequestFilters { get; private set; }

		public static List<Action<IHttpRequest, IHttpResponse, object>> RequestFilters { get; private set; }

		public static List<Action<IHttpRequest, IHttpResponse, object>> ResponseFilters { get; private set; }

        public static List<IViewEngine> ViewEngines { get; set; }

        public static Action<IHttpRequest, IHttpResponse, string, Exception> ExceptionHandler { get; set; }

		public static List<HttpHandlerResolverDelegate> CatchAllHandlers { get; set; }

		private static bool pluginsLoaded = false;

		public static List<IPlugin> Plugins { get; set; }

		public static IVirtualPathProvider VirtualPathProvider { get; set; }

        public static DateTime StartedAt { get; set; }

        public static DateTime ReadyAt { get; set; }

		static EndpointHost()
		{
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
			};
		}
		
		// Pre user config
		public static void ConfigureHost(IAppHost appHost, string serviceName, ServiceManager serviceManager)
		{
			AppHost = appHost;

			EndpointHostConfig.Instance.ServiceName = serviceName;
			EndpointHostConfig.Instance.ServiceManager = serviceManager;

			var config = EndpointHostConfig.Instance;
			Config = config; // avoid cross-dependency on Config setter
			VirtualPathProvider = new FileSystemVirtualPathProvider(AppHost, Config.WebHostPhysicalPath);
		}

		// Config has changed
		private static void ApplyConfigChanges()
		{
			config.ServiceEndpointsMetadataConfig = ServiceEndpointsMetadataConfig.Create(config.ServiceStackHandlerFactoryPath);

			JsonDataContractSerializer.Instance.UseBcl = config.UseBclJsonSerializers;
			JsonDataContractDeserializer.Instance.UseBcl = config.UseBclJsonSerializers;
		}

		//After configure called
		public static void AfterInit()
		{
            StartedAt = DateTime.Now;

			if (config.EnableFeatures != Feature.All)
			{
				if ((Feature.Xml & config.EnableFeatures) != Feature.Xml)
					config.IgnoreFormatsInMetadata.Add("xml");
				if ((Feature.Json & config.EnableFeatures) != Feature.Json)
					config.IgnoreFormatsInMetadata.Add("json");
				if ((Feature.Jsv & config.EnableFeatures) != Feature.Jsv)
					config.IgnoreFormatsInMetadata.Add("jsv");
				if ((Feature.Csv & config.EnableFeatures) != Feature.Csv)
					config.IgnoreFormatsInMetadata.Add("csv");
				if ((Feature.Html & config.EnableFeatures) != Feature.Html)
					config.IgnoreFormatsInMetadata.Add("html");
				if ((Feature.Soap11 & config.EnableFeatures) != Feature.Soap11)
					config.IgnoreFormatsInMetadata.Add("soap11");
				if ((Feature.Soap12 & config.EnableFeatures) != Feature.Soap12)
					config.IgnoreFormatsInMetadata.Add("soap12");
			}

			if ((Feature.Html & config.EnableFeatures) != Feature.Html)
				Plugins.RemoveAll(x => x is HtmlFormat);

			if ((Feature.Csv & config.EnableFeatures) != Feature.Csv)
				Plugins.RemoveAll(x => x is CsvFormat);

			if ((Feature.Markdown & config.EnableFeatures) != Feature.Markdown)
				Plugins.RemoveAll(x => x is MarkdownFormat);

			if ((Feature.Razor & config.EnableFeatures) != Feature.Razor)
				Plugins.RemoveAll(x => x is IRazorPlugin);    //external

			if ((Feature.ProtoBuf & config.EnableFeatures) != Feature.ProtoBuf)
				Plugins.RemoveAll(x => x is IProtoBufPlugin); //external

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

			var specifiedContentType = config.DefaultContentType; //Before plugins loaded

            ConfigurePlugins();

			AppHost.LoadPlugin(Plugins.ToArray());
			pluginsLoaded = true;

			AfterPluginsLoaded(specifiedContentType);

		    ReadyAt = DateTime.Now;
		}

	    private static void ConfigurePlugins()
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

	    private static void AfterPluginsLoaded(string specifiedContentType)
		{
			if (!string.IsNullOrEmpty(specifiedContentType))
				config.DefaultContentType = specifiedContentType;
			else if (string.IsNullOrEmpty(config.DefaultContentType))
				config.DefaultContentType = ContentType.Json;

			config.ServiceManager.AfterInit();
			ServiceManager = config.ServiceManager; //reset operations
		}

		public static void AddPlugin(params IPlugin[] plugins)
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

		public static ServiceManager ServiceManager
		{
			get { return config.ServiceManager; }
			set
			{
				config.ServiceManager = value;
				ServiceOperations = value.ServiceOperations;
				AllServiceOperations = value.AllServiceOperations;
			}
		}

		public static class UserConfig
		{
			public static bool DebugMode
			{
				get { return Config != null && Config.DebugMode; }
			}
		}

		private static EndpointHostConfig config;

		public static EndpointHostConfig Config
		{
			get
			{
				return config;
			}
			set
			{
				if (value.ServiceName == null)
					throw new ArgumentNullException("ServiceName");

				if (value.ServiceController == null)
					throw new ArgumentNullException("ServiceController");

				config = value;
				ApplyConfigChanges();
			}
		}

		/// <summary>
		/// Applies the raw request filters. Returns whether or not the request has been handled 
		/// and no more processing should be done.
		/// </summary>
		/// <returns></returns>
		public static bool ApplyPreRequestFilters(IHttpRequest httpReq, IHttpResponse httpRes)
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
		public static bool ApplyRequestFilters(IHttpRequest httpReq, IHttpResponse httpRes, object requestDto)
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
		public static bool ApplyResponseFilters(IHttpRequest httpReq, IHttpResponse httpRes, object response)
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

		public static void SetOperationTypes(ServiceOperations operationTypes, ServiceOperations allOperationTypes)
		{
			ServiceOperations = operationTypes;
			AllServiceOperations = allOperationTypes;
		}

		internal static object ExecuteService(object request, EndpointAttributes endpointAttributes, IHttpRequest httpReq, IHttpResponse httpRes)
		{
			using (Profiler.Current.Step("Execute Service"))
			{
                return config.ServiceController.Execute(request,
                    new HttpRequestContext(httpReq, httpRes, request, endpointAttributes));
            }
		}

        /// <summary>
        /// Call to signal the completion of a ServiceStack-handled Request
        /// </summary>
	    internal static void CompleteRequest()
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