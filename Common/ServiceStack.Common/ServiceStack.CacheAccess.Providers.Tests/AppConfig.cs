using System.Collections.Generic;
using ServiceStack.Configuration;

namespace ServiceStack.CacheAccess.Providers.Tests
{
	public static class AppConfig
	{
		public static List<string> MemcachedServers = ConfigUtils.GetListFromAppSetting("MemcachedServers");
	}
}