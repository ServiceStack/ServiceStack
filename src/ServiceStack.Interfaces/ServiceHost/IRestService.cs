using System;

namespace ServiceStack.ServiceHost
{
	/// <summary>
	/// Utility interface that implements all Rest operations
	/// </summary>
	/// <typeparam name="T"></typeparam>
    [Obsolete("Use IService - ServiceStack's New API for future services")]
    public interface IRestService<T> :
		IRestGetService<T>,
		IRestPostService<T>,
		IRestPutService<T>,
		IRestDeleteService<T>,
		IRestPatchService<T>
	{
	}
}