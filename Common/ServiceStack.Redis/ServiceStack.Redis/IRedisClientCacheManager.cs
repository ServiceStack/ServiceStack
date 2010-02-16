using ServiceStack.CacheAccess;

namespace ServiceStack.Redis
{
	public interface IRedisClientCacheManager
	{
		/// <summary>
		/// Returns a Read/Write ICacheClient (The default) using the hosts defined in ReadWriteHosts
		/// </summary>
		/// <returns></returns>
		ICacheClient GetClientCache();

		/// <summary>
		/// Returns a ReadOnly ICacheClient using the hosts defined in ReadOnlyHosts.
		/// </summary>
		/// <returns></returns>
		ICacheClient GetReadOnlyClientCache();

		/// <summary>
		/// Returns a Read/Write client (The default) using the hosts defined in ReadWriteHosts
		/// </summary>
		/// <returns></returns>
		IRedisClient GetClient();

		/// <summary>
		/// Returns a ReadOnly client using the hosts defined in ReadOnlyHosts.
		/// </summary>
		/// <returns></returns>
		IRedisClient GetReadOnlyClient();

		void Dispose();
	}
}