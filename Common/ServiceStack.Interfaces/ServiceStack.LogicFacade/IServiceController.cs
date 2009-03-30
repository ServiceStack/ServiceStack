using System;
using System.Collections.Generic;

namespace ServiceStack.LogicFacade
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
		/// Executes the specified context given a DTO request.
		/// Will return a DTO response for SyncReply services.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <returns></returns>
		object Execute(IOperationContext context);

		/// <summary>
		/// Executes the specified context given an XML request.
		/// Will return an XML response
		/// </summary>
		/// <param name="context">The context.</param>
		/// <returns></returns>
		string ExecuteXml(IOperationContext context);
	}
}