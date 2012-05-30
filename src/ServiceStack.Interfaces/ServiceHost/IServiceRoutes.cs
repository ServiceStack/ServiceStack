namespace ServiceStack.ServiceHost
{
    /// <summary>
    /// Allow the registration of user-defined routes for services
    /// </summary>
    public interface IServiceRoutes
    {
        /// <summary>
        ///		Maps the specified REST path to the specified request DTO.
        /// </summary>
        /// <typeparam name="TRequest">The type of request DTO to map 
        ///		the path to.</typeparam>
        /// <param name="restPath">The path to map the request DTO to.
        ///		See <see cref="RestServiceAttribute.Path">RestServiceAttribute.Path</see>
        ///		for details on the correct format.</param>
        /// <returns>The same <see cref="IServiceRoutes"/> instance;
        ///		never <see langword="null"/>.</returns>
        IServiceRoutes Add<TRequest>(string restPath);

        /// <summary>
        ///		Maps the specified REST path to the specified request DTO, and
        ///		specifies the HTTP verbs supported by the path.
        /// </summary>
        /// <typeparam name="TRequest">The type of request DTO to map 
        ///		the path to.</typeparam>
        /// <param name="restPath">The path to map the request DTO to.
        ///		See <see cref="RestServiceAttribute.Path">RestServiceAttribute.Path</see>
        ///		for details on the correct format.</param>
        /// <param name="verbs">
        ///		The comma-delimited list of HTTP verbs supported by the path, 
        ///		such as "GET,PUT,DELETE".  Specify empty or <see langword="null"/>
        ///		to indicate that all verbs are supported.
        /// </param>
        /// <returns>The same <see cref="IServiceRoutes"/> instance;
        ///		never <see langword="null"/>.</returns>
        IServiceRoutes Add<TRequest>(string restPath, string verbs);

        /// <summary>
        ///		Maps the specified REST path to the specified request DTO, 
        ///		specifies the HTTP verbs supported by the path, and indicates
        ///		the default MIME type of the returned response.
        /// </summary>
        /// <typeparam name="TRequest">The type of request DTO to map 
        ///		the path to.</typeparam>
        /// <param name="restPath">The path to map the request DTO to.
        ///		See <see cref="RestServiceAttribute.Path">RestServiceAttribute.Path</see>
        ///		for details on the correct format.</param>
        /// <param name="verbs">
        ///		The comma-delimited list of HTTP verbs supported by the path, 
        ///		such as "GET,PUT,DELETE".
        /// </param>
        /// <param name="defaultContentType">
        ///		The default MIME type in which the response object returned to the client
        ///		is formatted, if formatting hints are not provided by the client.
        ///		Specify <see langword="null"/> or empty to require formatting hints from
        ///		the client.
        /// </param>
        /// <returns>The same <see cref="IServiceRoutes"/> instance;
        ///		never <see langword="null"/>.</returns>
        IServiceRoutes Add<TRequest>(string restPath, string verbs, string defaultContentType);

        /// <summary>
        ///		Maps the specified REST path to the specified request DTO, 
        ///		specifies the HTTP verbs supported by the path, and indicates
        ///		the default MIME type of the returned response.
        /// </summary>
        /// <param name="requestType">
        ///		The type of request DTO to map the path to.
        /// </param>
        /// <param name="restPath">The path to map the request DTO to.
        ///		See <see cref="RestServiceAttribute.Path">RestServiceAttribute.Path</see>
        ///		for details on the correct format.</param>
        /// <param name="verbs">
        ///		The comma-delimited list of HTTP verbs supported by the path, 
        ///		such as "GET,PUT,DELETE".
        /// </param>
        /// <param name="defaultContentType">
        ///		The default MIME type in which the response object returned to the client
        ///		is formatted, if formatting hints are not provided by the client.
        ///		Specify <see langword="null"/> or empty to require formatting hints from
        ///		the client.
        /// </param>
        /// <returns>The same <see cref="IServiceRoutes"/> instance;
        ///		never <see langword="null"/>.</returns>
        IServiceRoutes Add(System.Type requestType, string restPath, string verbs, string defaultContentType);
    }
}