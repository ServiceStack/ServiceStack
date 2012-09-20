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
}
