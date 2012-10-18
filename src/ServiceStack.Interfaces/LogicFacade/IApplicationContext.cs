using ServiceStack.CacheAccess;
using ServiceStack.Configuration;

namespace ServiceStack.LogicFacade
{
	public interface IApplicationContext
	{
		IFactoryProvider Factory { get; }

		T Get<T>() where T : class;

		ICacheClient Cache { get; }

		IResourceManager Resources { get; }
	}
}