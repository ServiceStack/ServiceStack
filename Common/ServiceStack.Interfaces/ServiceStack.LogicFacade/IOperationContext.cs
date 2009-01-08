using System;

namespace ServiceStack.LogicFacade
{
	public interface IOperationContext : IDisposable
	{
		IApplicationContext Application { get;  }
		
		IRequestContext Request { get; }
	}
}