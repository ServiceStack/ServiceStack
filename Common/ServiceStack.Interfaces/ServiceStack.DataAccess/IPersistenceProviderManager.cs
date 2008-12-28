namespace ServiceStack.DataAccess
{
	/// <summary>
	/// Manages a connection to a persistance provider
	/// </summary>
	public interface IPersistenceProviderManager
	{
		string ConnectionString { get; }
		IPersistenceProvider CreateProvider();
	}
}