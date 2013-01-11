using System;

namespace ServiceStack.ServiceHost
{
	/// <summary>
	/// If the Service also implements this interface,
	/// IRestPostService.Post() will be used instead of IService.Execute() for 
	/// EndpointAttributes.HttpPost requests
	/// </summary>
	/// <typeparam name="T"></typeparam>
    [Obsolete("Use IService - ServiceStack's New API for future services")]
    public interface IRestPostService<T>
	{
		object Post(T request);
	}
}