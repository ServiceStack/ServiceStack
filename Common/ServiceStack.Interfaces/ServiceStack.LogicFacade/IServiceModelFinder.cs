using System;
using System.Reflection;

namespace ServiceStack.LogicFacade
{
	public interface IServiceModelFinder
	{
		Type FindTypeByOperation(string operationName, int? version);
		Assembly ServiceModelAssembly { get; }
	}
}