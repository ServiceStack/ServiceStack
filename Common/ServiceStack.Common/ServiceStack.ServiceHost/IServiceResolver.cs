using System;
using System.Collections.Generic;

namespace ServiceStack.ServiceHost
{
	/// <summary>
	/// Provides service resolution functionality to retrieve a service based on a operationName and a version.
	/// The service resolver also lists all operation types available for this service.
	/// </summary>
	public interface IServiceResolver
	{
		///// <summary>
		///// Returns a list of operation types available in this service
		///// </summary>
		///// <value>The operation types.</value>
		//IList<Type> OperationTypes { get; }

		///// <summary>
		///// Returns a list of All operation types available in this service. needed for WSDL generation
		///// </summary>
		///// <value>The operation types.</value>
		//IList<Type> AllOperationTypes { get; }

		///// <summary>
		///// Finds the handler when no version is provided.
		///// </summary>
		///// <param name="operationName">Name of the service.</param>
		///// <returns></returns>
		//object FindService(string operationName);

		///// <summary>
		///// Finds the handler for the specified version.
		///// </summary>
		///// <param name="operationName">Name of the service.</param>
		///// <param name="version">The version.</param>
		///// <returns></returns>
		//object FindService(string operationName, int? version);

		///// <summary>
		///// Finds the service model.
		///// </summary>
		///// <param name="operationName">Name of the operation.</param>
		///// <returns></returns>
		//Type FindOperationType(string operationName);

		///// <summary>
		///// Finds the specified version service model.
		///// </summary>
		///// <param name="operationName">Name of the operation.</param>
		///// <param name="version">The version.</param>
		///// <returns></returns>
		//Type FindOperationType(string operationName, int? version);

		///// <summary>
		///// Gets or sets the handler factory.
		///// </summary>
		///// <value>The handler factory.</value>
		//Func<Type, object> HandlerFactory { get; set; }
	}
}