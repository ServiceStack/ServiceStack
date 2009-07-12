using System;
using ServiceStack.CacheAccess;
using ServiceStack.Configuration;
using ServiceStack.LogicFacade;

namespace ServiceStack.ServiceInterface
{

	public class ApplicationContext : IApplicationContext
	{
		public static IApplicationContext Instance { get; private set; }

		public static void SetInstanceContext(IApplicationContext applicationContext)
		{
			if (ApplicationContext.Instance != null)
			{
				throw new NotSupportedException("Cannot set the singleton instance once it has already been set");
			}
			ApplicationContext.Instance = applicationContext;
		}

		[ThreadStatic]
		public static ApplicationContext current;
		public static ApplicationContext Current
		{
			get { return current; }
			set { current = value; }
		}

		public ICacheClient Cache { get; set; }

		public IResourceManager Resources { get; set; }

		public IFactoryProvider Factory { get; set; }

		public T Get<T>() where T : class
		{
			return this.Factory.Resolve<T>();
		}
	}
}