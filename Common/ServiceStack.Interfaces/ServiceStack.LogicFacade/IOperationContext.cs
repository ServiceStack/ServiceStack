using ServiceStack.CacheAccess;
using ServiceStack.Configuration;
using ServiceStack.Logging;

namespace ServiceStack.LogicFacade
{
	public interface IOperationContext
	{
		ILogFactory LogFactory { get; set; }
		ICacheClient Cache { get; set; }
		IResourceManager Resources { get; set; }
		IFactoryProvider Factory { get; set; }
		string IpAddress { get; }
	}
}