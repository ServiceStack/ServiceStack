using System;

namespace ServiceStack.LogicFacade
{
	public interface IServiceController
	{
		object Execute(ICallContext context);
		string ExecuteXml(ICallContext context);
	}
}