namespace ServiceStack.CacheAccess
{
	/// <summary>
	/// A Users Session
	/// </summary>
	public interface ISession
	{
		/// <summary>
		/// Store any object at key
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		object this[string key] { get; set; }

		/// <summary>
		/// Set a typed value at key
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <param name="value"></param>
		void Set<T>(string key, T value);
		
		/// <summary>
		/// Get a typed value at key
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <returns></returns>
		T Get<T>(string key);
	}
}