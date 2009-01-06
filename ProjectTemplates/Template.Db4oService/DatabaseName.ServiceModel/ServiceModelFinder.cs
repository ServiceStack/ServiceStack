using ServiceStack.LogicFacade;
using ServiceStack.ServiceModel;

namespace @ServiceModelNamespace@
{
	public class ServiceModelFinder : ServiceModelFinderBase, IServiceModelFinder
	{
		public static ServiceModelFinder Instance = new ServiceModelFinder();
	}

}