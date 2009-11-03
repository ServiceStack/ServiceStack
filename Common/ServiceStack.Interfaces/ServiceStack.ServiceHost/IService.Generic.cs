namespace ServiceStack.ServiceHost
{
	public interface IService<T> 
	{
		object Execute(T request);
	}
}