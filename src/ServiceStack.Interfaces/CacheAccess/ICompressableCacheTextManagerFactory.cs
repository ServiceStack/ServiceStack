namespace ServiceStack.CacheAccess
{
	public interface ICompressableCacheTextManagerFactory
	{
		ICompressableCacheTextManager Resolve(string contentType);
	}
}