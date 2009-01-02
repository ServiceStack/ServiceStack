using System;

namespace ServiceStack.LogicFacade
{
	public interface ICallContext : IDisposable
	{
		IOperationContext Operation { get; set; }
		IRequestContext Request { get; set; }
	}
}