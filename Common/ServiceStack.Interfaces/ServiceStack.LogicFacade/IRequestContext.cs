using System;

namespace ServiceStack.LogicFacade
{
	public interface IRequestContext : IDisposable
	{
		object Dto { get; set; }
		T Get<T>() where T : class;
		string IpAddress { get; }
	}
}