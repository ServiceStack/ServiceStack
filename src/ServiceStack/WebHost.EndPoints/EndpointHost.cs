using System;
using System.Collections.Generic;
using System.Reflection;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.WebHost.Endpoints.Formats;

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

		static EndpointHost()
		{
			ContentTypeFilter = HttpResponseFilter.Instance;
			RequestFilters = new List<Action<IHttpRequest, IHttpResponse, object>>();
			ResponseFilters = new List<Action<IHttpRequest, IHttpResponse, object>>();
		}

		// Pre user config
		public static void ConfigureHost(IAppHost appHost, string serviceName, Assembly[] assembliesWithServices)
		{
			AppHost = appHost;

			EndpointHostConfig.Instance.ServiceName = serviceName;
			EndpointHostConfig.Instance.ServiceManager = new ServiceManager(assembliesWithServices);

            var config = EndpointHostConfig.Instance;
		    Config = config; // avoid cross-dependency on Config setter

			ContentCacheManager.ContentTypeFilter = appHost.ContentTypeFilters;
			HtmlFormat.Register(appHost);
			CsvFormat.Register(appHost);
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
			config.ServiceManager.AfterInit();
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
			internal get
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
			foreach (var requestFilter in RequestFilters)
			{
				requestFilter(httpReq, httpRes, requestDto);
				if (httpRes.IsClosed) break;
			}

			return httpRes.IsClosed;
		}

		/// <summary>
		/// Applies the response filters. Returns whether or not the request has been handled 
		/// and no more processing should be done.
		/// </summary>
		/// <returns></returns>
		public static bool ApplyResponseFilters(IHttpRequest httpReq, IHttpResponse httpRes, object responseDto)
		{
			foreach (var responseFilter in ResponseFilters)
			{
				responseFilter(httpReq, httpRes, responseDto);
				if (httpRes.IsClosed) break;
			}

			return httpRes.IsClosed;
		}

		internal static void SetOperationTypes(ServiceOperations operationTypes, ServiceOperations allOperationTypes)
		{
			ServiceOperations = operationTypes;
			AllServiceOperations = allOperationTypes;
		}

		internal static object ExecuteService(object request, EndpointAttributes endpointAttributes, IHttpRequest httpReq)
		{
			return config.ServiceController.Execute(request,
				new HttpRequestContext(httpReq, request, endpointAttributes));
		}

	}
}