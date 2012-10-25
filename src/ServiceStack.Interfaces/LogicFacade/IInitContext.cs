using System;

namespace ServiceStack.LogicFacade
{
	public interface IInitContext : IDisposable
	{
		object InitialisedObject
		{
			get;
		}
	}
}