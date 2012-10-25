namespace ServiceStack.ServiceHost
{
	/// <summary>
	/// If the Service also implements this interface,
	/// IRestGetService.Get() will be used instead of IService.Execute() for 
	/// EndpointAttributes.HttpGet requests
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IRestGetService<T>
	{
		object Get(T request);
	}
}