namespace ServiceStack.ServiceHost
{
	public interface IService<T> 
		: IService
	{
		object Execute(T request);
	}
}