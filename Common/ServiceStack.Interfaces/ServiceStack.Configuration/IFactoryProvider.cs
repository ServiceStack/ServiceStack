namespace ServiceStack.Configuration
{
	public interface IFactoryProvider
	{
		T Resolve<T>(string name);

		T Create<T>(string name);
	}
}