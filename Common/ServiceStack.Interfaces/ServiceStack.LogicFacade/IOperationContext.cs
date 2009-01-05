using System;

namespace ServiceStack.LogicFacade
{
	public interface IOperationContext : IDisposable
	{
		IApplicationContext Application { get; set; }
		
		IRequestContext Request { get; set; }
	}
}