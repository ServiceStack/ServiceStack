namespace ServiceStack.CacheAccess
{
	public interface IHasCacheClient
	{
		ICacheClient CacheClient { get; }
	}
}