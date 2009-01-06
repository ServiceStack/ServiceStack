using ServiceStack.LogicFacade;
using ServiceStack.ServiceModel;

namespace Sakila.ServiceModel
{
	public class ServiceModelFinder : ServiceModelFinderBase, IServiceModelFinder
	{
		public static ServiceModelFinder Instance = new ServiceModelFinder();
	}

}