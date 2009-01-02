using System;
using System.Reflection;

namespace ServiceStack.WebHost.Endpoints
{
	public class EndpointHost
	{
		public static EndpointHostConfig Config { internal get; set; }

		public static string GetOperationTypeFullName(string operationTypeName)
		{
			return Config.OperationsNamespace + "." + operationTypeName;
		}

		public static Type GetOperationType(string operationTypeName)
		{
			return ServiceModelAssembly.GetType(Config.OperationsNamespace + "." + operationTypeName);
		}

		internal static object ExecuteService(object request)
		{
			return Config.ServiceHost.ExecuteService(request);
		}

		internal static string ExecuteXmlService(string xmlRequest)
		{
			return Config.ServiceHost.ExecuteXmlService(xmlRequest);
		}

		internal static Assembly ServiceModelAssembly
		{
			get { return Config.ModelInfo.GetType().Assembly; }
		}
	}
}