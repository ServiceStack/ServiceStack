namespace ServiceStack.ServiceInterface
{
	public interface IServiceResolver
	{
		object FindService(string serviceName);
		object FindService(string serviceName, int version);
	}
}