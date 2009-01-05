using System;

namespace ServiceStack.LogicFacade
{
	public interface IServiceController
	{
		object Execute(IOperationContext context);
		
		string ExecuteXml(IOperationContext context);
	}
}