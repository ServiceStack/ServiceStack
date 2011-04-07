namespace ServiceStack.Configuration
{
	public interface IContainerAdapter
	{
		T TryResolve<T>();
		T Resolve<T>();
	}
}