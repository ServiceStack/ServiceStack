using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Caching
{
    /// <summary>
    /// A Users Session
    /// </summary>
    public interface ISessionAsync
    {
        /// <summary>
        /// Set a typed value at key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        Task SetAsync<T>(string key, T value, CancellationToken token=default);

        /// <summary>
        /// Get a typed value at key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<T> GetAsync<T>(string key, CancellationToken token=default);

        /// <summary>
        /// Remove the value at key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<bool> RemoveAsync(string key, CancellationToken token=default);

        /// <summary>
        /// Delete all Cache Entries (requires ICacheClient that implements IRemoveByPattern)
        /// </summary>
        Task RemoveAllAsync(CancellationToken token=default);
    }
}