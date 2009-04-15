namespace ServiceStack.Service
{
	public interface IServiceHost
	{
		object ExecuteService(object request, EndpointAttributes endpointAttributes);
		string ExecuteXmlService(string xmlRequest, EndpointAttributes endpointAttributes);
	}
}