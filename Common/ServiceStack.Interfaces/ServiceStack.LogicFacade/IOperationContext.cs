using ServiceStack.CacheAccess;
using ServiceStack.Configuration;

namespace ServiceStack.LogicFacade
{
	public interface IOperationContext
	{
		IFactoryProvider Factory { get; set; }
		ICacheClient Cache { get; set; }
		IResourceManager Resources { get; set; }
	}
}