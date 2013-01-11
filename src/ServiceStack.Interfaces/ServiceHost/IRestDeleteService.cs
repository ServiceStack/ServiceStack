using System;

namespace ServiceStack.ServiceHost
{
	/// <summary>
	/// If the Service also implements this interface,
	/// IRestDeleteService.Delete() will be used instead of IService.Execute() for 
	/// EndpointAttributes.HttpDelete requests
	/// </summary>
	/// <typeparam name="T"></typeparam>
    [Obsolete("Use IService - ServiceStack's New API for future services")]
    public interface IRestDeleteService<T>
	{
		object Delete(T request);
	}
}