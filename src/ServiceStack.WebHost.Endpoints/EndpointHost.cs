using System;
using System.Collections.Generic;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints
{
	public class EndpointHost
	{
		public static ServiceOperations ServiceOperations { get; private set; }
		public static ServiceOperations AllServiceOperations { get; private set; }

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

		internal static void SetOperationTypes(ServiceOperations operationTypes, ServiceOperations allOperationTypes)
		{
			ServiceOperations = operationTypes;
			AllServiceOperations = allOperationTypes;
		}

		internal static object ExecuteService(object request, EndpointAttributes endpointAttributes)
		{
			return config.ServiceController.Execute(request,
				new HttpRequestContext(request, endpointAttributes));
		}

	}
}