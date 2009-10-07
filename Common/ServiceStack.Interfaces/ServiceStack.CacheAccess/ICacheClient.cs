using System;
using System.Collections.Generic;

namespace ServiceStack.CacheAccess
{
	/// <summary>
	/// A light interface over a cache client.
	/// This interface was inspired by Enyim.Caching.MemcachedClient
	/// 
	/// Only the methods that are intended to be used are required, if you require
	/// extra functionality you can uncomment the unused methods below as they have been
	/// implemented in DdnMemcachedClient
	/// </summary>
	public interface ICacheClient : IDisposable
	{
		/// <summary>
		/// Removes the specified item from the cache.
		/// </summary>
		/// <param name="key">The identifier for the item to delete.</param>
		/// <returns>
		/// true if the item was successfully removed from the cache; false otherwise.
		/// </returns>
		bool Remove(string key);

		/// <summary>
		/// Removes the cache for all the keys provided.
		/// </summary>
		/// <param name="keys">The keys.</param>
		void RemoveAll(IEnumerable<string> keys);

		/// <summary>
		/// Retrieves the specified item from the cache.
		/// </summary>
		/// <param name="key">The identifier for the item to retrieve.</param>
		/// <returns>
		/// The retrieved item, or <value>null</value> if the key was not found.
		/// </returns>
		object Get(string key);

		/// <summary>
		/// Retrieves the specified item from the cache.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key">The identifier for the item to retrieve.</param>
		/// <returns>
		/// The retrieved item, or <value>null</value> if the key was not found.
		/// </returns>
		T Get<T>(string key);

		/// <summary>
		/// Gets the specified key.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key">The key.</param>
		/// <param name="ucas">The ucas.</param>
		/// <returns></returns>
		T Get<T>(string key, out ulong ucas);

		/// <summary>
		/// Increments the value of the specified key by the given amount. The operation is atomic and happens on the server.
		/// </summary>
		/// <param name="key">The identifier for the item to increment.</param>
		/// <param name="amount">The amount by which the client wants to increase the item.</param>
		/// <returns>
		/// The new value of the item or -1 if not found.
		/// </returns>
		/// <remarks>The item must be inserted into the cache before it can be changed. The item must be inserted as a <see cref="T:System.String"/>. The operation only works with <see cref="System.UInt32"/> values, so -1 always indicates that the item was not found.</remarks>
		long Increment(string key, uint amount);

		/// <summary>
		/// Increments the value of the specified key by the given amount. The operation is atomic and happens on the server.
		/// </summary>
		/// <param name="key">The identifier for the item to increment.</param>
		/// <param name="amount">The amount by which the client wants to decrease the item.</param>
		/// <returns>
		/// The new value of the item or -1 if not found.
		/// </returns>
		/// <remarks>The item must be inserted into the cache before it can be changed. The item must be inserted as a <see cref="T:System.String"/>. The operation only works with <see cref="System.UInt32"/> values, so -1 always indicates that the item was not found.</remarks>
		long Decrement(string key, uint amount);

		/// <summary>
		/// Inserts an item into the cache with a cache key to reference its location.
		/// </summary>
		/// <param name="key">The key used to reference the item.</param>
		/// <param name="value">The object to be inserted into the cache.</param>
		/// <returns>
		/// true if the item was successfully stored in the cache; false otherwise.
		/// </returns>
		/// <remarks>The item does not expire unless it is removed due memory pressure.</remarks>
		bool Add(string key, object value);
		bool Set(string key, object value);
		bool Replace(string key, object value);

		/// <summary>
		/// Inserts an item into the cache with a cache key to reference its location.
		/// </summary>
		/// <param name="key">The key used to reference the item.</param>
		/// <param name="value">The object to be inserted into the cache.</param>
		/// <param name="expiresAt">The time when the item is invalidated in the cache.</param>
		/// <returns>true if the item was successfully stored in the cache; false otherwise.</returns>
		bool Add(string key, object value, DateTime expiresAt);
		bool Set(string key, object value, DateTime expiresAt);
		bool Replace(string key, object value, DateTime expiresAt);

		/// <summary>
		/// Updates an item in the cache with a cache key to reference its location, but only if it has not been changed since the last retrieval. The invoker must pass in the value returned by <see cref="M:MultiGet"/> called "lastModifiedValue" value. If this value matches the server's value, the item will be updated; otherwise the update fails.
		/// </summary>
		/// <param name="key">The key used to reference the item.</param>
		/// <param name="value">The object to be inserted into the cache.</param>
		/// <param name="lastModifiedValue">The unique value returned by <see cref="M:MultiGet"/>.</param>
		/// <returns></returns>
		/// <remarks>The item does not expire unless it is removed due memory pressure.</remarks>
		bool CheckAndSet(string key, object value, ulong lastModifiedValue);

		/// <summary>
		/// Updates an item in the cache with a cache key to reference its location, but only if it has not been changed since the last retrieval. The invoker must pass in the value returned by <see cref="M:MultiGet"/> called "lastModifiedValue" value. If this value matches the server's value, the item will be updated; otherwise the update fails.
		/// </summary>
		/// <param name="key">The key used to reference the item.</param>
		/// <param name="value">The object to be inserted into the cache.</param>
		/// <param name="lastModifiedValue">The unique value returned by <see cref="M:MultiGet"/>.</param>
		/// <param name="expiresAt">The time when the item is invalidated in the cache.</param>
		/// <returns></returns>
		bool CheckAndSet(string key, object value, ulong lastModifiedValue, DateTime expiresAt);

		/// <summary>
		/// Removes all data from the cache.
		/// </summary>
		void FlushAll();

		/// <summary>
		/// Retrieves multiple items from the cache.
		/// </summary>
		/// <param name="keys">The list of identifiers for the items to retrieve.</param>
		/// <returns>
		/// a Dictionary holding all items indexed by their key.
		/// </returns>
		IDictionary<string, object> GetAll(IEnumerable<string> keys);

		/// Retrieves multiple items from the cache.
		/// </summary>
		/// <param name="keys">The list of identifiers for the items to retrieve.</param>
		/// <returns>
		/// a Dictionary holding all items indexed by their key.
		/// </returns>
		IDictionary<string, T> GetAll<T>(IEnumerable<string> keys);

		/// <summary>
		/// Retrieves multiple items from the cache.
		/// </summary>
		/// <param name="keys">The list of identifiers for the items to retrieve.</param>
		/// <param name="lastModifiedValues">The CAS values for the keys.</param>
		/// <returns>
		/// a Dictionary holding all items indexed by their key.
		/// </returns>
		IDictionary<string, object> GetAll(IEnumerable<string> keys, out IDictionary<string, ulong> lastModifiedValues);
	}
}