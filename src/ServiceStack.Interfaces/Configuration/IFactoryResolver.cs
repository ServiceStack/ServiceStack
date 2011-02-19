namespace ServiceStack.Configuration
{
	public interface IFactoryResolver
	{
		T Resolve<T>();
	}
}