namespace ServiceStack.ServiceHost
{
	/// <summary>
	/// If the Service also implements this interface,
	/// IRestPutService.Patch() will be used instead of IService.Execute() for 
	/// EndpointAttributes.HttpPatch requests
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IRestPatchService<T>
	{
		object Patch(T request);
	}
}