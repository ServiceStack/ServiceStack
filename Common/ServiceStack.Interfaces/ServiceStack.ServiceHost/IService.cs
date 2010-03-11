namespace ServiceStack.ServiceHost
{
	/// <summary>
	/// Base interface all webservices need to implement.
	/// For simplicity this is the only interface you need to implement
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IService<T> 
	{
		object Execute(T request);
	}

	/// <summary>
	/// If the Service also implements this interface,
	/// IAsyncService.ExecuteAsync() will be used instead of IService.Execute() for 
	/// EndpointAttributes.AsyncOneWay requests
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IAsyncService<T>
	{
		void ExecuteAsync(T request);
	}


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

	/// <summary>
	/// If the Service also implements this interface,
	/// IRestPostService.Post() will be used instead of IService.Execute() for 
	/// EndpointAttributes.HttpPost requests
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IRestPostService<T>
	{
		object Post(T request);
	}

	/// <summary>
	/// If the Service also implements this interface,
	/// IRestPutService.Put() will be used instead of IService.Execute() for 
	/// EndpointAttributes.HttpPut requests
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IRestPutService<T>
	{
		object Put(T request);
	}

	/// <summary>
	/// If the Service also implements this interface,
	/// IRestDeleteService.Delete() will be used instead of IService.Execute() for 
	/// EndpointAttributes.HttpDelete requests
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IRestDeleteService<T>
	{
		object Delete(T request);
	}

	/// <summary>
	/// Utility interface that implements all Rest operations
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IRestService<T> :
		IRestGetService<T>,
		IRestPostService<T>,
		IRestPutService<T>,
		IRestDeleteService<T>
	{
	}

}
