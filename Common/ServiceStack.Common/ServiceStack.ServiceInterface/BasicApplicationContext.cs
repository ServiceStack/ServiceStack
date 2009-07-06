using ServiceStack.CacheAccess;
using ServiceStack.Configuration;
using ServiceStack.LogicFacade;

namespace ServiceStack.ServiceInterface
{
	/// <summary>
	/// A Basic Application Context for simple applications.
	/// It is good practice to create a statically typed Application Context to manage your 
	/// application singletons.
	/// </summary>
	public class BasicApplicationContext : IApplicationContext
	{
		public BasicApplicationContext(IFactoryProvider provider, ICacheClient cacheClient, IResourceManager resources)
		{
			this.Factory = provider;
			this.Cache = cacheClient;
			this.Resources = resources;
		}

		public T Get<T>() where T : class
		{
			return Factory.Resolve<T>();
		}

		public IFactoryProvider Factory { get; private set; }

		public ICacheClient Cache { get; private set; }

		public IResourceManager Resources { get; private set; }
	}
}
