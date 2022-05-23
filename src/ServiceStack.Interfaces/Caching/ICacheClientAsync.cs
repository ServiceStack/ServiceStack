using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Caching
{
    public interface ICacheClientAsync
        : IAsyncDisposable
    {
        /// <summary>
        /// Removes the specified item from the cache.
        /// </summary>
        /// <param name="key">The identifier for the item to delete.</param>
        /// <returns>
        /// true if the item was successfully removed from the cache; false otherwise.
        /// </returns>
        Task<bool> RemoveAsync(string key, CancellationToken token=default);

        /// <summary>
        /// Removes the cache for all the keys provided.
        /// </summary>
        /// <param name="keys">The keys.</param>
        Task RemoveAllAsync(IEnumerable<string> keys, CancellationToken token=default);

        /// <summary>
        /// Retrieves the specified item from the cache.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The identifier for the item to retrieve.</param>
        /// <returns>
        /// The retrieved item, or <value>null</value> if the key was not found.
        /// </returns>
        Task<T> GetAsync<T>(string key, CancellationToken token=default);

        /// <summary>
        /// Increments the value of the specified key by the given amount. 
        /// The operation is atomic and happens on the server.
        /// A non existent value at key starts at 0
        /// </summary>
        /// <param name="key">The identifier for the item to increment.</param>
        /// <param name="amount">The amount by which the client wants to increase the item.</param>
        /// <returns>
        /// The new value of the item or -1 if not found.
        /// </returns>
        /// <remarks>The item must be inserted into the cache before it can be changed. The item must be inserted as a <see cref="T:System.String"/>. The operation only works with <see cref="System.UInt32"/> values, so -1 always indicates that the item was not found.</remarks>
        Task<long> IncrementAsync(string key, uint amount, CancellationToken token=default);

        /// <summary>
        /// Increments the value of the specified key by the given amount. 
        /// The operation is atomic and happens on the server.
        /// A non existent value at key starts at 0
        /// </summary>
        /// <param name="key">The identifier for the item to increment.</param>
        /// <param name="amount">The amount by which the client wants to decrease the item.</param>
        /// <returns>
        /// The new value of the item or -1 if not found.
        /// </returns>
        /// <remarks>The item must be inserted into the cache before it can be changed. The item must be inserted as a <see cref="T:System.String"/>. The operation only works with <see cref="System.UInt32"/> values, so -1 always indicates that the item was not found.</remarks>
        Task<long> DecrementAsync(string key, uint amount, CancellationToken token=default);

        /// <summary>
        /// Adds a new item into the cache at the specified cache key only if the cache is empty.
        /// </summary>
        /// <param name="key">The key used to reference the item.</param>
        /// <param name="value">The object to be inserted into the cache.</param>
        /// <returns>
        /// true if the item was successfully stored in the cache; false otherwise.
        /// </returns>
        /// <remarks>The item does not expire unless it is removed due memory pressure.</remarks>
        Task<bool> AddAsync<T>(string key, T value, CancellationToken token=default);

        /// <summary>
        /// Sets an item into the cache at the cache key specified regardless if it already exists or not.
        /// </summary>
        Task<bool> SetAsync<T>(string key, T value, CancellationToken token=default);

        /// <summary>
        /// Replaces the item at the cachekey specified only if an items exists at the location already. 
        /// </summary>
        Task<bool> ReplaceAsync<T>(string key, T value, CancellationToken token=default);

        Task<bool> AddAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token=default);
        Task<bool> SetAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token=default);
        Task<bool> ReplaceAsync<T>(string key, T value, DateTime expiresAt, CancellationToken token=default);

        Task<bool> AddAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token=default);
        Task<bool> SetAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token=default);
        Task<bool> ReplaceAsync<T>(string key, T value, TimeSpan expiresIn, CancellationToken token=default);

        /// <summary>
        /// Invalidates all data on the cache.
        /// </summary>
        Task FlushAllAsync(CancellationToken token=default);

        /// <summary>
        /// Retrieves multiple items from the cache. 
        /// The default value of T is set for all keys that do not exist.
        /// </summary>
        /// <param name="keys">The list of identifiers for the items to retrieve.</param>
        /// <param name="token"></param>
        /// <returns>
        /// a Dictionary holding all items indexed by their key.
        /// </returns>
        Task<IDictionary<string, T>> GetAllAsync<T>(IEnumerable<string> keys, CancellationToken token=default);

        /// <summary>
        /// Sets multiple items to the cache. 
        /// </summary>
        Task SetAllAsync<T>(IDictionary<string, T> values, CancellationToken token=default);
        
        Task<TimeSpan?> GetTimeToLiveAsync(string key, CancellationToken token=default);

        IAsyncEnumerable<string> GetKeysByPatternAsync(string pattern, CancellationToken token=default);
        
        Task RemoveExpiredEntriesAsync(CancellationToken token=default);
    }
}