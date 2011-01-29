using System;
using System.Collections.Generic;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints
{
	public class EndpointHost
	{
		public static ServiceOperations ServiceOperations { get; private set; }
		public static ServiceOperations AllServiceOperations { get; private set; }

		public static List<Action<IHttpRequest, IHttpResponse, object>> RequestFilters { get; private set; }

		public static List<Action<IHttpRequest, IHttpResponse, object>> ResponseFilters { get; private set; }

		static EndpointHost()
		{
			RequestFilters = new List<Action<IHttpRequest, IHttpResponse, object>>();
			ResponseFilters = new List<Action<IHttpRequest, IHttpResponse, object>>();
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
				get { return Config != null ? Config.DebugMode : false; }
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