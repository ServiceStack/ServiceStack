namespace ServiceStack.Configuration
{
	public interface IFactoryProvider
	{
		T Resolve<T>(string name);

		T ResolveOptional<T>(string name, T defaultValue);

		T Create<T>(string name);
	}
}