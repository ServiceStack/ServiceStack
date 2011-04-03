using System;
using System.Collections.Generic;

namespace ServiceStack.ServiceHost
{
	/// <summary>
	/// Responsible for executing the operation within the specified context.
	/// </summary>
	/// <value>The operation types.</value>
	public interface IServiceController
	{
		/// <summary>
		/// Returns a list of operation types available in this service
		/// </summary>
		/// <value>The operation types.</value>
		IList<Type> OperationTypes { get; }

		/// <summary>
		/// Returns a list of ALL operation types available in this service
		/// </summary>
		/// <value>The operation types.</value>
		IList<Type> AllOperationTypes { get; }

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
		/// Executes the DTO request under the supplied requestContext.
		/// </summary>
		/// <param name="request"></param>
		/// <param name="requestContext"></param>
		/// <returns></returns>
		object Execute(object request, IRequestContext requestContext);
	}
}