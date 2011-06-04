namespace ServiceStack.ServiceHost
{
	/// <summary>
	/// Allow the registration of user-defined routes for services
	/// </summary>
	public interface IServiceRoutes
	{
		/// <summary>
		/// Register the user-defined restPath for the Service's Request DTO TRequest
		/// </summary>
		/// <typeparam name="TRequest"></typeparam>
		/// <param name="restPath"></param>
		/// <returns></returns>
		IServiceRoutes Add<TRequest>(string restPath);

		/// <summary>
		/// Register the user-defined restPath, HTTP verbs it applies to (empty == all)
		/// </summary>
		/// <typeparam name="TRequest"></typeparam>
		/// <param name="restPath"></param>
		/// <param name="verbs">comma-delimited verbs e.g. GET,POST,PUT,DELETE</param>
		/// <returns></returns>
		IServiceRoutes Add<TRequest>(string restPath, string verbs);

		/// <summary>
		/// Register the user-defined restPath, HTTP verbs it applies to (empty == all) and
		/// the defaultContentType the service should return if not specified by the client
		/// </summary>
		/// <typeparam name="TRequest"></typeparam>
		/// <param name="restPath"></param>
		/// <param name="verbs">comma-delimited verbs e.g. GET,POST,PUT,DELETE</param>
		/// <param name="defaultContentType"></param>
		/// <returns></returns>
		IServiceRoutes Add<TRequest>(string restPath, string verbs, string defaultContentType);

        /// <summary>
        /// Register the user-defined restPath, HTTP verbs it applies to (empty == all) and
        /// the defaultContentType the service should return if not specified by the client
        /// </summary>
        /// <param name="requestType"></param>
        /// <param name="restPath"></param>
        /// <param name="verbs">comma-delimited verbs e.g. GET,POST,PUT,DELETE.  Pass null to allow all verbs.</param>
        /// <param name="defaultContentType">Pass null to use default.</param>
        /// <returns></returns>
        IServiceRoutes Add(System.Type requestType, string restPath, string verbs, string defaultContentType);
	}
}