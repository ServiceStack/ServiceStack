using System;

namespace ServiceStack.DataAccess
{
	/// <summary>
	/// Manages a connection to a persistance provider
	/// </summary>
	public interface IPersistenceProviderManager : IDisposable
	{
		IPersistenceProvider GetProvider();
	}
}