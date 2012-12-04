using System;
using System.Collections.Generic;
using ServiceStack.Messaging;

namespace ServiceStack.ServiceHost
{
	/// <summary>
	/// Responsible for executing the operation within the specified context.
	/// </summary>
	/// <value>The operation types.</value>
	public interface IServiceController
	{
		/// <summary>
		/// Returns the first matching RestPath
		/// </summary>
		/// <param name="httpMethod"></param>
		/// <param name="pathInfo"></param>
		/// <returns></returns>
		IRestPath GetRestPathForRequest(string httpMethod, string pathInfo);

		/// <summary>
		/// Allow the registration of custom routes
		/// </summary>
		IServiceRoutes Routes { get; }

        /// <summary>
        /// Executes the MQ DTO request.
        /// </summary>
        object ExecuteMessage<T>(IMessage<T> mqMessage);

        /// <summary>
        /// Executes the MQ DTO request with the supplied requestContext
        /// </summary>
	    object ExecuteMessage<T>(IMessage<T> dto, IRequestContext requestContext);

		/// <summary>
		/// Executes the DTO request under the supplied requestContext.
		/// </summary>
		/// <param name="request"></param>
		/// <param name="requestContext"></param>
		/// <returns></returns>
		object Execute(object request, IRequestContext requestContext);
	}
}