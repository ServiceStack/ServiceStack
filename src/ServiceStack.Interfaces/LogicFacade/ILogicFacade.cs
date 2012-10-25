using System;

namespace ServiceStack.LogicFacade
{
	public interface ILogicFacade : IDisposable
	{
		IInitContext AcquireInitContext(InitOptions options);
	}
}