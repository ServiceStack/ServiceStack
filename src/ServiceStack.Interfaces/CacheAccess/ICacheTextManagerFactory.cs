namespace ServiceStack.CacheAccess
{
	public interface ICacheTextManagerFactory
	{
		ICacheTextManager Resolve(string contentType);
	}
}