using System;
using System.Collections.Generic;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Metadata;

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

		internal static void SetOperationTypes(IList<Type> operationTypes, IList<Type> allOperationTypes)
		{
			ServiceOperations = new ServiceOperations(operationTypes);
			AllServiceOperations = new ServiceOperations(allOperationTypes);
		}

		internal static object ExecuteService(object request, EndpointAttributes endpointAttributes)
		{
			AssertConfig();
			return Config.ServiceController.Execute(request,
				new HttpRequestContext(request, endpointAttributes));
		}

		internal static string ExecuteXmlService(string xmlRequest, EndpointAttributes endpointAttributes)
		{
			AssertConfig();
			return (string)Config.ServiceController.ExecuteText(xmlRequest,
				new HttpRequestContext(xmlRequest, endpointAttributes));
		}

		private static void AssertConfig()
		{
			if (Config == null) throw new ArgumentNullException("Config");
		}
	}
}