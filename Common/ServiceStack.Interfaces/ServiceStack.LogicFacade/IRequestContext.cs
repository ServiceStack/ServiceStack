using System;
using ServiceStack.Service;

namespace ServiceStack.LogicFacade
{
	public interface IRequestContext : IDisposable
	{
		T Get<T>() where T : class;

		object Dto { get; set; }
		
		string IpAddress { get; }

		EndpointAttributes EndpointAttributes { get; }
	}
}