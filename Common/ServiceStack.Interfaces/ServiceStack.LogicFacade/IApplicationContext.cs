using ServiceStack.CacheAccess;
using ServiceStack.Configuration;

namespace ServiceStack.LogicFacade
{
	public interface IApplicationContext
	{
		IFactoryProvider Factory { get; set; }

		T Get<T>() where T : class;
		
		ICacheClient Cache { get; set; }
		
		IResourceManager Resources { get; set; }
	}
}