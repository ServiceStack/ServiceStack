using System;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public class EndpointHandlerBase
	{
		protected static object ExecuteService(object request)
		{
			return EndpointHost.ExecuteService(request);
		}

		protected static string ExecuteXmlService(string xmlRequest)
		{
			return EndpointHost.ExecuteXmlService(xmlRequest);
		}

		protected static void AssertOperationExists(string operationName, Type type)
		{
			if (type == null)
			{
				throw new NotImplementedException(
					string.Format("The operation '{0}' does not exist for this service", operationName));
			}
		}
	}
}