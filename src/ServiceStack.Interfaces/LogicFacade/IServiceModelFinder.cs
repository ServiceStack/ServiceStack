using System;
using System.Reflection;

namespace ServiceStack.LogicFacade
{
	/// <summary>
	/// The same functionality is on IServiceResolver
	/// </summary>
	[Obsolete]
	public interface IServiceModelFinder
	{
		Type FindTypeByOperation(string operationName, int? version);
	}
}