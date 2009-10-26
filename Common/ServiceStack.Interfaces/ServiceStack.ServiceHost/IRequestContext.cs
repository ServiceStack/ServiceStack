using System;

namespace ServiceStack.ServiceHost
{
	public interface IRequestContext : IDisposable
	{
		T Get<T>() where T : class;
		
		string IpAddress { get; }

		EndpointAttributes EndpointAttributes { get; }
		
		IRequestAttributes RequestAttributes { get; }
	}
}