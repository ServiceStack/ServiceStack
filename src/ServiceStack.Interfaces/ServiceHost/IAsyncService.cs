namespace ServiceStack.ServiceHost
{
	/// <summary>
	/// If the Service also implements this interface,
	/// IAsyncService.ExecuteAsync() will be used instead of IService.Execute() for 
	/// EndpointAttributes.AsyncOneWay requests
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IAsyncService<T>
	{
		object ExecuteAsync(T request);
	}
}