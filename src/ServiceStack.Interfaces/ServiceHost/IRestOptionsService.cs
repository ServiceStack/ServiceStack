namespace ServiceStack.ServiceHost
{
	/// <summary>
	/// If the Service also implements this interface,
    /// IRestPutService.Options() will be used instead of IService.Execute() for 
	/// EndpointAttributes.HttpPut requests
	/// </summary>
	/// <typeparam name="T"></typeparam>
    public interface IRestOptionsService<T>
	{
		object Options(T request);
	}
}