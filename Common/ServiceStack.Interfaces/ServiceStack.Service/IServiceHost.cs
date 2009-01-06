namespace ServiceStack.Service
{
	public interface IServiceHost
	{
		object ExecuteService(object request);

		string ExecuteXmlService(string xmlRequest);
	}
}