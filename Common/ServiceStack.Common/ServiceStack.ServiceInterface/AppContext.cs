using System;
using System.Collections.Generic;
using ServiceStack.CacheAccess;
using ServiceStack.Configuration;
using ServiceStack.Logging;

namespace ServiceStack.ServiceInterface
{
	public class AppContext
	{
		public ILogFactory LogFactory { get; set; }

		public ICacheClient Cache { get; set; }

		public IResourceManager ResourceManager { get; set; }

		public IFactoryProvider FactoryProvider { get; set; }
	}
}