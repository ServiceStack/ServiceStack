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
	}
}