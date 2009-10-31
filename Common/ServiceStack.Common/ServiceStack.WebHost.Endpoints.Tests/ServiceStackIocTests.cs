using System;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	public class ServiceStackIocTests
	{
		public class AppHost
			: ServiceStackWebHost
		{
			public AppHost(string serviceName) : base(serviceName)
			{
			}
		}
	}

	public abstract class ServiceStackWebHost
	{
		protected ServiceStackWebHost(string serviceName)
		{
			this.Config = new HostConfig {
				ServiceName = serviceName,				
			};
		}

		protected HostConfig Config { get; private set; }
	}

}