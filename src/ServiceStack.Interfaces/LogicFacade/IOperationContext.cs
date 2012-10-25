using System;
using ServiceStack.ServiceHost;

namespace ServiceStack.LogicFacade
{
	public interface IOperationContext : IDisposable
	{
		IApplicationContext Application { get;  }
		
		IRequestContext Request { get; }
	}
}