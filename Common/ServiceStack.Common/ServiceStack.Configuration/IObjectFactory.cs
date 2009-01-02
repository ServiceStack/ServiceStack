namespace ServiceStack.Configuration
{
	/// <summary>
	/// Provides a Object Factory to create factory instances
	/// </summary>
	public interface IObjectFactory
	{
		/// <summary>
		/// Creates this instance.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		T Create<T>();
		
		/// <summary>
		/// Creates an instance of type T
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="objectName">Name of the object.</param>
		/// <returns></returns>
		T Create<T>(string objectName);

		/// <summary>
		/// Determines whether the factory contains the specified object name.
		/// </summary>
		/// <param name="objectName">Name of the object.</param>
		/// <returns>
		/// 	<c>true</c> if it contains the specified object name; otherwise, <c>false</c>.
		/// </returns>
		bool Contains(string objectName);
	}
}