using System;
using System.Collections.Generic;
using System.Web;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.MiniProfiler;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.WebHost.Endpoints.Formats;
using ServiceStack.WebHost.Endpoints.Formats;
using ServiceStack.WebHost.Endpoints.Utils;

namespace ServiceStack.WebHost.Endpoints
{
	public class EndpointHost
	{
		public static ServiceOperations ServiceOperations { get; private set; }
		public static ServiceOperations AllServiceOperations { get; private set; }

		public static IAppHost AppHost { get; internal set; }

		public static IContentTypeFilter ContentTypeFilter { get; set; }

		public static List<Action<IHttpRequest, IHttpResponse, object>> RequestFilters { get; private set; }

		public static List<Action<IHttpRequest, IHttpResponse, object>> ResponseFilters { get; private set; }

		public static List<StreamSerializerResolverDelegate> HtmlProviders { get; set; }

		public static List<HttpHandlerResolverDelegate> CatchAllHandlers { get; set; }

		public static List<IPlugin> Plugins { get; set; }

		static EndpointHost()
		{
			ContentTypeFilter = HttpResponseFilter.Instance;
			RequestFilters = new List<Action<IHttpRequest, IHttpResponse, object>>();
			ResponseFilters = new List<Action<IHttpRequest, IHttpResponse, object>>();
			HtmlProviders = new List<StreamSerializerResolverDelegate>();
			CatchAllHandlers = new List<HttpHandlerResolverDelegate>();
			Plugins = new List<IPlugin>();
		}
		
		// Pre user config
		public static void ConfigureHost(IAppHost appHost, string serviceName, ServiceManager serviceManager)
		{
			AppHost = appHost;

			EndpointHostConfig.Instance.ServiceName = serviceName;
			EndpointHostConfig.Instance.ServiceManager = serviceManager;

			var config = EndpointHostConfig.Instance;
			Config = config; // avoid cross-dependency on Config setter

			Plugins = new List<IPlugin> {
				new HtmlFormat(),
				new CsvFormat(),
				new MarkdownFormat(),
			};
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

			var specifiedContentType = config.DefaultContentType; //Before plugins loaded

			AppHost.LoadPlugin(Plugins.ToArray());

			AfterPluginsLoaded(specifiedContentType);
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
				foreach (var requestFilter in RequestFilters)
				{
					requestFilter(httpReq, httpRes, requestDto);
					if (httpRes.IsClosed) break;
				}

				var attributes = FilterAttributeCache.GetRequestFilterAttributes(requestDto.GetType());
				foreach (var attribute in attributes)
				{
					EndpointHost.ServiceManager.Container.AutoWire(attribute);
					attribute.RequestFilter(httpReq, httpRes, requestDto);
					if (EndpointHost.AppHost != null) //tests
						EndpointHost.AppHost.Release(attribute);
					if (httpRes.IsClosed) break;
				}

				return httpRes.IsClosed;
			}
		}

		/// <summary>
		/// Applies the response filters. Returns whether or not the request has been handled 
		/// and no more processing should be done.
		/// </summary>
		/// <returns></returns>
		public static bool ApplyResponseFilters(IHttpRequest httpReq, IHttpResponse httpRes, object responseDto)
		{
			httpReq.ThrowIfNull("httpReq");
			httpRes.ThrowIfNull("httpRes");

			using (Profiler.Current.Step("Executing Response Filters"))
			{
				foreach (var responseFilter in ResponseFilters)
				{
					responseFilter(httpReq, httpRes, responseDto);
					if (httpRes.IsClosed) break;
				}

				if (responseDto != null)
				{
					var httpResult = responseDto as IHttpResult;
					if (httpResult != null)
						responseDto = httpResult.Response;

					if (responseDto != null)
					{
						var attributes = FilterAttributeCache.GetResponseFilterAttributes(responseDto.GetType());
						foreach (var attribute in attributes)
						{
							EndpointHost.ServiceManager.Container.AutoWire(attribute);
							attribute.ResponseFilter(httpReq, httpRes, responseDto);
							if (EndpointHost.AppHost != null) //tests
								EndpointHost.AppHost.Release(attribute);
							if (httpRes.IsClosed) break;
						}
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

	}
}