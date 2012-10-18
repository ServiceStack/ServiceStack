namespace ServiceStack.DataAccess
{
	public interface IPersistenceProviderManagerFactory
	{
		IPersistenceProviderManager CreateProviderManager(string connectionString);
	}
}